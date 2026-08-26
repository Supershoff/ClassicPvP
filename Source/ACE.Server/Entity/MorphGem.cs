using ACE.Common;
using ACE.Common.Extensions;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Factories.Tables;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ACE.Server.Entity
{
    public class MorphGem
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Morph Gem Weenie IDs
        //public const uint MorphGemValue            = 4200023;
        public const uint MorphGemRandomWorkmanship = 490027;
        public const uint MorphGemArcane           = 4200026;
        public const uint MorphGemRemoveMissileDReq = 480484;
        public const uint MorphGemRemoveMeleeDReq  = 480483;
        public const uint MorphGemRandomizeWeaponImbue = 480486;
        public const uint MorphGemRemovePlayerReq  = 480485;
        public const uint MorphGemRemoveRacialReq  = 480642;
        public const uint MorphGemRemoveAllegianceReq = 480643;
        public const uint MorphGemCreatureSlayerRandom = 480610;
        public const uint MorphGemCreatureResistRandom = 600039;
        public const uint MorphGemSlayerUpgrade    = 480639;
        //public const uint MorphGemBurningCoal      = 480638;
        public const uint MorphGemImpen            = 490025;
        public const uint MorphGemLesserImpen      = 490050;
        //public const uint MorphGemBanditHilt       = 490026;
        //public const uint MorphGemRareUpgrade      = 490040;
        //public const uint MorphGemRareReduction    = 490270;
        public const uint MorphGemJewelersSawblade = 490271;
        public const uint MorphGemAddSlayer        = 490304;
        //public const uint MorphGemHematite         = 490284;
        //public const uint MorphGemStrengthbeer     = 490327;
        //public const uint MorphGemEndurancebeer    = 490328;
        //public const uint MorphGemCoordinationbeer = 490329;
        //public const uint MorphGemQuicknessbeer    = 490330;
        //public const uint MorphGemFocusbeer        = 490331;
        //public const uint MorphGemWillpowerbeer    = 490332;
        //public const uint MorphGemHeroicMaster     = 1548800;
        public const uint MorphGemRandomCantrip    = 1548803;
        //public const uint MorphGemBurden           = 1548804;
        //public const uint MorphGemRareDmgBoost     = 1548805;
        //public const uint MorphGemRareDmgReduction = 1548806;
        //public const uint MorphGemMeleeCleave      = 490512;
        //public const uint MorphGemMinValue         = 20000;
        #endregion Morph Gem Weenie IDs

        public static HashSet<uint> MorphGems = new HashSet<uint>()
        {
            MorphGemRandomWorkmanship,
            MorphGemArcane,
            MorphGemRemoveMissileDReq,
            MorphGemRemoveMeleeDReq,
            MorphGemRandomizeWeaponImbue,
            MorphGemRemovePlayerReq,
            MorphGemRemoveRacialReq,
            MorphGemRemoveAllegianceReq,
            MorphGemCreatureSlayerRandom,
            MorphGemCreatureResistRandom,
            MorphGemSlayerUpgrade,
            MorphGemImpen,
            MorphGemLesserImpen,
            MorphGemJewelersSawblade,
            MorphGemAddSlayer,
            MorphGemRandomCantrip,
        };

        public static bool IsMorphGem(uint weenieId)
        {
            return MorphGems.Contains(weenieId);
        }

        #region readonly references

        //public static readonly List<int> HeroicMasterSpells =
        //    new List<int>()
        //    {
        //        4733,    //Master Duelist's Coordination
        //        4737,    //Master Hero's Endurance
        //        4741,    //Master Sage's Focus
        //        4745,    //Master Rover's Quickness
        //        4749,    //Master Brute's Strength
        //        4753,    //Master Adherent's Willpower
        //        4755,    //Journeyman Survivor's Health
        //        4757,    //Journeyman Clairvoyant's Mana
        //        4759,    //Journeyman Tracker's Stamina
        //        4906,    //Apprentice Challenger's Rejuvenation
        //        6333,    //Gauntlet Damage Reduction II
        //        6335,    //Gauntlet Critical Damage Reduction II
        //        6340,    //Gauntlet Vitality III
        //        6337,    //Gauntlet Healing Boost II
        //        6331,    //Gauntlet Damage Boost II
        //        6329,    //Gauntlet Critical Damage Boost II
        //    };

        private static readonly HashSet<uint> morphGemsAllowedNonLootGen = new HashSet<uint>()
        {
            MorphGemRemovePlayerReq,
            MorphGemRemoveRacialReq,
            MorphGemRemoveAllegianceReq,
            MorphGemRemoveMissileDReq,
            MorphGemRemoveMeleeDReq,
            MorphGemJewelersSawblade,
            MorphGemImpen,
            MorphGemLesserImpen,
        };

        #endregion readonly references

        public static void ApplyMorphGem(Player player, WorldObject source, WorldObject target)
        {
            try
            {
                //Only allow loot gen items to be morphed, except for gems that are allowed to be applied to quest / rare items
                if ((target.ItemWorkmanship == null ||
                    target.IsAttunedOrContainsAttuned ||
                    (target.ResistMagic == 9999 && !target.IsShield && !(target.ValidLocations?.HasFlag(EquipMask.Cloak) ?? false)))
                    && !morphGemsAllowedNonLootGen.Contains(source.WeenieClassId))
                {
                    player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                    return;
                }

                string playerMsg = string.Empty;

                var targetItemSpells = target.Biota.GetKnownSpellsIds(target.BiotaDatabaseLock);

                switch (source.WeenieClassId)
                {
                    #region MorphGemValue
//                    case 4200023: // MorphGemValue
//
//                        var currentItemValue = target.GetProperty(PropertyInt.Value);
//
//                        if (!currentItemValue.HasValue)
//                        {
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (currentItemValue.Value <= 20000)
//                        {
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat("Morph gems do not allow an item's Value to be reduced below 20k", ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (target.GetProperty(PropertyInt.RareId).HasValue)
//                        {
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat("This gem cannot be used on Rare armor or weapons.", ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        var valRandom = new Random();
//                        bool valueGain = valRandom.Next(0, 99) < 10;
//                        var percentChange = valRandom.Next(5, 16) / 100f;
//                        var valueChange = (int)Math.Round(currentItemValue.Value * percentChange * (valueGain ? 1 : -1));
//                        var newValue = currentItemValue.Value + valueChange;
//
//                        if (newValue < 20000)
//                        {
//                            valueChange = 20000 - currentItemValue.Value;
//                            newValue = 20000;
//                        }
//
//                        player.UpdateProperty(target, PropertyInt.Value, newValue);
//                        AddMorphGemLog(target, MorphGemValue);
//
//                        if (valueChange > 0)
//                            playerMsg = $"Bad luck. The Morph Gem backfired. Your item's value has increased by {valueChange}";
//                        else if (valueChange == 0)
//                            playerMsg = $"The Morph Gem shatters against your item and leaves it unchanged. Could be worse.";
//                        else
//                            playerMsg = $"You apply the Morph Gem skillfully and have reduced the value of your item by {-1 * valueChange}";
//
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        break;
                    #endregion MorphGemValue

                    #region MorphGemRandomWorkmanship
                    case MorphGemRandomWorkmanship:

                        var currentItemWork = target.GetProperty(PropertyInt.ItemWorkmanship);

                        if (!currentItemWork.HasValue)
                        {
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }
                        
                        player.UpdateProperty(target, PropertyInt.ItemWorkmanship, Math.Max(1, currentItemWork.Value - 1));
                        AddMorphGemLog(target, MorphGemRandomWorkmanship);

                        playerMsg = $"You apply the Morph Gem skillfully and have reduced the workmanship of your {target.NameWithMaterial} to {currentItemWork.Value - 1}.";

                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                        break;

                    #endregion MorphGemRandomWorkmanship

                    #region MorphGemArcane
                    case MorphGemArcane:

                        var currentItemArcane = target.GetProperty(PropertyInt.ItemDifficulty);

                        if (!currentItemArcane.HasValue)
                        {
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }
                    
                        var outcomeRoll = ThreadSafeRandom.Next(1, 100);
                        int arcaneChange;

                        if (outcomeRoll <= 75)          // 75% — success, reduce by 5–25
                            arcaneChange = -ThreadSafeRandom.Next(5, 25);
                        else if (outcomeRoll <= 90)     // 15% — fizzle, no effect
                            arcaneChange = 0;
                        else                            // 10% — backfire, increase by 5–15
                            arcaneChange = ThreadSafeRandom.Next(5, 15);

                        var newArcane = Math.Max(1, currentItemArcane.Value + arcaneChange);
                        arcaneChange = newArcane - currentItemArcane.Value;

                        player.UpdateProperty(target, PropertyInt.ItemDifficulty, newArcane);
                        AddMorphGemLog(target, MorphGemArcane);

                        if (arcaneChange < 0)
                            playerMsg = $"You apply the Morph Gem skillfully and have reduced the arcane requirement of your {target.NameWithMaterial} by {-arcaneChange}";
                        else if (arcaneChange == 0)
                            playerMsg = $"The Morph Gem shatters against your {target.NameWithMaterial} and leaves it unchanged. Could be worse.";
                        else
                            playerMsg = $"The Morph Gem backfires against your {target.NameWithMaterial} and its arcane requirement has increased by {arcaneChange}";

                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                        break;

                    #endregion MorphGemArcane

                    #region MorphGemRemoveMissileDReq
                    case MorphGemRemoveMissileDReq:

                        var hasMissileActivationReq = target.ItemSkillLimit == Skill.MissileDefense && target.ItemSkillLevelLimit != null;
                        var removedMissileWieldReq = RemoveWieldRequirementForSkill(target, Skill.MissileDefense);

                        if (!hasMissileActivationReq && !removedMissileWieldReq)
                        {
                            playerMsg = $"Your {target.NameWithMaterial} does not currently have a Missile Defense requirement to remove.";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        if (hasMissileActivationReq)
                        {
                            target.ItemSkillLimit = null;
                            target.ItemSkillLevelLimit = null;
                        }

                        playerMsg = $"You apply the Morph Gem skillfully and have removed the Missile Defense requirement of your item.";
                        AddMorphGemLog(target, MorphGemRemoveMissileDReq);

                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                        break;

                    #endregion MorphGemRemoveMissileDReq

                    #region MorphGemRemoveMeleeDReq
                    case MorphGemRemoveMeleeDReq:

                        var hasMeleeActivationReq = target.ItemSkillLimit == Skill.MeleeDefense && target.ItemSkillLevelLimit != null;
                        var removedMeleeWieldReq = RemoveWieldRequirementForSkill(target, Skill.MeleeDefense);

                        if (!hasMeleeActivationReq && !removedMeleeWieldReq)
                        {
                            playerMsg = $"Your {target.NameWithMaterial} does not currently have a Melee Defense requirement to remove.";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        if (hasMeleeActivationReq)
                        {
                            target.ItemSkillLimit = null;
                            target.ItemSkillLevelLimit = null;
                        }

                        playerMsg = $"You apply the Morph Gem skillfully and have removed the Melee Defense requirement of your item.";
                        AddMorphGemLog(target, MorphGemRemoveMeleeDReq);

                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                        break;

                    #endregion MorphGemRemoveMeleeDReq

                    #region MorphGemRandomizeWeaponImbue
                    case MorphGemRandomizeWeaponImbue:

                        //Target must have an AR/CS/CB imbue
                        if (!(target.HasImbuedEffect(ImbuedEffectType.CripplingBlow) ||
                            target.HasImbuedEffect(ImbuedEffectType.ArmorRending) ||
                            target.HasImbuedEffect(ImbuedEffectType.CriticalStrike)))
                        {
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        var hasFetish = target.HasImbuedEffect(ImbuedEffectType.IgnoreSomeMagicProjectileDamage);

                        var origImbueEffect = target.ImbuedEffect;
                        var roll = ThreadSafeRandom.Next(0, 1);

                        if (target.HasImbuedEffect(ImbuedEffectType.CripplingBlow))
                            target.ImbuedEffect = (roll == 0 && target.WeenieType != WeenieType.Caster) ? ImbuedEffectType.ArmorRending : ImbuedEffectType.CriticalStrike;
                        else if (target.HasImbuedEffect(ImbuedEffectType.ArmorRending))
                            target.ImbuedEffect = roll == 0 ? ImbuedEffectType.CripplingBlow : ImbuedEffectType.CriticalStrike;
                        else if (target.HasImbuedEffect(ImbuedEffectType.CriticalStrike))
                            target.ImbuedEffect = (roll == 0 && target.WeenieType != WeenieType.Caster) ? ImbuedEffectType.ArmorRending : ImbuedEffectType.CripplingBlow;

                        target.IconUnderlayId = RecipeManager.IconUnderlay[target.ImbuedEffect];

                        if (hasFetish)
                            target.ImbuedEffect |= ImbuedEffectType.IgnoreSomeMagicProjectileDamage;

                        playerMsg = $"You apply the Morph Gem skillfully and have changed your weapon's imbue from {origImbueEffect} to {target.ImbuedEffect}";
                        AddMorphGemLog(target, MorphGemRandomizeWeaponImbue);

                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                        break;

                    #endregion MorphGemRandomizeWeaponImbue

                    #region MorphGemRemovePlayerReq
                    case MorphGemRemovePlayerReq:

                        if (target.WeenieClassId == 8142017u)
                        {
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        if (!target.GetProperty(PropertyInstanceId.AllowedWielder).HasValue)
                        {
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        var origWielder = target.GetProperty(PropertyString.CraftsmanName);

                        target.RemoveProperty(PropertyInstanceId.AllowedWielder);
                        target.RemoveProperty(PropertyString.CraftsmanName);

                        playerMsg = $"You apply the Morph Gem skillfully and have altered your item so it is no longer wield restricted to {origWielder}";
                        AddMorphGemLog(target, MorphGemRemovePlayerReq);

                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                        break;

                    #endregion MorphGemRemovePlayerReq

                    #region MorphGemRemoveRacialReq
                    case MorphGemRemoveRacialReq:

                        var hasRacialActivationReq = target.HeritageGroup != HeritageGroup.Invalid;
                        var hasRacialWieldReq = HasHeritageWieldRequirement(target);

                        if (!hasRacialActivationReq && !hasRacialWieldReq)
                        {
                            playerMsg = $"Your {target.NameWithMaterial} does not currently have a racial requirement to remove.";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        //capture the race for the player message before either requirement is cleared
                        var origRace = hasRacialActivationReq
                            ? target.ItemHeritageGroupRestriction ?? target.HeritageGroup.ToString()
                            : GetHeritageWieldRequirement(target).ToString();

                        if (hasRacialActivationReq)
                        {
                            player.UpdateProperty(target, PropertyInt.HeritageGroup, null);
                            player.UpdateProperty(target, PropertyString.ItemHeritageGroupRestriction, null);
                        }

                        if (hasRacialWieldReq)
                            RemoveHeritageWieldRequirement(target);

                        playerMsg = $"You apply the Morph Gem skillfully and have removed the {origRace} racial requirement from your {target.NameWithMaterial}.";
                        AddMorphGemLog(target, MorphGemRemoveRacialReq);

                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                        break;

                    #endregion MorphGemRemoveRacialReq

                    #region MorphGemRemoveAllegianceReq
                    case MorphGemRemoveAllegianceReq:

                        //there is no wield side allegiance requirement to sweep - WieldRequirement.IntStat is unused in PY16
                        var allegianceRankReq = target.ItemAllegianceRankLimit ?? 0;

                        if (allegianceRankReq <= 0)
                        {
                            playerMsg = $"Your {target.NameWithMaterial} does not currently have an allegiance rank requirement to remove.";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        player.UpdateProperty(target, PropertyInt.ItemAllegianceRankLimit, null);

                        playerMsg = $"You apply the Morph Gem skillfully and have removed the allegiance rank {allegianceRankReq} requirement from your {target.NameWithMaterial}.";
                        AddMorphGemLog(target, MorphGemRemoveAllegianceReq);

                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                        break;

                    #endregion MorphGemRemoveAllegianceReq

                    #region MorphGemCreatureSlayerRandom
                    case MorphGemCreatureSlayerRandom:

                        //Only allow on loot gen weapons, casters or armor that already has a weapon slayer or gear slayer
                        if(( target.ItemType != ItemType.MeleeWeapon &&
                             target.ItemType != ItemType.MissileWeapon &&
                             target.ItemType != ItemType.Caster &&
                             (target.ArmorLevel ?? 0) < 1) ||
                             (!target.SlayerCreatureType.HasValue &&
                              target.GearCreatureSlayerType == CreatureType.Invalid))
                        {
                            playerMsg = "This gem can only be used on loot generated weapons, casters with a Slayer or armor with a Creature Slayer Rating.";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        //For weapons and wands
                        if (target.ItemType == ItemType.MeleeWeapon || target.ItemType == ItemType.MissileWeapon || target.ItemType == ItemType.Caster)
                        {
                            target.ApplyRandomSlayer(target.SlayerDamageBonus ?? 1.2, target.SlayerCreatureType.HasValue ? target.SlayerCreatureType.Value : CreatureType.Invalid);
                            playerMsg = $"The Morph Gem alters your weapon's slayer type to {target.SlayerCreatureType.ToDisplayString()}";
                            
                        }
                        else if (target.GearCreatureSlayerType != CreatureType.Invalid && target.GearCreatureSlayerRating > 0)
                        {
                            target.GearCreatureSlayerType = target.GetRandomCreatureType(target.GearCreatureSlayerType);
                            playerMsg = $"The Morph Gem alters your {target.NameWithMaterial} to have {target.GearCreatureSlayerType.ToDisplayString()} Slayer Rating {target.GearCreatureSlayerRating}";
                        }
                        else
                        {
                            playerMsg = "This gem can only be used on loot generated weapons, casters with a Slayer or armor with a Creature Slayer Rating.";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        //Send player message confirming the applied morph gem
                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));

                        AddMorphGemLog(target, MorphGemCreatureSlayerRandom);
                        break;

                    #endregion MorphGemCreatureSlayerRandom

                    #region MorphGemCreatureResistRandom
                    case MorphGemCreatureResistRandom:

                        if (target.GearCreatureResistType != CreatureType.Invalid && target.GearCreatureResistRating > 0)
                        {
                            target.GearCreatureResistType = target.GetRandomCreatureType(target.GearCreatureResistType);
                            playerMsg = $"The Morph Gem alters your {target.NameWithMaterial} to have {target.GearCreatureResistType.ToDisplayString()} Resist Rating {target.GearCreatureResistRating}";
                        }
                        else
                        {
                            playerMsg = "This gem can only be used on loot generated armor, jewelry or undies with a Creature Resist Rating.";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        //Send player message confirming the applied morph gem
                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));

                        AddMorphGemLog(target, MorphGemCreatureResistRandom);
                        break;

                    #endregion MorphGemCreatureResistRandom

                    #region MorphGemSlayerUpgrade
                    case MorphGemSlayerUpgrade:
                        
                        if (target.SlayerCreatureType != null)
                        {
                            if (target.SlayerDamageBonus < 1.8)
                            {
                                playerMsg = $"The Morph Gem alters your weapon's slayer damage bonus to 1.8";
                                target.SlayerDamageBonus = 1.8;
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            }
                            else
                            {
                                playerMsg = $"Your weapon's slayer damage bonus is already >= 1.8";
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                                player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                                return;
                            }
                        }
                        else
                        {
                            playerMsg = "The gem can only be applied to weapons that have a Slayer property";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        AddMorphGemLog(target, MorphGemSlayerUpgrade);
                        break;

                    #endregion MorphGemSlayerUpgrade

                    #region MorphGemBurningCoal
//                    case 480638: // MorphGemBurningCoal
//
//                        if (!(target.ItemType == ItemType.Armor || target.ItemType == ItemType.Jewelry || target.ItemType == ItemType.Clothing))
//                        {
//                            playerMsg = "The gem can only be applied to armor, clothing or jewelry";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (targetItemSpells == null || targetItemSpells.Count < 1)
//                        {
//                            playerMsg = "The gem can only be applied to magical items";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//                        else if (targetItemSpells.Contains(3204))
//                        {
//                            playerMsg = "Your target item already has Blazing Heart on it, you cannot add it twice";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        target.Biota.GetOrAddKnownSpell(3204, target.BiotaDatabaseLock, out _);
//                        playerMsg = $"With a steady hand and pure heart, you skillfully apply the morph gem to your {target.NameWithMaterial} and have successfully added the spell Blazing Heart";
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, 480638); // MorphGemBurningCoal
//                        break;
                    #endregion MorphGemBurningCoal

                    #region MorphGemImpen
                    case MorphGemImpen:

                        if (target.WeenieType != WeenieType.Clothing)
                        {
                            playerMsg = "The gem can only be applied to armor and underclothes";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        if (target.ArmorLevel > 0 && target.ItemWorkmanship == null && !target.GetProperty(PropertyInt.RareId).HasValue)
                        {
                            playerMsg = "The gem cannot be applied quest armor, only loot gen or rare armor";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        if (!target.ItemMaxMana.HasValue || targetItemSpells == null || targetItemSpells.Count == 0)
                        {
                            playerMsg = "The gem can only be applied to magical items";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        if (targetItemSpells.Contains(2604) ||
                            targetItemSpells.Contains(2592) ||
                            targetItemSpells.Contains(4667) ||
                            targetItemSpells.Contains(6095) ||
                            targetItemSpells.Contains(3710))
                        {
                            playerMsg = "The gem cannot be used on an item that already has an Impenetrability cantrip";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }
                        
                        playerMsg = "You successfully apply the morph gem and have added {0} Impenetrability cantrip to your {1}";

                        var spellId = 0;
                        var impenLevel = ThreadSafeRandom.Next(0, 99);
                        if (impenLevel < 67)
                        {
                            spellId = 2604;
                            playerMsg = String.Format(playerMsg, "a Minor", target.Name);
                        }
                        else
                        {
                            spellId = 2592;
                            playerMsg = String.Format(playerMsg, "a Major", target.Name);
                        }                            

                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                        target.Biota.GetOrAddKnownSpell(spellId, target.BiotaDatabaseLock, out _);
                        AddMorphGemLog(target, MorphGemImpen);
                        
                        break;

                    #endregion MorphGemImpen

                    #region MorphGemLesserImpen
                    case MorphGemLesserImpen:

                        if (target.WeenieType != WeenieType.Clothing)
                        {
                            playerMsg = "The gem can only be applied to armor and underclothes";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        if (target.ArmorLevel > 0 && target.ItemWorkmanship == null && !target.GetProperty(PropertyInt.RareId).HasValue)
                        {
                            playerMsg = "The gem cannot be applied quest armor, only loot gen or rare armor";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        if (!target.ItemMaxMana.HasValue || targetItemSpells == null || targetItemSpells.Count == 0)
                        {
                            playerMsg = "The gem can only be applied to magical items";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        // Major or better cannot be improved any further by this gem
                        if (targetItemSpells.Contains(2592) ||
                            targetItemSpells.Contains(4667) ||
                            targetItemSpells.Contains(6095) ||
                            targetItemSpells.Contains(3710))
                        {
                            playerMsg = "The gem cannot be used on an item that already has a Major Impenetrability cantrip";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        var lesserImpenHasMinor = targetItemSpells.Contains(2604);
                        var lesserImpenIsMajor = ThreadSafeRandom.Next(0, 99) >= 97;

                        if (lesserImpenHasMinor)
                        {
                            // Rolling to upgrade the existing Minor Impenetrability into a Major Impenetrability
                            if (lesserImpenIsMajor)
                            {
                                RemoveAllCantripsInProgression(target, 2592);
                                target.Biota.GetOrAddKnownSpell(2592, target.BiotaDatabaseLock, out _);
                                playerMsg = $"You successfully apply the morph gem and have upgraded the Minor Impenetrability cantrip on your {target.Name} to a Major Impenetrability cantrip";
                            }
                            else
                            {
                                playerMsg = $"You apply the morph gem, but it fails to strengthen the Minor Impenetrability cantrip on your {target.Name}";
                            }
                        }
                        else
                        {
                            var lesserImpenSpellId = lesserImpenIsMajor ? 2592 : 2604;
                            target.Biota.GetOrAddKnownSpell(lesserImpenSpellId, target.BiotaDatabaseLock, out _);
                            playerMsg = $"You successfully apply the morph gem and have added {(lesserImpenIsMajor ? "a Major" : "a Minor")} Impenetrability cantrip to your {target.Name}";
                        }

                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                        AddMorphGemLog(target, MorphGemLesserImpen);

                        break;

                    #endregion MorphGemLesserImpen

                    #region MorphGemBanditHilt
//                    case 490026: // MorphGemBanditHilt
//
//                        if (target.WeenieType != WeenieType.MeleeWeapon ||
//                            target.WeaponSkill != Skill.LightWeapons ||
//                            (!target.W_AttackType.HasFlag(AttackType.DoubleSlash) && !target.W_AttackType.HasFlag(AttackType.DoubleThrust)))
//                        {
//                            playerMsg = "This gem can only be used on Light Weapon melee weapons with the Multi-Strike property";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        target.W_AttackType = AttackType.TripleStrike;
//                        playerMsg = $"The morph gem alters your {target.NameWithMaterial} into a Triple-Strike weapon";
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, 490026); // MorphGemBanditHilt
//                        break;
                    #endregion MorphGemBanditHilt

                    #region MorphGemRareUpgrade
//                    case 490040: // MorphGemRareUpgrade
//
//                        if (!target.GetProperty(PropertyInt.RareId).HasValue)
//                        {
//                            playerMsg = "This gem can only be used on rare armor, jewelry and weapons";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (target.WeenieType != WeenieType.Clothing &&
//                            target.WeenieType != WeenieType.Caster &&
//                            target.WeenieType != WeenieType.MeleeWeapon &&
//                            target.WeenieType != WeenieType.MissileLauncher &&
//                            target.ItemType != ItemType.Jewelry &&
//                            !target.IsShield)
//                        {
//                            playerMsg = "This gem can only be used on rare armor, jewelry and weapons";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        var itemEpicList = target.EpicCantrips.Keys;
//                        if (itemEpicList == null || itemEpicList.Count < 1)
//                        {
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat("The target item has no epic cantrips to upgrade", ChatMessageType.Broadcast));
//                            return;
//                        }
//
//                        foreach (var epicSpellId in itemEpicList)
//                        {
//                            var level1SpellId = SpellLevelProgression.GetLevel1SpellId((SpellId)epicSpellId);
//                            var progression = SpellLevelProgression.GetSpellLevels(level1SpellId);
//                            if (progression != null && progression.Count >= 4)
//                            {
//                                var legendarySpellId = progression[3];
//                                target.Biota.TryRemoveKnownSpell(epicSpellId, target.BiotaDatabaseLock);
//                                target.Biota.GetOrAddKnownSpell((int)legendarySpellId, target.BiotaDatabaseLock, out _);
//                            }
//                        }
//
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Your {target.NameWithMaterial} has had its epic armor cantrips upgraded to legendaries", ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, 490040); // MorphGemRareUpgrade
//                        break;
                    #endregion MorphGemRareUpgrade

                    #region MorphGemRareReduction
//                    case 490270: // MorphGemRareReduction
//
//                        if (!target.ArmorLevel.HasValue || target.ArmorLevel.Value < 1 || !target.GetProperty(PropertyInt.RareId).HasValue)
//                        {
//                            playerMsg = "This gem can only be used on multi-slot rare armor";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        EquipMask targetValidLocations = target.ValidLocations ?? EquipMask.None;
//
//                        if (targetValidLocations.HasFlag(EquipMask.ChestArmor))
//                        {
//                            playerMsg = $"You successfully apply the {source.Name} to reduce your {target.NameWithMaterial} to cover only your chest.";
//                            player.UpdateProperty(target, PropertyInt.ValidLocations, (int)EquipMask.ChestArmor);
//                            player.UpdateProperty(target, PropertyInt.ClothingPriority, (int)CoverageMask.OuterwearChest);
//                        }
//                        else if (targetValidLocations.HasFlag(EquipMask.UpperLegArmor))
//                        {
//                            playerMsg = $"You successfully apply the {source.Name} to reduce your {target.NameWithMaterial} to cover only your upper legs.";
//                            player.UpdateProperty(target, PropertyInt.ValidLocations, (int)EquipMask.UpperLegArmor);
//                            player.UpdateProperty(target, PropertyInt.ClothingPriority, (int)CoverageMask.OuterwearUpperLegs);
//                        }
//                        else
//                        {
//                            playerMsg = "This gem can only be used on multi-slot rare armor";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, 490270); // MorphGemRareReduction
//                        break;
                    #endregion MorphGemRareReduction

                    #region MorphGemJewelersSawblade
                    case MorphGemJewelersSawblade:

                        EquipMask validLocations = target.ValidLocations ?? EquipMask.None;
                        int newLocRoll = ThreadSafeRandom.Next(0, 1);

                        if (validLocations.HasFlag(EquipMask.NeckWear))
                        {
                            if (newLocRoll == 0)
                            {
                                player.UpdateProperty(target, PropertyInt.ValidLocations, (int)EquipMask.WristWear);
                                playerMsg = $"You have successfully used the {source.Name} to alter your {target.NameWithMaterial} to be wearable on your wrists!";
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            }
                            else
                            {
                                player.UpdateProperty(target, PropertyInt.ValidLocations, (int)EquipMask.FingerWear);
                                playerMsg = $"You have successfully used the {source.Name} to alter your {target.NameWithMaterial} to be wearable on your fingers!";
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            }
                        }
                        else if (validLocations.HasFlag(EquipMask.FingerWearLeft) || validLocations.HasFlag(EquipMask.FingerWearRight))
                        {
                            if (newLocRoll == 0)
                            {
                                player.UpdateProperty(target, PropertyInt.ValidLocations, (int)EquipMask.WristWear);
                                playerMsg = $"You have successfully used the {source.Name} to alter your {target.NameWithMaterial} to be wearable on your wrists!";
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            }
                            else
                            {
                                player.UpdateProperty(target, PropertyInt.ValidLocations, (int)EquipMask.NeckWear);
                                playerMsg = $"You have successfully used the {source.Name} to alter your {target.NameWithMaterial} to be wearable on your neck!";
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            }
                        }
                        else if (validLocations.HasFlag(EquipMask.WristWearLeft) || validLocations.HasFlag(EquipMask.WristWearRight))
                        {
                            if (newLocRoll == 0)
                            {
                                player.UpdateProperty(target, PropertyInt.ValidLocations, (int)EquipMask.FingerWear);
                                playerMsg = $"You have successfully used the {source.Name} to alter your {target.NameWithMaterial} to be wearable on your fingers!";
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            }
                            else
                            {
                                player.UpdateProperty(target, PropertyInt.ValidLocations, (int)EquipMask.NeckWear);
                                playerMsg = $"You have successfully used the {source.Name} to alter your {target.NameWithMaterial} to be wearable on your neck!";
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            }
                        }
                        else
                        {
                            playerMsg = "This gem can only be used on necklaces, rings and bracelets";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        AddMorphGemLog(target, MorphGemJewelersSawblade);
                        break;

                    #endregion MorphGemJewelersSawblade

                    #region MorphGemAddSlayer
                    case MorphGemAddSlayer:

                        if (target as MeleeWeapon == null &&
                            !target.IsCaster &&
                            !target.IsBow &&
                            !target.IsThrownWeapon &&
                            !target.IsAtlatl)
                        {
                            playerMsg = "This gem can only be used on weapons or magic casters";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        if (target.SlayerCreatureType != null &&
                            target.SlayerCreatureType > 0 &&
                            target.SlayerDamageBonus > 1)
                        {
                            playerMsg = "This gem cant be used on a weapon or magic caster that already has a slayer on it";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        target.ApplyRandomSlayer(1.8);
                        playerMsg = $"You have successfully used the {source.Name} to add {target.SlayerCreatureType?.ToString() ?? "Unknown"} Slayer to your {target.NameWithMaterial}!";
                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                        AddMorphGemLog(target, MorphGemAddSlayer);
                        break;

                    #endregion MorphGemAddSlayer

                    #region MorphGemHematite
//                    case 490284: // MorphGemHematite
//
//                        if (!(target.ItemType == ItemType.Armor || target.ItemType == ItemType.Jewelry || target.ItemType == ItemType.Clothing))
//                        {
//                            playerMsg = "The gem can only be applied to armor, clothing or jewelry";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (targetItemSpells == null || targetItemSpells.Count < 1)
//                        {
//                            playerMsg = "The gem can only be applied to magical items";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//                        else if (targetItemSpells.Contains(2004))
//                        {
//                            playerMsg = "Your target item already has Warrior's Vitality on it, you cannot add it twice";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        target.Biota.GetOrAddKnownSpell(2004, target.BiotaDatabaseLock, out _);
//                        playerMsg = $"With a steady hand you skillfully apply the morph gem to your {target.NameWithMaterial} and have successfully added the spell Warrior's Vitality";
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, 490284); // MorphGemHematite
//                        break;
                    #endregion MorphGemHematite

                    #region MorphGemStrengthbeer
//                    case 490327: // MorphGemStrengthbeer
//
//                        if (!(target.ItemType == ItemType.Armor || target.ItemType == ItemType.Jewelry || target.ItemType == ItemType.Clothing))
//                        {
//                            playerMsg = "The gem can only be applied to armor, clothing or jewelry";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (targetItemSpells == null || targetItemSpells.Count < 1)
//                        {
//                            playerMsg = "The gem can only be applied to magical items";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//                        else if (targetItemSpells.Contains(3864))
//                        {
//                            playerMsg = "Your target item already has Zongo's Fist on it, you cannot add it twice";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        target.Biota.GetOrAddKnownSpell(3864, target.BiotaDatabaseLock, out _);
//                        playerMsg = $"With a steady hand you skillfully apply the morph gem to your {target.NameWithMaterial} and have successfully added the spell Zongo's Fist";
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, MorphGemStrengthbeer);
//                        break;
//
//                    #endregion MorphGemStrengthbeer
//
//                    #region MorphGemEndurancebeer
//                    case MorphGemEndurancebeer:
//
//                        if (!(target.ItemType == ItemType.Armor || target.ItemType == ItemType.Jewelry || target.ItemType == ItemType.Clothing))
//                        {
//                            playerMsg = "The gem can only be applied to armor, clothing or jewelry";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (targetItemSpells == null || targetItemSpells.Count < 1)
//                        {
//                            playerMsg = "The gem can only be applied to magical items";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//                        else if (targetItemSpells.Contains(3863))
//                        {
//                            playerMsg = "Your target item already has Hunter's Hardiness on it, you cannot add it twice";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        target.Biota.GetOrAddKnownSpell(3863, target.BiotaDatabaseLock, out _);
//                        playerMsg = $"With a steady hand you skillfully apply the morph gem to your {target.NameWithMaterial} and have successfully added the spell Hunter's Hardiness";
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, MorphGemEndurancebeer);
//                        break;
//
//                    #endregion MorphGemEndurancebeer
//
//                    #region MorphGemCoordinationbeer
//                    case MorphGemCoordinationbeer:
//
//                        if (!(target.ItemType == ItemType.Armor || target.ItemType == ItemType.Jewelry || target.ItemType == ItemType.Clothing))
//                        {
//                            playerMsg = "The gem can only be applied to armor, clothing or jewelry";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (targetItemSpells == null || targetItemSpells.Count < 1)
//                        {
//                            playerMsg = "The gem can only be applied to magical items";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//                        else if (targetItemSpells.Contains(3533))
//                        {
//                            playerMsg = "Your target item already has Brighteyes' Favor on it, you cannot add it twice";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        target.Biota.GetOrAddKnownSpell(3533, target.BiotaDatabaseLock, out _);
//                        playerMsg = $"With a steady hand you skillfully apply the morph gem to your {target.NameWithMaterial} and have successfully added the spell Brighteyes' Favor";
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, MorphGemCoordinationbeer);
//                        break;
//
//                    #endregion MorphGemCoordinationbeer
//
//                    #region MorphGemQuicknessbeer
//                    case MorphGemQuicknessbeer:
//
//                        if (!(target.ItemType == ItemType.Armor || target.ItemType == ItemType.Jewelry || target.ItemType == ItemType.Clothing))
//                        {
//                            playerMsg = "The gem can only be applied to armor, clothing or jewelry";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (targetItemSpells == null || targetItemSpells.Count < 1)
//                        {
//                            playerMsg = "The gem can only be applied to magical items";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//                        else if (targetItemSpells.Contains(3531))
//                        {
//                            playerMsg = "Your target item already has Bobo's Quickening on it, you cannot add it twice";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        target.Biota.GetOrAddKnownSpell(3531, target.BiotaDatabaseLock, out _);
//                        playerMsg = $"With a steady hand you skillfully apply the morph gem to your {target.NameWithMaterial} and have successfully added the spell Bobo's Quickening";
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, MorphGemQuicknessbeer);
//                        break;
//
//                    #endregion MorphGemQuicknessbeer
//
//                    #region MorphGemFocusbeer
//                    case MorphGemFocusbeer:
//
//                        if (!(target.ItemType == ItemType.Armor || target.ItemType == ItemType.Jewelry || target.ItemType == ItemType.Clothing))
//                        {
//                            playerMsg = "The gem can only be applied to armor, clothing or jewelry";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (targetItemSpells == null || targetItemSpells.Count < 1)
//                        {
//                            playerMsg = "The gem can only be applied to magical items";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//                        else if (targetItemSpells.Contains(3530))
//                        {
//                            playerMsg = "Your target item already has Ketnan's Eye on it, you cannot add it twice";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        target.Biota.GetOrAddKnownSpell(3530, target.BiotaDatabaseLock, out _);
//                        playerMsg = $"With a steady hand you skillfully apply the morph gem to your {target.NameWithMaterial} and have successfully added the spell Ketnan's Eye";
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, MorphGemFocusbeer);
//                        break;
//
//                    #endregion MorphGemFocusbeer
//
//                    #region MorphGemWillpowerbeer
//                    case MorphGemWillpowerbeer:
//
//                        if (!(target.ItemType == ItemType.Armor || target.ItemType == ItemType.Jewelry || target.ItemType == ItemType.Clothing))
//                        {
//                            playerMsg = "The gem can only be applied to armor, clothing or jewelry";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (targetItemSpells == null || targetItemSpells.Count < 1)
//                        {
//                            playerMsg = "The gem can only be applied to magical items";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//                        else if (targetItemSpells.Contains(3862))
//                        {
//                            playerMsg = "Your target item already has Duke Raoul's Pride on it, you cannot add it twice";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        target.Biota.GetOrAddKnownSpell(3862, target.BiotaDatabaseLock, out _);
//                        playerMsg = $"With a steady hand you skillfully apply the morph gem to your {target.NameWithMaterial} and have successfully added the spell Duke Raoul's Pride";
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, 490332); // MorphGemWillpowerbeer
//                        break;
                    #endregion MorphGemWillpowerbeer

                    #region MorphGemHeroicMaster
//                    case 1548800: // MorphGemHeroicMaster
//
//                        if (target.ItemType != ItemType.Jewelry)
//                        {
//                            playerMsg = $"{source.Name} can only be applied to jewelry.";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (GetMorphGemLogCount(target, MorphGemHeroicMaster) > 0)
//                        {
//                            playerMsg = $"{source.Name} can only be applied once and has already been applied to your target item.";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        int spellRoll = ThreadSafeRandom.Next(0, 100);
//                        int spellCount = 1;
//                        if (spellRoll > 66) spellCount = 2;
//                        if (spellRoll > 96) spellCount = 3;
//
//                        var spellList = HeroicMasterSpells.OrderBy(x => Guid.NewGuid()).Take(spellCount);
//
//                        var spellNames = new List<string>();
//                        foreach (var heroicSpellId in spellList)
//                        {
//                            target.Biota.GetOrAddKnownSpell(heroicSpellId, target.BiotaDatabaseLock, out _);
//                            spellNames.Add(new Spell(heroicSpellId).Name);
//                        }
//
//                        playerMsg = $"With a steady hand you skillfully apply the {source.Name} to your {target.NameWithMaterial} and have successfully added the following spells\n{String.Join('\n', spellNames)}";
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, 1548800); // MorphGemHeroicMaster
//                        break;
                    #endregion MorphGemHeroicMaster

                    #region MorphGemRandomCantrip
                    case MorphGemRandomCantrip:

                        if (target.WeenieClassId == 8142017u)
                        {
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        if ((target.ItemType != ItemType.Jewelry &&
                            target.ItemType != ItemType.Armor &&
                            target.ItemType != ItemType.Clothing &&
                            !target.IsShield)
                            || (target.ValidLocations?.HasFlag(EquipMask.Cloak) ?? false))
                        {
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The {source.Name} can only be applied to armor, jewelry or underclothes", ChatMessageType.Broadcast));
                            return;
                        }

                        if (GetMorphGemLogCount(target, MorphGemRandomCantrip) > 0)
                        {
                            playerMsg = $"{source.Name} can only be applied once and has already been applied to your target item.";
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            return;
                        }

                        var itemMajors = target.MajorCantrips;
                        if (itemMajors == null || itemMajors.Count < 1)
                        {
                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                            player.Session.Network.EnqueueSend(new GameMessageSystemChat("The target item has no Major cantrips to randomize", ChatMessageType.Broadcast));
                            return;
                        }

                        List<int> newMajorList = new List<int>();
                        foreach (var currMajor in itemMajors)
                        {
                            var counter = 0;
                            while (counter < 20)
                            {
                                SpellId newCantrip = ArmorCantrips.Roll(target.IsShield);

                                if (target.ItemType == ItemType.Jewelry)
                                    newCantrip = JewelryCantrips.Roll();

                                List<SpellId> progression = SpellLevelProgression.GetSpellLevels(newCantrip);

                                if (progression != null && progression.Count >= 2)
                                {
                                    int newMajorSpellId = (int)progression[1];
                                    if (newMajorSpellId != currMajor.Key && !newMajorList.Contains(newMajorSpellId))
                                    {
                                        newMajorList.Add(newMajorSpellId);
                                        break;
                                    }
                                }
                                counter++;
                            }
                        }

                        if (newMajorList.Count > 1)
                        {
                            var majRandom = new Random();
                            var majRandomRoll = majRandom.Next(0, int.MaxValue);
                            if (majRandomRoll % 15 == 0 && newMajorList.Count > 0)
                                newMajorList.RemoveAt(0);
                        }

                        if (newMajorList.Count < 4)
                        {
                            var majRandom = new Random();
                            var majRandomRoll = majRandom.Next(0, int.MaxValue);
                            if (majRandomRoll % 10 == 0 && newMajorList.Count > 0)
                            {
                                while (true)
                                {
                                    SpellId newCantrip = ArmorCantrips.Roll(target.IsShield);
                                    List<SpellId> progression = SpellLevelProgression.GetSpellLevels(newCantrip);
                                    if (progression != null && progression.Count >= 2)
                                    {
                                        int newMajorSpellId = (int)progression[1];
                                        if (!newMajorList.Contains(newMajorSpellId))
                                        {
                                            newMajorList.Add(newMajorSpellId);
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        bool cantripImpenSuccess = false;
                        if (target.ItemType != ItemType.Jewelry)
                        {
                            var majorImpen = (int)SpellId.CANTRIPIMPENETRABILITY2; // 2592
                            var minorImpen = (int)SpellId.CANTRIPIMPENETRABILITY1; // 2604

                            // Don't stack Impenetrability: skip if the item will already have the major
                            // (among the rerolled majors) or already has the minor (among its minors)
                            var alreadyHasImpen = newMajorList.Contains(majorImpen)
                                                  || (target.MinorCantrips?.ContainsKey(minorImpen) ?? false);

                            if (!alreadyHasImpen)
                            {
                                var impenRandom = new Random();

                                // 10% overall chance to add an Impenetrability cantrip
                                if (impenRandom.Next(0, 100) < 10)
                                {
                                    // major:minor split of 1:2 (one third major, two thirds minor)
                                    if (impenRandom.Next(0, 3) == 0)
                                    {
                                        if (newMajorList.Count < 4)
                                            newMajorList.Add(majorImpen);
                                        else
                                            newMajorList[0] = majorImpen;
                                    }
                                    else
                                    {
                                        target.Biota.GetOrAddKnownSpell(minorImpen, target.BiotaDatabaseLock, out _);
                                    }

                                    cantripImpenSuccess = true;
                                }
                            }
                        }

                        string removedSpellList = "";
                        int removedMajorNum = 0;
                        foreach (var spell in itemMajors)
                        {
                            target.Biota.TryRemoveKnownSpell(spell.Key, target.BiotaDatabaseLock);
                            removedMajorNum++;
                            if (removedMajorNum == 1)
                                removedSpellList = $"{new Spell(spell.Key, true).Name}";
                            else if (removedMajorNum == itemMajors.Count)
                                removedSpellList += $" and {new Spell(spell.Key, true).Name}";
                            else
                                removedSpellList += $", {new Spell(spell.Key, true).Name}";
                        }

                        string addedSpellList = "";
                        int addedMajorNum = 0;
                        foreach (var cantripSpellId in newMajorList)
                        {
                            target.Biota.GetOrAddKnownSpell(cantripSpellId, target.BiotaDatabaseLock, out _);
                            addedMajorNum++;
                            if (addedMajorNum == 1)
                                addedSpellList = $"{new Spell(cantripSpellId, true).Name}";
                            else if (addedMajorNum == newMajorList.Count)
                                addedSpellList += $" and {new Spell(cantripSpellId, true).Name}";
                            else
                                addedSpellList += $", {new Spell(cantripSpellId, true).Name}";
                        }

                        string cantripImpenMessage = cantripImpenSuccess ? "\n\nYour armor also somehow looks tougher, like it might have once been worn by some kind of tough guy and his tough guy essence sort of rubbed off on it and now it's more tough than it was before." : "";

                        string randomizeResultMsg = $"Staring into the morph gem intently, your head swims at the chaos within it.  As you slump to the ground you scream in silence at the realization that eternity is boundless and upon you; upon us all.  You smash the morph gem hard against your armor and it explodes into everything and nothing.  Washed away are the Major enchantments that once took hold.\n\nThe spells {removedSpellList} are no longer.\n\nIn their place, the spells {addedSpellList} have been cast upon your armor.{cantripImpenMessage}";
                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(randomizeResultMsg, ChatMessageType.Broadcast));
                        AddMorphGemLog(target, MorphGemRandomCantrip);
                        break;

                    #endregion MorphGemRandomCantrip

                    #region MorphGemBurden
//                    case 1548804: // MorphGemBurden
//
//                        if (!target.EncumbranceVal.HasValue)
//                        {
//                            playerMsg = $"{source.Name} can only be applied to items that have an encumbrance.";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (target.EncumbranceVal.Value < -999)
//                        {
//                            playerMsg = $"Your {target.NameWithMaterial} has already reached the minimum amount of encumbrance.";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (GetMorphGemLogCount(target, MorphGemBurden) > 2)
//                        {
//                            playerMsg = $"{source.Name} can only be applied to an item three times and your target item has reached this maximum.";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        int encumbranceRoll;
//                        if (target.EncumbranceVal >= 1000)
//                            encumbranceRoll = ThreadSafeRandom.Next(100, 650);
//                        else if (target.EncumbranceVal >= 500)
//                            encumbranceRoll = ThreadSafeRandom.Next(75, 420);
//                        else if (target.EncumbranceVal > 0)
//                            encumbranceRoll = ThreadSafeRandom.Next(50, 333);
//                        else
//                            encumbranceRoll = ThreadSafeRandom.Next(10, 333);
//
//                        if (target.EncumbranceVal.Value - encumbranceRoll < -1000)
//                            encumbranceRoll = 1000 + target.EncumbranceVal.Value;
//
//                        target.EncumbranceVal = target.EncumbranceVal - encumbranceRoll;
//
//                        playerMsg = $"With a steady hand you skillfully apply the {source.Name} to your {target.NameWithMaterial} and have successfully reduced its encumbrance by {encumbranceRoll}";
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, 1548804); // MorphGemBurden
//                        break;
                    #endregion MorphGemBurden

                    #region MorphGemRareDmgBoost
//                    case 1548805: // MorphGemRareDmgBoost
//
//                        if (!(target.ItemType == ItemType.Armor || target.ItemType == ItemType.Jewelry || target.ItemType == ItemType.Clothing))
//                        {
//                            playerMsg = $"The {source.Name} can only be applied to armor, clothing or jewelry";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (targetItemSpells == null || targetItemSpells.Count < 1)
//                        {
//                            playerMsg = "The gem can only be applied to magical items";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//                        else if (targetItemSpells.Contains(5978))
//                        {
//                            playerMsg = "Your target item already has Rare Damage Boost V on it, you cannot add it twice";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        target.Biota.GetOrAddKnownSpell(5978, target.BiotaDatabaseLock, out _);
//                        playerMsg = $"With a steady hand you skillfully apply the {source.Name} to your {target.NameWithMaterial} and have successfully added the spell Rare Damage Boost V";
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, 1548805); // MorphGemRareDmgBoost
//                        break;
                    #endregion MorphGemRareDmgBoost

                    #region MorphGemRareDmgReduction
//                    case 1548806: // MorphGemRareDmgReduction
//
//                        if (!(target.ItemType == ItemType.Armor || target.ItemType == ItemType.Jewelry || target.ItemType == ItemType.Clothing))
//                        {
//                            playerMsg = $"The {source.Name} can only be applied to armor, clothing or jewelry";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (targetItemSpells == null || targetItemSpells.Count < 1)
//                        {
//                            playerMsg = "The gem can only be applied to magical items";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//                        else if (targetItemSpells.Contains(5192))
//                        {
//                            playerMsg = "Your target item already has Rare Damage Reduction V on it, you cannot add it twice";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        target.Biota.GetOrAddKnownSpell(5192, target.BiotaDatabaseLock, out _);
//                        playerMsg = $"With a steady hand you skillfully apply the {source.Name} to your {target.NameWithMaterial} and have successfully added the spell Rare Damage Reduction V";
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, 1548806); // MorphGemRareDmgReduction
//                        break;
                    #endregion MorphGemRareDmgReduction

                    #region MorphGemMeleeCleave
//                    case 490512: // MorphGemMeleeCleave
//
//                        if (target.ItemType != ItemType.MeleeWeapon)
//                        {
//                            playerMsg = "This gem can only be used on melee weapons";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        int currCleave = target.GetProperty(PropertyInt.Cleaving) ?? 0;
//
//                        if (currCleave >= 3)
//                        {
//                            playerMsg = $"Your {target.NameWithMaterial} already has the maximum number of cleave targets and thus the gem would have no effect";
//                            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
//                            return;
//                        }
//
//                        if (currCleave < 2)
//                            target.SetProperty(PropertyInt.Cleaving, 2);
//                        else
//                            target.SetProperty(PropertyInt.Cleaving, 3);
//
//                        playerMsg = $"You have successfully used the {source.Name} on your {target.NameWithMaterial} to increase its melee cleaving targets to {target.CleaveTargets + 1}!";
//                        player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
//                        AddMorphGemLog(target, 490512); // MorphGemMeleeCleave
//                        break;
                    #endregion MorphGemMeleeCleave

                    default:
                        player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                        return;
                }

                player.TryConsumeFromInventoryWithNetworking(source, 1);
                target.SaveBiotaToDatabase();
                player.SendUseDoneEvent();
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Exception in MorphGem.ApplyMorphGem. Ex: {0}", ex);
            }
        }

        private static bool ApplyMorphGem_RareLegendaryCantrip(Player player, WorldObject source, WorldObject target, int spellId, List<int> targetItemSpells)
        {
            string playerMsg = "";

            var spell = new Spell(spellId);
            if (spell == null)
                return false;

            if (target.ItemType == ItemType.Jewelry && (spell.Name.Contains(" Bane", StringComparison.OrdinalIgnoreCase) || spell.Name.Contains(" Impenitrability")))
            {
                playerMsg = "The gem can only be applied to armor or clothing";
                player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                return false;
            }
            else if (!(target.ItemType == ItemType.Armor || target.ItemType == ItemType.Jewelry || target.ItemType == ItemType.Clothing))
            {
                playerMsg = "The gem can only be applied to armor, clothing or jewelry";
                player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                return false;
            }

            if (targetItemSpells == null || targetItemSpells.Count < 1)
            {
                playerMsg = "The gem can only be applied to magical items";
                player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                return false;
            }
            else if (targetItemSpells.Contains(spellId))
            {
                playerMsg = $"Your target item already has {spell.Name} on it, you cannot add it twice";
                player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
                player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                return false;
            }

            RemoveAllCantripsInProgression(target, spellId);
            target.Biota.GetOrAddKnownSpell(spellId, target.BiotaDatabaseLock, out _);
            playerMsg = $"With a steady hand you skillfully apply the morph gem to your {target.NameWithMaterial} and have successfully added the spell {spell.Name}";
            player.Session.Network.EnqueueSend(new GameMessageSystemChat(playerMsg, ChatMessageType.Broadcast));
            AddMorphGemLog(target, source.WeenieClassId);
            return true;
        }

        private static void RemoveAllCantripsInProgression(WorldObject target, int spellId)
        {
            var progression = SpellLevelProgression.GetSpellLevels((SpellId)spellId);
            if (progression != null)
            {
                foreach (var progressionSpellId in progression)
                    target.Biota.TryRemoveKnownSpell((int)progressionSpellId, target.BiotaDatabaseLock);
            }
        }

        /// <summary>
        /// Removes any skill-based wield requirement (Skill / RawSkill / Training) on the target
        /// that references the given skill, across all four wield requirement slots.
        /// Returns true if at least one matching requirement was cleared.
        /// </summary>
        private static bool RemoveWieldRequirementForSkill(WorldObject target, Skill skill)
        {
            var removedAny = false;

            void ClearSlot(WieldRequirement req, int? skillType, Action clear)
            {
                if (skillType != (int)skill)
                    return;

                if (req != WieldRequirement.Skill && req != WieldRequirement.RawSkill && req != WieldRequirement.Training)
                    return;

                clear();
                removedAny = true;
            }

            ClearSlot(target.WieldRequirements, target.WieldSkillType, () =>
            {
                target.WieldRequirements = WieldRequirement.Invalid;
                target.WieldSkillType = null;
                target.WieldDifficulty = null;
            });

            ClearSlot(target.WieldRequirements2, target.WieldSkillType2, () =>
            {
                target.WieldRequirements2 = WieldRequirement.Invalid;
                target.WieldSkillType2 = null;
                target.WieldDifficulty2 = null;
            });

            ClearSlot(target.WieldRequirements3, target.WieldSkillType3, () =>
            {
                target.WieldRequirements3 = WieldRequirement.Invalid;
                target.WieldSkillType3 = null;
                target.WieldDifficulty3 = null;
            });

            ClearSlot(target.WieldRequirements4, target.WieldSkillType4, () =>
            {
                target.WieldRequirements4 = WieldRequirement.Invalid;
                target.WieldSkillType4 = null;
                target.WieldDifficulty4 = null;
            });

            return removedAny;
        }

        /// <summary>
        /// Returns true if any of the target's four wield requirement slots is a heritage requirement.
        /// </summary>
        private static bool HasHeritageWieldRequirement(WorldObject target)
        {
            return target.WieldRequirements == WieldRequirement.HeritageType
                || target.WieldRequirements2 == WieldRequirement.HeritageType
                || target.WieldRequirements3 == WieldRequirement.HeritageType
                || target.WieldRequirements4 == WieldRequirement.HeritageType;
        }

        /// <summary>
        /// Returns the heritage required by the target's first heritage wield requirement slot,
        /// or Invalid if it has none. The heritage is stored in the slot's WieldDifficulty.
        /// </summary>
        private static HeritageGroup GetHeritageWieldRequirement(WorldObject target)
        {
            if (target.WieldRequirements == WieldRequirement.HeritageType)
                return (HeritageGroup)(target.WieldDifficulty ?? 0);

            if (target.WieldRequirements2 == WieldRequirement.HeritageType)
                return (HeritageGroup)(target.WieldDifficulty2 ?? 0);

            if (target.WieldRequirements3 == WieldRequirement.HeritageType)
                return (HeritageGroup)(target.WieldDifficulty3 ?? 0);

            if (target.WieldRequirements4 == WieldRequirement.HeritageType)
                return (HeritageGroup)(target.WieldDifficulty4 ?? 0);

            return HeritageGroup.Invalid;
        }

        /// <summary>
        /// Removes any heritage-based wield requirement on the target, across all four wield requirement slots.
        /// </summary>
        private static void RemoveHeritageWieldRequirement(WorldObject target)
        {
            if (target.WieldRequirements == WieldRequirement.HeritageType)
            {
                target.WieldRequirements = WieldRequirement.Invalid;
                target.WieldSkillType = null;
                target.WieldDifficulty = null;
            }

            if (target.WieldRequirements2 == WieldRequirement.HeritageType)
            {
                target.WieldRequirements2 = WieldRequirement.Invalid;
                target.WieldSkillType2 = null;
                target.WieldDifficulty2 = null;
            }

            if (target.WieldRequirements3 == WieldRequirement.HeritageType)
            {
                target.WieldRequirements3 = WieldRequirement.Invalid;
                target.WieldSkillType3 = null;
                target.WieldDifficulty3 = null;
            }

            if (target.WieldRequirements4 == WieldRequirement.HeritageType)
            {
                target.WieldRequirements4 = WieldRequirement.Invalid;
                target.WieldSkillType4 = null;
                target.WieldDifficulty4 = null;
            }
        }

        #region Morph Gem Log
        public static void AddMorphGemLog(WorldObject target, uint gemWeenieId)
        {
            if (!string.IsNullOrEmpty(target.MorphGemLog))
                target.MorphGemLog += ",";

            target.MorphGemLog += gemWeenieId;
        }

        public static int GetMorphGemLogCount(WorldObject target, uint gemWeenieId)
        {
            if (string.IsNullOrEmpty(target.MorphGemLog))
                return 0;

            var logEntries = target.MorphGemLog.Split(',');
            var matchingLogEntries = logEntries.Where(x => x.Equals(gemWeenieId.ToString()));
            return matchingLogEntries?.Count() ?? 0;
        }
        #endregion Morph Gem Log
    }
}
