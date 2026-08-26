using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using System;
using System.Linq;

namespace ACE.Server.WorldObjects
{
    partial class Player
    {
        #region XP Bottle
        // -------------------------------------------------------------------------
        // Ancient Bottle (WCID 490071) — XP Bottle
        // -------------------------------------------------------------------------
        private const long XpBottleMaxXp = 100_000_000L; // 100 million XP cap per bottle

        /// <summary>
        /// Uses the Ancient Bottle, releasing stored PvP XP up to the player's current
        /// cap headroom (lower of daily PvP budget and global rolling cap).
        /// If only part of the bottle is consumed the remainder stays in the bottle.
        /// </summary>
        public void UseXpBottle(Gem bottle)
        {
            var bottleXp = bottle.ItemTotalXp ?? 0;
            if (bottleXp <= 0)
            {
                Session.Network.EnqueueSend(new GameMessageSystemChat("The Ancient Bottle is empty.", ChatMessageType.Broadcast));
                return;
            }

            long rollingCapXp = RollingLevelCapManager.GetCurrentXpCap();
            if (rollingCapXp <= 0)
            {
                Session.Network.EnqueueSend(new GameMessageSystemChat("You cannot use the Ancient Bottle at this time.", ChatMessageType.Broadcast));
                return;
            }

            var xpRemainingGlobal = rollingCapXp - (TotalExperience ?? 0);
            if (xpRemainingGlobal <= 0)
            {
                Session.Network.EnqueueSend(new GameMessageSystemChat("You have reached the global XP cap and cannot absorb any experience from the Ancient Bottle right now.", ChatMessageType.Broadcast));
                return;
            }

            var capDailyMaxPvpCat = CapDailyMaxPvpCat > 0
                ? CapDailyMaxPvpCat
                : (long)(rollingCapXp * PropertyManager.GetDouble("daily_pvp_xp_category_ratio").Item);
            var pvpRemaining = Math.Max(0L, capDailyMaxPvpCat - CapPvpXp);
            if (pvpRemaining <= 0)
            {
                Session.Network.EnqueueSend(new GameMessageSystemChat("You have reached your daily PvP XP cap and cannot absorb any experience from the Ancient Bottle right now.", ChatMessageType.Broadcast));
                return;
            }

            // Grant the lesser of: what's in the bottle, daily PvP headroom, global headroom
            var headroom = Math.Min(pvpRemaining, xpRemainingGlobal);
            var xpToGrant = Math.Min(bottleXp, headroom);

            // GrantXP with exactly the computed headroom — no overflow possible (pre-capped).
            // Releasing stored XP is not a kill reward, so it opts out of the allegiance-size
            // zerg penalty applied in GrantXP (same rationale as the Ancient Bottle drain on death).
            GrantXP(xpToGrant, XpType.PvP, ShareType.None, applyPkSizeNerf: false);

            var leftInBottle = bottleXp - xpToGrant;
            if (leftInBottle <= 0)
            {
                bottle.ItemTotalXp = 0;
                Session.Network.EnqueueSend(new GameMessagePrivateUpdatePropertyInt64(bottle, PropertyInt64.ItemTotalXp, 0));
                Session.Network.EnqueueSend(new GameMessageSystemChat(
                    $"You drink from the Ancient Bottle and absorb {xpToGrant:N0} experience. The bottle is empty.",
                    ChatMessageType.Broadcast));
            }
            else
            {
                bottle.ItemTotalXp = leftInBottle;
                Session.Network.EnqueueSend(new GameMessagePrivateUpdatePropertyInt64(bottle, PropertyInt64.ItemTotalXp, leftInBottle));
                Session.Network.EnqueueSend(new GameMessageSystemChat(
                    $"You drink from the Ancient Bottle and absorb {xpToGrant:N0} experience. The bottle still contains {leftInBottle:N0} experience.",
                    ChatMessageType.Broadcast));
            }
        }

        /// <summary>
        /// Fills Ancient Bottles in the player's inventory with PvP XP that overflowed
        /// past the daily PvP cap or global XP cap.  Only a configurable fraction
        /// (ancient_bottle_fill_ratio) of the overflow is captured; the remainder is
        /// lost to the cap.  Excess beyond all bottle capacity is discarded.
        /// </summary>
        private void FillXpBottlesFromOverflow(long overflow)
        {
            if (overflow <= 0) return;

            // Only capture a fraction of post-cap PK XP; the rest is lost to the cap.
            var fillRatio = PropertyManager.GetDouble("ancient_bottle_fill_ratio").Item;
            overflow = (long)(overflow * fillRatio);
            if (overflow <= 0) return;

            var bottles = GetInventoryItemsOfWCID(CustomWeenieId.XpBottle);
            if (bottles == null || bottles.Count == 0) return;

            long remaining = overflow;
            foreach (var item in bottles)
            {
                if (remaining <= 0) break;

                var current = item.ItemTotalXp ?? 0;
                if (current >= XpBottleMaxXp) continue;

                var space = XpBottleMaxXp - current;
                var fill = Math.Min(remaining, space);
                item.ItemTotalXp = current + fill;
                remaining -= fill;

                Session.Network.EnqueueSend(new GameMessagePrivateUpdatePropertyInt64(item, PropertyInt64.ItemTotalXp, item.ItemTotalXp.Value));
                Session.Network.EnqueueSend(new GameMessageSystemChat(
                    $"Your Ancient Bottle absorbed {fill:N0} overflow PvP experience. ({item.ItemTotalXp.Value:N0} / {XpBottleMaxXp:N0})",
                    ChatMessageType.Broadcast));
            }
        }

        #endregion XP Bottle

        private const int SkillAttrResetBaseCost = 100;
        private const int SkillAttrResetMaxCost  = 10000;

        private static int CalculateSkillAttrResetCost(int useCount)
        {
            if (useCount == 0) return SkillAttrResetBaseCost;
            return Math.Min((int)(SkillAttrResetBaseCost * Math.Pow(1.35, useCount)), SkillAttrResetMaxCost);
        }

        public void UseSkillAttrResetGem(Gem gem)
        {
            if (this.QuestManager.CanSolve("SkillAlterationGemPickedUp") &&
                this.QuestManager.CanSolve("CombatSkillAlterationGemPickedUp") &&
                this.QuestManager.CanSolve("AttributeLoweringGemPickedUp") &&
                this.QuestManager.CanSolve("AttributeRaisingGemPickedUp"))
            {
                this.Session.Network.EnqueueSend(new GameMessageSystemChat(
                    $"You are already able to alter your skills and attributes without the help of a {gem.Name}. Seek the Temple of Enlightenment or Temple of Forgetfulness.",
                    ChatMessageType.Broadcast));
                return;
            }

            int useCount  = this.QuestManager.GetCurrentSolves("SkillAttrResetGemUsed");
            int trophyCost = CalculateSkillAttrResetCost(useCount);
            int trophiesHeld = this.GetNumInventoryItemsOfWCID(CustomWeenieId.PkTrophy);

            if (trophiesHeld < trophyCost)
            {
                this.Session.Network.EnqueueSend(new GameMessageSystemChat(
                    $"Using the {gem.Name} costs {trophyCost} PK Trophies (this is use #{useCount + 1}). You have {trophiesHeld} PK Trophies.",
                    ChatMessageType.Broadcast));
                return;
            }

            if (!this.TryConsumeFromInventoryWithNetworking(CustomWeenieId.PkTrophy, trophyCost))
            {
                this.Session.Network.EnqueueSend(new GameMessageSystemChat(
                    $"Failed to consume PK Trophies. Please try again or contact an admin.",
                    ChatMessageType.Broadcast));
                return;
            }

            this.QuestManager.Erase("SkillAlterationGemPickedUp");
            this.QuestManager.Erase("CombatSkillAlterationGemPickedUp");
            this.QuestManager.Erase("AttributeLoweringGemPickedUp");
            this.QuestManager.Erase("AttributeRaisingGemPickedUp");
            this.QuestManager.Increment("SkillAttrResetGemUsed");

            this.Session.Network.EnqueueSend(new GameMessageSystemChat(
                $"The {gem.Name} has cleared your quest stamps for the Temple of Enlightenment and Temple of Forgetfulness. {trophyCost} PK Trophies have been consumed.",
                ChatMessageType.Broadcast));
            this.TryConsumeFromInventoryWithNetworking(gem, 1);
        }

        public void UseTinkeringTool(Gem gem)
        {
            string playerMsg = "";

            var tinkeringSkills =
            this.Skills.Values.Where(
                s =>
                (
                    s.AdvancementClass == SkillAdvancementClass.Specialized ||
                    s.AdvancementClass == SkillAdvancementClass.Trained
                ) &&
                (
                    s.Skill == Skill.ItemTinkering ||
                    s.Skill == Skill.MagicItemTinkering ||
                    s.Skill == Skill.WeaponTinkering ||
                    s.Skill == Skill.ArmorTinkering
                ));

            if (tinkeringSkills == null || tinkeringSkills.Count() < 1)
            {
                playerMsg = $"You must have trained at least one tinkering skill to use an {gem.Name}";
                this.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                this.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                return;
            }

            //Check if player already has the NextTinkIsFoolproof flag set
            if (this.NextTinkIsFoolproof)
            {
                playerMsg = $"You have already consumed an {gem.Name} and have not yet used it for a tinkering attempt.";
                this.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                this.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                return;
            }

            //Set NextTinkIsFoolproof flag to true
            this.NextTinkIsFoolproof = true;

            playerMsg = $"You feel a slight discomfort as the {gem.Name} slowly slides into you, but that discomfort soon fades and is replaced by an overwhelming feeling of fulfillment.  The tool suddenly blasts its essence into you, filling you with good luck. As the tool empties itself into you, it becomes flacid and wet and slowly melts into nothing, now fully consumed.  You feel as though the next time you tinker anything it can't possibly fail.";
            this.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));

            this.TryConsumeFromInventoryWithNetworking(gem, 1);
        }
    }
}
