using System;
using System.Collections.Generic;

using log4net;

using ACE.Common;
using ACE.Common.Extensions;
using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using System.Linq;
using System.Text;
using ACE.Entity.Enum.Properties;
using ACE.Database.Models.Shard;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity.Models;
using ACE.Server.Physics.Common;

namespace ACE.Server.Command.Handlers
{
    public static class PlayerCommands
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // pop
        [CommandHandler("pop", AccessLevel.Player, CommandHandlerFlag.None, 0,
            "Show current world population",
            "")]
        public static void HandlePop(Session session, params string[] parameters)
        {
            if (!CheckPlayerCommandRateLimit(session, 1))
                return;

            ShowPop(session);
        }

        public static void ShowPop(Session session, ulong discordChannel = 0)
        {
            var showCurrent = PropertyManager.GetBool("cmd_pop_show_current").Item;
            var showUnique24Hours = PropertyManager.GetBool("cmd_pop_show_24_hours").Item;
            var showUnique7Days = PropertyManager.GetBool("cmd_pop_show_7_days").Item;
            var showUnique30Days = PropertyManager.GetBool("cmd_pop_show_30_days").Item;

            if (!showCurrent && !showUnique24Hours && !showUnique7Days && !showUnique30Days)
            {
                CommandHandlerHelper.WriteOutputInfo(session, "This command has been disabled.", ChatMessageType.Broadcast);
                return;
            }

            if (showCurrent)
            {
                if (discordChannel == 0)
                    CommandHandlerHelper.WriteOutputInfo(session, $"Current world population: {PlayerManager.GetOnlineCount():N0}", ChatMessageType.Broadcast);
                else
                    DiscordChatBridge.SendMessage(discordChannel, $"Current world population: {PlayerManager.GetOnlineCount():N0}");
            }

            if (showUnique24Hours)
            {
                if (discordChannel == 0)
                    DatabaseManager.Shard.GetUniqueIPsInTheLast(TimeSpan.FromHours(24), result => CommandHandlerHelper.WriteOutputInfo(session, $"Unique IPs connected in the last 24 hours: {result:N0}", ChatMessageType.Broadcast));
                else
                    DatabaseManager.Shard.GetUniqueIPsInTheLast(TimeSpan.FromHours(24), result => DiscordChatBridge.SendMessage(discordChannel, $"Unique IPs connected in the last 24 hours: {result:N0}"));
            }

            if (showUnique7Days)
            {
                if (discordChannel == 0)
                    DatabaseManager.Shard.GetUniqueIPsInTheLast(TimeSpan.FromDays(7), result => CommandHandlerHelper.WriteOutputInfo(session, $"Unique IPs connected in the last 7 days: {result:N0}", ChatMessageType.Broadcast));
                else
                    DatabaseManager.Shard.GetUniqueIPsInTheLast(TimeSpan.FromDays(7), result => DiscordChatBridge.SendMessage(discordChannel, $"Unique IPs connected in the last 7 days: {result:N0}"));
            }

            if (showUnique30Days)
            {
                if (discordChannel == 0)
                    DatabaseManager.Shard.GetUniqueIPsInTheLast(TimeSpan.FromDays(30), result => CommandHandlerHelper.WriteOutputInfo(session, $"Unique IPs connected in the last 30 days: {result:N0}", ChatMessageType.Broadcast));
                else
                    DatabaseManager.Shard.GetUniqueIPsInTheLast(TimeSpan.FromDays(30), result => DiscordChatBridge.SendMessage(discordChannel, $"Unique IPs connected in the last 30 days: {result:N0}"));
            }
        }

        // quest info (uses GDLe formatting to match plugin expectations)
        [CommandHandler("myquests", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Shows your quest log")]
        public static void HandleQuests(Session session, params string[] parameters)
        {
            if (!CheckPlayerCommandRateLimit(session))
                return;

            if (!PropertyManager.GetBool("quest_info_enabled").Item)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat("The command \"myquests\" is not currently enabled on this server.", ChatMessageType.Broadcast));
                return;
            }

            var quests = session.Player.QuestManager.GetQuests();

            if (quests.Count == 0)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat("Quest list is empty.", ChatMessageType.Broadcast));
                return;
            }

            foreach (var playerQuest in quests)
            {
                var text = "";
                var questName = QuestManager.GetQuestName(playerQuest.QuestName);
                var quest = DatabaseManager.World.GetCachedQuest(questName);
                if (quest == null)
                {
                    Console.WriteLine($"Couldn't find quest {playerQuest.QuestName}");
                    continue;
                }

                var minDelta = quest.MinDelta;
                if (QuestManager.CanScaleQuestMinDelta(quest))
                {
                    minDelta = (uint)(quest.MinDelta * PropertyManager.GetDouble("quest_mindelta_rate").Item);

                    if (minDelta != quest.MinDelta)
                        minDelta = Math.Max(minDelta, (uint)PropertyManager.GetLong("quest_mindelta_rate_shortest", 0).Item);
                }

                text += $"{playerQuest.QuestName.ToLower()} - {playerQuest.NumTimesCompleted} solves ({playerQuest.LastTimeCompleted})";
                text += $"\"{quest.Message}\" {quest.MaxSolves} {minDelta}";

                session.Network.EnqueueSend(new GameMessageSystemChat(text, ChatMessageType.Broadcast));
            }
        }

        /// <summary>
        /// For characters/accounts who currently own multiple houses, used to select which house they want to keep
        /// </summary>
        [CommandHandler("house-select", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 1, "For characters/accounts who currently own multiple houses, used to select which house they want to keep")]
        public static void HandleHouseSelect(Session session, params string[] parameters)
        {
            if (!CheckPlayerCommandRateLimit(session))
                return;

            HandleHouseSelect(session, false, parameters);
        }

        public static void HandleHouseSelect(Session session, bool confirmed, params string[] parameters)
        {
            if (!int.TryParse(parameters[0], out var houseIdx))
                return;

            // ensure current multihouse owner
            if (!session.Player.IsMultiHouseOwner(false))
            {
                log.Warn($"{session.Player.Name} tried to /house-select {houseIdx}, but they are not currently a multi-house owner!");
                return;
            }

            // get house info for this index
            var multihouses = session.Player.GetMultiHouses();

            if (houseIdx < 1 || houseIdx > multihouses.Count)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat($"Please enter a number between 1 and {multihouses.Count}.", ChatMessageType.Broadcast));
                return;
            }

            var keepHouse = multihouses[houseIdx - 1];

            // show confirmation popup
            if (!confirmed)
            {
                var houseType = $"{keepHouse.HouseType}".ToLower();
                var loc = HouseManager.GetCoords(keepHouse.SlumLord.Location);

                var msg = $"Are you sure you want to keep the {houseType} at\n{loc}?";
                if (!session.Player.ConfirmationManager.EnqueueSend(new Confirmation_Custom(session.Player.Guid, () => HandleHouseSelect(session, true, parameters)), msg))
                    session.Player.SendWeenieError(WeenieError.ConfirmationInProgress);
                return;
            }

            // house to keep confirmed, abandon the other houses
            var abandonHouses = new List<House>(multihouses);
            abandonHouses.RemoveAt(houseIdx - 1);

            foreach (var abandonHouse in abandonHouses)
            {
                var house = session.Player.GetHouse(abandonHouse.Guid.Full);

                HouseManager.HandleEviction(house, house.HouseOwner ?? 0, true);
            }

            // set player properties for house to keep
            var player = PlayerManager.FindByGuid(keepHouse.HouseOwner ?? 0, out bool isOnline);
            if (player == null)
            {
                log.Error($"{session.Player.Name}.HandleHouseSelect({houseIdx}) - couldn't find HouseOwner {keepHouse.HouseOwner} for {keepHouse.Name} ({keepHouse.Guid})");
                return;
            }

            player.HouseId = keepHouse.HouseId;
            player.HouseInstance = keepHouse.Guid.Full;

            player.SaveBiotaToDatabase();

            // update house panel for current player
            var actionChain = new ActionChain();
            actionChain.AddDelaySeconds(3.0f);  // wait for slumlord inventory biotas above to save
            actionChain.AddAction(session.Player, session.Player.HandleActionQueryHouse);
            actionChain.EnqueueChain();

            Console.WriteLine("OK");
        }

        [CommandHandler("debugcast", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Shows debug information about the current magic casting state")]
        public static void HandleDebugCast(Session session, params string[] parameters)
        {
            if (!CheckPlayerCommandRateLimit(session))
                return;

            var physicsObj = session.Player.PhysicsObj;

            var pendingActions = physicsObj.MovementManager.MoveToManager.PendingActions;
            var currAnim = physicsObj.PartArray.Sequence.CurrAnim;

            session.Network.EnqueueSend(new GameMessageSystemChat(session.Player.MagicState.ToString(), ChatMessageType.Broadcast));
            session.Network.EnqueueSend(new GameMessageSystemChat($"IsMovingOrAnimating: {physicsObj.IsMovingOrAnimating}", ChatMessageType.Broadcast));
            session.Network.EnqueueSend(new GameMessageSystemChat($"PendingActions: {pendingActions.Count}", ChatMessageType.Broadcast));
            session.Network.EnqueueSend(new GameMessageSystemChat($"CurrAnim: {currAnim?.Value.Anim.ID:X8}", ChatMessageType.Broadcast));
        }

        [CommandHandler("fixcast", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Fixes magic casting if locked up for an extended time")]
        public static void HandleFixCast(Session session, params string[] parameters)
        {
            if (!CheckPlayerCommandRateLimit(session))
                return;

            var magicState = session.Player.MagicState;

            if (magicState.IsCasting && DateTime.UtcNow - magicState.StartTime > TimeSpan.FromSeconds(5))
            {
                session.Network.EnqueueSend(new GameEventCommunicationTransientString(session, "Fixed casting state"));
                session.Player.SendUseDoneEvent();
                magicState.OnCastDone();
            }
        }

        [CommandHandler("castmeter", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Shows the fast casting efficiency meter")]
        public static void HandleCastMeter(Session session, params string[] parameters)
        {
            if (!CheckPlayerCommandRateLimit(session))
                return;

            if (parameters.Length == 0)
            {
                session.Player.MagicState.CastMeter = !session.Player.MagicState.CastMeter;
            }
            else
            {
                if (parameters[0].Equals("on", StringComparison.OrdinalIgnoreCase))
                    session.Player.MagicState.CastMeter = true;
                else
                    session.Player.MagicState.CastMeter = false;
            }
            session.Network.EnqueueSend(new GameMessageSystemChat($"Cast efficiency meter {(session.Player.MagicState.CastMeter ? "enabled" : "disabled")}", ChatMessageType.Broadcast));
        }

        private static List<string> configList = new List<string>()
        {
            "Common settings:\nConfirmVolatileRareUse, MainPackPreferred, SalvageMultiple, SideBySideVitals, UseCraftSuccessDialog",
            "Interaction settings:\nAcceptLootPermits, AllowGive, AppearOffline, AutoAcceptFellowRequest, DragItemOnPlayerOpensSecureTrade, FellowshipShareLoot, FellowshipShareXP, IgnoreAllegianceRequests, IgnoreFellowshipRequests, IgnoreTradeRequests, UseDeception",
            "UI settings:\nCoordinatesOnRadar, DisableDistanceFog, DisableHouseRestrictionEffects, DisableMostWeatherEffects, FilterLanguage, LockUI, PersistentAtDay, ShowCloak, ShowHelm, ShowTooltips, SpellDuration, TimeStamp, ToggleRun, UseMouseTurning",
            "Chat settings:\nHearAllegianceChat, HearGeneralChat, HearLFGChat, HearRoleplayChat, HearSocietyChat, HearTradeChat, HearPKDeaths, StayInChatMode",
            "Combat settings:\nAdvancedCombatUI, AutoRepeatAttack, AutoTarget, LeadMissileTargets, UseChargeAttack, UseFastMissiles, ViewCombatTarget, VividTargetingIndicator",
            "Character display settings:\nDisplayAge, DisplayAllegianceLogonNotifications, DisplayChessRank, DisplayDateOfBirth, DisplayFishingSkill, DisplayNumberCharacterTitles, DisplayNumberDeaths"
        };

        /// <summary>
        /// Mapping of GDLE -> ACE CharacterOptions
        /// </summary>
        private static Dictionary<string, string> translateOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Common
            { "ConfirmVolatileRareUse", "ConfirmUseOfRareGems" },
            { "MainPackPreferred", "UseMainPackAsDefaultForPickingUpItems" },
            { "SalvageMultiple", "SalvageMultipleMaterialsAtOnce" },
            { "SideBySideVitals", "SideBySideVitals" },
            { "UseCraftSuccessDialog", "UseCraftingChanceOfSuccessDialog" },

            // Interaction
            { "AcceptLootPermits", "AcceptCorpseLootingPermissions" },
            { "AllowGive", "LetOtherPlayersGiveYouItems" },
            { "AppearOffline", "AppearOffline" },
            { "AutoAcceptFellowRequest", "AutomaticallyAcceptFellowshipRequests" },
            { "DragItemOnPlayerOpensSecureTrade", "DragItemToPlayerOpensTrade" },
            { "FellowshipShareLoot", "ShareFellowshipLoot" },
            { "FellowshipShareXP", "ShareFellowshipExpAndLuminance" },
            { "IgnoreAllegianceRequests", "IgnoreAllegianceRequests" },
            { "IgnoreFellowshipRequests", "IgnoreFellowshipRequests" },
            { "IgnoreTradeRequests", "IgnoreAllTradeRequests" },
            { "UseDeception", "AttemptToDeceiveOtherPlayers" },

            // UI
            { "CoordinatesOnRadar", "ShowCoordinatesByTheRadar" },
            { "DisableDistanceFog", "DisableDistanceFog" },
            { "DisableHouseRestrictionEffects", "DisableHouseRestrictionEffects" },
            { "DisableMostWeatherEffects", "DisableMostWeatherEffects" },
            { "FilterLanguage", "FilterLanguage" },
            { "LockUI", "LockUI" },
            { "PersistentAtDay", "AlwaysDaylightOutdoors" },
            { "ShowCloak", "ShowYourCloak" },
            { "ShowHelm", "ShowYourHelmOrHeadGear" },
            { "ShowTooltips", "Display3dTooltips" },
            { "SpellDuration", "DisplaySpellDurations" },
            { "TimeStamp", "DisplayTimestamps" },
            { "ToggleRun", "RunAsDefaultMovement" },
            { "UseMouseTurning", "UseMouseTurning" },

            // Chat
            { "HearAllegianceChat", "ListenToAllegianceChat" },
            { "HearGeneralChat", "ListenToGeneralChat" },
            { "HearLFGChat", "ListenToLFGChat" },
            { "HearRoleplayChat", "ListentoRoleplayChat" },
            { "HearSocietyChat", "ListenToSocietyChat" },
            { "HearTradeChat", "ListenToTradeChat" },
            { "HearPKDeaths", "ListenToPKDeathMessages" },
            { "StayInChatMode", "StayInChatModeAfterSendingMessage" },

            // Combat
            { "AdvancedCombatUI", "AdvancedCombatInterface" },
            { "AutoRepeatAttack", "AutoRepeatAttacks" },
            { "AutoTarget", "AutoTarget" },
            { "LeadMissileTargets", "LeadMissileTargets" },
            { "UseChargeAttack", "UseChargeAttack" },
            { "UseFastMissiles", "UseFastMissiles" },
            { "ViewCombatTarget", "KeepCombatTargetsInView" },
            { "VividTargetingIndicator", "VividTargetingIndicator" },

            // Character Display
            { "DisplayAge", "AllowOthersToSeeYourAge" },
            { "DisplayAllegianceLogonNotifications", "ShowAllegianceLogons" },
            { "DisplayChessRank", "AllowOthersToSeeYourChessRank" },
            { "DisplayDateOfBirth", "AllowOthersToSeeYourDateOfBirth" },
            { "DisplayFishingSkill", "AllowOthersToSeeYourFishingSkill" },
            { "DisplayNumberCharacterTitles", "AllowOthersToSeeYourNumberOfTitles" },
            { "DisplayNumberDeaths", "AllowOthersToSeeYourNumberOfDeaths" },
        };

        /// <summary>
        /// Manually sets a character option on the server. Use /config list to see a list of settings.
        /// </summary>
        [CommandHandler("config", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 1, "Manually sets a character option on the server.\nUse /config list to see a list of settings.", "<setting> <on/off>")]
        public static void HandleConfig(Session session, params string[] parameters)
        {
            if (!CheckPlayerCommandRateLimit(session))
                return;

            if (!PropertyManager.GetBool("player_config_command").Item)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat("The command \"config\" is not currently enabled on this server.", ChatMessageType.Broadcast));
                return;
            }

            // /config list - show character options
            if (parameters[0].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var line in configList)
                    session.Network.EnqueueSend(new GameMessageSystemChat(line, ChatMessageType.Broadcast));

                return;
            }

            // translate GDLE CharacterOptions for existing plugins
            if (!translateOptions.TryGetValue(parameters[0], out var param) || !Enum.TryParse(param, out CharacterOption characterOption))
            {
                session.Network.EnqueueSend(new GameMessageSystemChat($"Unknown character option: {parameters[0]}", ChatMessageType.Broadcast));
                return;
            }

            var option = session.Player.GetCharacterOption(characterOption);

            // modes of operation:
            // on / off / toggle

            // - if none specified, default to toggle
            var mode = "toggle";

            if (parameters.Length > 1)
            {
                if (parameters[1].Equals("on", StringComparison.OrdinalIgnoreCase))
                    mode = "on";
                else if (parameters[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                    mode = "off";
            }

            // set character option
            if (mode.Equals("on"))
                option = true;
            else if (mode.Equals("off"))
                option = false;
            else
                option = !option;

            session.Player.SetCharacterOption(characterOption, option);

            session.Network.EnqueueSend(new GameMessageSystemChat($"Character option {parameters[0]} is now {(option ? "on" : "off")}.", ChatMessageType.Broadcast));

            // update client
            session.Network.EnqueueSend(new GameEventPlayerDescription(session));
        }

        /// <summary>
        /// Force resend of all visible objects known to this player. Can fix rare cases of invisible object bugs.
        /// Can only be used once every 5 mins max.
        /// </summary>
        [CommandHandler("objsend", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Force resend of all visible objects known to this player. Can fix rare cases of invisible object bugs. Can only be used once every 5 mins max.")]
        public static void HandleObjSend(Session session, params string[] parameters)
        {
            // a good repro spot for this is the first room after the door in facility hub
            // in the portal drop / staircase room, the VisibleCells do not have the room after the door
            // however, the room after the door *does* have the portal drop / staircase room in its VisibleCells (the inverse relationship is imbalanced)
            // not sure how to fix this atm, seems like it triggers a client bug..

            if (DateTime.UtcNow - session.Player.PrevObjSend < TimeSpan.FromMinutes(5))
            {
                session.Player.SendTransientError("You have used this command too recently!");
                return;
            }

            var creaturesOnly = parameters.Length > 0 && parameters[0].Contains("creature", StringComparison.OrdinalIgnoreCase);

            var knownObjs = session.Player.GetKnownObjects();

            foreach (var knownObj in knownObjs)
            {
                if (creaturesOnly && !(knownObj is Creature))
                    continue;

                session.Player.RemoveTrackedObject(knownObj, false);
                session.Player.TrackObject(knownObj);
            }
            session.Player.PrevObjSend = DateTime.UtcNow;
        }

        // show player ace server versions
        [CommandHandler("aceversion", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Shows this server's version data")]
        public static void HandleACEversion(Session session, params string[] parameters)
        {
            if (!CheckPlayerCommandRateLimit(session))
                return;

            if (!PropertyManager.GetBool("version_info_enabled").Item)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat("The command \"aceversion\" is not currently enabled on this server.", ChatMessageType.Broadcast));
                return;
            }

            var msg = ServerBuildInfo.GetVersionInfo();

            session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.WorldBroadcast));
        }

        // reportbug < code | content > < description >
        [CommandHandler("reportbug", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 2,
            "Generate a Bug Report",
            "<category> <description>\n" +
            "This command generates a URL for you to copy and paste into your web browser to submit for review by server operators and developers.\n" +
            "Category can be the following:\n" +
            "Creature\n" +
            "NPC\n" +
            "Item\n" +
            "Quest\n" +
            "Recipe\n" +
            "Landblock\n" +
            "Mechanic\n" +
            "Code\n" +
            "Other\n" +
            "For the first three options, the bug report will include identifiers for what you currently have selected/targeted.\n" +
            "After category, please include a brief description of the issue, which you can further detail in the report on the website.\n" +
            "Examples:\n" +
            "/reportbug creature Drudge Prowler is over powered\n" +
            "/reportbug npc Ulgrim doesn't know what to do with Sake\n" +
            "/reportbug quest I can't enter the portal to the Lost City of Frore\n" +
            "/reportbug recipe I cannot combine Bundle of Arrowheads with Bundle of Arrowshafts\n" +
            "/reportbug code I was killed by a Non-Player Killer\n"
            )]
        public static void HandleReportbug(Session session, params string[] parameters)
        {
            if (!PropertyManager.GetBool("reportbug_enabled").Item)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat("The command \"reportbug\" is not currently enabled on this server.", ChatMessageType.Broadcast));
                return;
            }

            var category = parameters[0];
            var description = "";

            for (var i = 1; i < parameters.Length; i++)
                description += parameters[i] + " ";

            description.Trim();

            switch (category.ToLower())
            {
                case "creature":
                case "npc":
                case "quest":
                case "item":
                case "recipe":
                case "landblock":
                case "mechanic":
                case "code":
                case "other":
                    break;
                default:
                    category = "Other";
                    break;
            }

            var sn = ConfigManager.Config.Server.WorldName;
            var c = session.Player.Name;

            var st = "ACE";

            //var versions = ServerBuildInfo.GetVersionInfo();
            var databaseVersion = DatabaseManager.World.GetVersion();
            var sv = ServerBuildInfo.FullVersion;
            var pv = databaseVersion.PatchVersion;

            //var ct = PropertyManager.GetString("reportbug_content_type").Item;
            var cg = category.ToLower();

            var w = "";
            var g = "";

            if (cg == "creature" || cg == "npc" || cg == "item" || cg == "item")
            {
                var objectId = new ObjectGuid();
                if (session.Player.HealthQueryTarget.HasValue || session.Player.ManaQueryTarget.HasValue || session.Player.CurrentAppraisalTarget.HasValue)
                {
                    if (session.Player.HealthQueryTarget.HasValue)
                        objectId = new ObjectGuid((uint)session.Player.HealthQueryTarget);
                    else if (session.Player.ManaQueryTarget.HasValue)
                        objectId = new ObjectGuid((uint)session.Player.ManaQueryTarget);
                    else
                        objectId = new ObjectGuid((uint)session.Player.CurrentAppraisalTarget);

                    //var wo = session.Player.CurrentLandblock?.GetObject(objectId);

                    var wo = session.Player.FindObject(objectId.Full, Player.SearchLocations.Everywhere);

                    if (wo != null)
                    {
                        w = $"{wo.WeenieClassId}";
                        g = $"0x{wo.Guid:X8}";
                    }
                }
            }

            var l = session.Player.Location.ToLOCString();

            var issue = description;

            var urlbase = $"https://www.accpp.net/bug?";

            var url = urlbase;
            if (sn.Length > 0)
                url += $"sn={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sn))}";
            if (c.Length > 0)
                url += $"&c={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(c))}";
            if (st.Length > 0)
                url += $"&st={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(st))}";
            if (sv.Length > 0)
                url += $"&sv={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sv))}";
            if (pv.Length > 0)
                url += $"&pv={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(pv))}";
            //if (ct.Length > 0)
            //    url += $"&ct={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ct))}";
            if (cg.Length > 0)
            {
                if (cg == "npc")
                    cg = cg.ToUpper();
                else
                    cg = char.ToUpper(cg[0]) + cg.Substring(1);
                url += $"&cg={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(cg))}";
            }
            if (w.Length > 0)
                url += $"&w={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(w))}";
            if (g.Length > 0)
                url += $"&g={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(g))}";
            if (l.Length > 0)
                url += $"&l={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(l))}";
            if (issue.Length > 0)
                url += $"&i={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(issue))}";

            var msg = "\n\n\n\n";
            msg += "Bug Report - Copy and Paste the following URL into your browser to submit a bug report\n";
            msg += "-=-\n";
            msg += $"{url}\n";
            msg += "-=-\n";
            msg += "\n\n\n\n";

            session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.AdminTell));
        }

        static List<ActivityRecommendation> Recommendations = new List<ActivityRecommendation>()
        {
            // General
            new ActivityRecommendation(10, 80, new HashSet<Skill>{Skill.Axe, Skill.Dagger, Skill.Mace, Skill.Spear, Skill.Staff, Skill.Sword, Skill.UnarmedCombat}, "Equipment: Hunt Golems for Motes to craft Atlan and Isparian Weapons at the Crater Lake Village."),
            new ActivityRecommendation(10, 80, new HashSet<Skill>{Skill.Bow, Skill.Crossbow, Skill.ThrownWeapon, Skill.WarMagic, Skill.LifeMagic}, "Equipment: Hunt Golems for Motes to craft Isparian Weapons at the Crater Lake Village."),
            new ActivityRecommendation(15, 126, Skill.Lockpick, "XP: Hunt Undead for Mnemosynes. Unlock them with keys made using Lockpicking from Golem Hearts, and turn them in at the Mnemosyne Collection Site near Samsur at 2.5S, 16.4E."),
            new ActivityRecommendation(15, 126, Skill.Lockpick, "Equipment: Hunt Undead for Mnemosynes. Unlock them with keys made using Lockpicking from Golem Hearts, and turn them in at the Undead Hunter's tent near Tufa at 13.3S, 5.1E."),
            new ActivityRecommendation(15, 126, Skill.Armor, "Equipment: Hunt Shadows and Crystals for shards to craft Shadow Armor near Eastham at 18.5N, 62.8E, near Al-Jalima at 7.1N, 3.0E or near Kara at 82.9S, 46.0E."),
            new ActivityRecommendation(10, 126, "Hunting Grounds: Hunt Olthois in the Olthoi Arcade near Redspire at 39.1N 81.2W."),
            new ActivityRecommendation(20, 126, "GaerlanDefeated" , "Fellowship - Equipment: Explore Gaerlan's Citadel and recover Olthoi Slayer equipment."),

            // T1
            new ActivityRecommendation(1, 1, "RecruitSent", "XP/Equipment: Go through the Training Academy Portal and complete the tutorial for equipment and experience."),
            new ActivityRecommendation(2, 15, "Equipment: Collect Red and Gold Letters, gather stamps and trade them in for Exploration Society Equipment. For more information talk to Exploration Society Agents, usually located in taverns wearing green clothes."),

            //new ActivityRecommendation(2, 5, new HashSet<string>{ "RITHWICMINDORLALETTER"}, "XP: Visit Rithwic East and help Mindorla with an errand."),
            //new ActivityRecommendation(2, 5, new HashSet<string>{ "RITHWICCELCYNDRING"}, "XP: Recover a letter from the Old Warehouse near Rithwic at 7.6N, 58.4E and deliver it to Celcynd the Dour in Rithwic."),

            new ActivityRecommendation(2, 8, new HashSet<string>{ "HoltburgAfrinCorn1204", "HoltburgAfrinRye1204", "HoltburgAfrinWheat1204","HoltburgAfrinDrudge1204"}, "XP: Recover Stolen Supplies from the Drudge Hideout near Holtburg at 41.3N, 33.3E and deliver them to Alfrin in Holtburg."),
            new ActivityRecommendation(2, 8, new HashSet<string>{ "AxeBrogordQuest", "HoltburgNoteBrogord1204"}, "XP: Recover Brogord's Axe and a Letter to Ryndya from the Cave of Alabree near Holtburg at 41.8N, 32.1E and deliver them to Flinrala Ryndmad in Holtburg."),
            new ActivityRecommendation(2, 8, new HashSet<string>{ "HoltburgRedoubtCandlestick1204", "HoltburgRedoubtBowl1204", "AntiquePlatterQuest", "HoltburgRedoubtLamp1204","HoltburgRedoubtHandbell1204","HardunnaBandQuest","HoltburgRedoubtMug1204","HoltburgRedoubtGoblet1204"}, "XP: Recover Heirlooms from the Holtburg Redoubt near Holtburg at 40.4N, 34.4E and return them to Worcer in Holtburg."),

            new ActivityRecommendation(2, 8, new HashSet<string>{ "YaojiLouKaQuest", "ShoushiBraidBracelet1204", "ShoushiBraidKatar1204", "ShoushiBraidNecklace1204", "ShoushiBraidRing1204", "ShoushiBraidShuriken1204", "ShoushiBraidTrident1204"}, "XP: Recover Lou Ka's Stolen Items from Braid Mansion Ruin just outside of Shoushi, at 34.2S 72.0E and return them to Lou Ka in the bar in Shoushi."),
            new ActivityRecommendation(2, 8, new HashSet<string>{ "ShoushiNenAiCheese1204", "ShoushiNenAiCider1204"}, "XP: Help Nen Ai feed her pet drudge at 34.8S, 71.2E near Shoushi."),
            new ActivityRecommendation(2, 8, new HashSet<string>{ "ShoushiStoneCompassion", "ShoushiStoneDetachment", "ShoushiStoneDiscipline1204", "ShoushiStoneHumility1204"}, "XP: Recover the four Stones of Jojii from the Shreth Hive at 32.4S, 71.0E near Shoushi and bring them to Oi-Tong Ye in Shoushi."),

            new ActivityRecommendation(2, 8, new HashSet<string>{ "PerfectlyAgedCoveCiderQuest", "YaraqAppleCovePerfect1204", "YaraqApplePieHot1204", "YaraqBakingPanCoveApple1204", "YaraqCiderCoveAppleAged1204", "YaraqCiderHardCoveApple1204", "YaraqKnifeCoveApple1204", "YaraqWineCoveApple1204"}, "XP: Retrieve Lubziklan al-Luq stolen items from the Sea Temple Catacombs at 20.2S, 4.4W near Yaraq and return them to Lubziklan al-Luq at 22.4S, 1.9W near Yaraq."),
            new ActivityRecommendation(2, 8, new HashSet<string>{ "YaraqNasunLetter", "YaraqAhyaraLetter" }, "XP: Help Nasun ibn Tifar and Ahyara by delivering some letters, they can be found in the North Yaraq Outpost and the East Yaraq Outpost respectively."),
            new ActivityRecommendation(2, 8, new HashSet<string>{ "NoteDrudgeScrawledPickup", "YaraqHeadMarionetteMadStar1204" }, "XP: Help Ma'yad ibn Ibsar locate her missing brother and investigate the mystery of the Mad Star. Meet her at the Cerulean Cove Pub in Yaraq."),

            new ActivityRecommendation(2, 8, "HeaToneawaCompleted", "XP: Deliver a love letter for Hea Toneawa at 43.7N 66.9W near Greenspire."),

            new ActivityRecommendation(8, 15, "OlthoiHunting1", "XP: Kill Olthois in the Abandoned Tumerok Site near Redspire at 42.0N, 82.2W and bring a Harvester Pincer to Behdo Yii in Redspire."),
            new ActivityRecommendation(12, 20, "OlthoiHunting2", "XP: Kill Olthois in the Dark Lair near Greenspire at 43.8N, 68.4W and bring a Gardener Pincer to Behdo Yii in Redspire."),

            new ActivityRecommendation(5, 10, Skill.Shield, "Equipment: Explore Eastham Sewer near Eastham at 18.7N, 63.4E for the Metal Round Shield."),

            new ActivityRecommendation(10, 20, Skill.Axe, new HashSet<string>{"BanderlingMaceShaft", "BanderlingMaceHead"}, "Equipment: Explore Banderling Conquest near Sawato at 29.0S, 50.5E for the Banderling Mace Shaft and Mosswart Maze near Al-Arqas at 25.2S, 19.4E for Banderling Mace Head and bring them to Olivier Rognath in Eastham for the Mace of the Explorer."),
            new ActivityRecommendation(10, 20, Skill.Axe, "CrimsonBrokenHaft", "Equipment: Reclaim the Silifi of Crimson Stars, start by exploring Leikotha's Crypt at 10.1S 31.3E and then visit Kayna bint Iswas at 1.7S, 36.6E."),
            new ActivityRecommendation(12, 20, Skill.Axe, "TumerokVanguardMorningstar", "Equipment: Explore the North Tumerok Vanguard Outpost near Tufa at 7.5S, 0.0W for the Vanguard Leader's Morningstar and Vanguard Leader's Amulet."),
            new ActivityRecommendation(12, 20, Skill.Bow, "TumerokVanguardCrossbow", "Equipment: Explore the South Tumerok Vanguard Outpost near Khayyaban at 45.9S, 14.2E for the Vanguard Leader's Crossbow and Vanguard Leader's Amulet."),
            new ActivityRecommendation(10, 20, Skill.Dagger, "Equipment: Explore the Folthid Estate near Yanshi at 8.6S, 52.9EE for the Dagger of Tikola and Dull Dagger. Speak with Raxanza Folthid at 8.8S, 53.7E for more information."),
            new ActivityRecommendation(10, 20, Skill.Spear, "MosswartExodusSpear", "Equipment: Help Bleeargh retrieve the Spear of Kreerg, Bleeargh can be found in Yanshi at 12.8S, 46.2E."),
            new ActivityRecommendation(10, 20, Skill.Spear, "Equipment: Recover the Quarter Staff of Fire from Banderlings camped near Edelbar at 43.9N, 25.1E."),
            new ActivityRecommendation(10, 20, "RegicideComplete", "Equipment: Speak with Sir Rylanan in Holtburg, Sir Tenshin in Shoushi or Dame Tsaya in Yaraq to help with an investigation and be rewarded with Elysa's Favor."),

            new ActivityRecommendation(5, 20, "IceTachiTurnedIn", "XP/Equipment: Retrieve the fabled Ice Tachi from the mosswart camp at 27.5S, 71.0E near Shoushi, keep it or deliver it to an Ivory Crafter for an experience reward."),
            new ActivityRecommendation(10, 20, "AcidAxeTurnedIn", "XP/Equipment: Explore Suntik Village near Zaikhal at 16.2N 4.3E for the Acid Axe, keep it or deliver it to an Ivory Crafter for an experience reward."),
            new ActivityRecommendation(10, 20, "GivenTibriSpear", "XP/Equipment: Explore a Cave near Cragstone at 24.2N, 43.2E for Tibri's Fire Spear, keep it or deliver it to Tibri also in the cave for an experience reward."),
            new ActivityRecommendation(10, 20, "LilithasBowGiven", "XP/Equipment: Explore Hunter's Leap near Holtburg at 35.7N, 32.6E for Lilitha's Bow, keep it or deliver it to Eldrista at 35.7N, 33.4E for an experience reward."),

            new ActivityRecommendation(10, 20, Skill.Armor, "Equipment: Explore the Glenden Wood Dungeon near Glenden Wood at 29.9N, 26.4E for the Platemail Hauberk of the Ogre."),
            new ActivityRecommendation(10, 20, new HashSet<Skill>{Skill.Armor, Skill.Shield}, "Equipment: Explore the Halls of the Helm near Zaikhal at 15.8N, 2.1E for the Fiery Shield and Superior Helmet."),
            new ActivityRecommendation(10, 20, new HashSet<Skill>{Skill.Axe, Skill.Shield}, "Equipment: Explore Trothyr's Rest near Rithwic at 10.3N, 54.9E for Trothyr's War Hammer and Trothyr's Shield. Talk to Ringoshu the Apple Seller at 13.6N, 50.7E for more information."),
            new ActivityRecommendation(5, 20, new HashSet<Skill>{Skill.Armor, Skill.Spear}, "Equipment: Explore the Green Mire Grave near Shoushi at 27.8S, 71.6E for the Green Mire Warrior's Yoroi Cuirass and the Green Mire Yari."),

            new ActivityRecommendation(20, 30, "Hunting Grounds: Hunt Lugians in the Hills Citadel near Lin at 56.6S, 66.9E."),

            // T2
            new ActivityRecommendation(15, 30, "PalenqualCompleted", "Equipment: Hunt Hea Warriors north of Greenspire to get Totems, once you have a totem take it to Aun Shimauri at 46.7N, 70.6W for more information on how to acquire your Palenqual weapon."),
            new ActivityRecommendation(20, 30, "OlthoiHunting3", "XP: Kill Olthois in the Crumbling Empyrean Mansion near Greenspire at 46.8N, 67.8W and bring a Worker Pincer to Behdo Yii in Redspire."),
            new ActivityRecommendation(14, 25, "TuskFemalePickup", "XP: Kill Tuskers in the Tusker Burrow in Alphus Lassel at 2.0N, 97.9E and bring a Female Tusker Tusk to Brighteyes in Oolutanga's Refuge. You can get to Alphus lassel via the Tusker Temples: 10.5S 65.6E for levels 1-30, 59.8N 28.4E for levels 20-50 and 0.7N 68.1W for levels 40+."),
            new ActivityRecommendation(18, 30, "TuskMalePickup", "XP: Kill Tuskers in the Tusker Lodge in Alphus Lassel at 0.1N, 98.1E and bring a Male Tusker Tusk to Brighteyes in Oolutanga's Refuge. You can get to Alphus lassel via the Tusker Temples: 10.5S 65.6E for levels 1-30, 59.8N 28.4E for levels 20-50 and 0.7N 68.1W for levels 40+."),
            new ActivityRecommendation(25, 35, "TuskCrimsonbackPickup", "XP: Kill Tuskers in the Tusker Cave in Alphus Lassel at 0.4N, 97.4E and bring a Tusker Crimsonback Tusk to Brighteyes in Oolutanga's Refuge. You can get to Alphus lassel via the Tusker Temples: 10.5S 65.6E for levels 1-30, 59.8N 28.4E for levels 20-50 and 0.7N 68.1W for levels 40+."),

            new ActivityRecommendation(15, 30, Skill.Lockpick, "Hunting Grounds: Halls of Metos - North of Tufa at 4.4S, 0.6W - Hunt Undeads for Mnemosynes, and golems for Motes and Hearts. Use a Intricate Carving Tool and the Lockpick skill to turn the Hearts into keys for the Mnemosynes."),
            new ActivityRecommendation(15, 30, "Hunting Grounds: Halls of Metos - North of Tufa at 4.4S, 0.6W - Hunt Undeads for Mnemosynes, and golems for Motes."),
            new ActivityRecommendation(20, 30, "Hunting Grounds: Northern Tiofor Woods - Hunt Shadows for Dark Slivers in the region north of Glenden Wood and Holtburg."),
            new ActivityRecommendation(20, 35, "Hunting Grounds: The very bottom of the Fenmalain Chamber is a great place to hunt Fragments for Tiny Shards. To get there you need to use Fenmalain Keys at the bottom of the Fenmalain Vestibule near Baishi at 46.9S, 55.2E."),
            new ActivityRecommendation(20, 30, Skill.Axe, "Equipment: Explore the Bellig Tower near Zaikhal at 17.8N, 16.0E for the Hammer of Lightning."),
            new ActivityRecommendation(20, 35, Skill.Mace, "JitteKrauLiLesser", "Equipment: Explore the Catacombs of the Forgotten in the Plains of Gaerwel at 17.3N, 32.8E for Mi Krau-Li's Jitte."),
            new ActivityRecommendation(20, 50, Skill.Axe, "Equipment: Kill the lugians in the Gotrok Raider Camp at 80.8S, 37.6E for the Lugian Scepter and Cloth of the Arm and take them to Master Ulkas in Livak Tukal for the Scepter of Might and Sleeves of Inexhaustibility."),
            new ActivityRecommendation(20, 50, new HashSet<Skill>{Skill.Shield, Skill.WarMagic}, "Equipment: Kill the lugians in the Gotrok Raider Camp at 87.2S, 27.3E for the Lugian Crest and Sceptre of the Mind and take them to Master Ulkas in Livak Tukal for the Crest of Kings and Staff of Clarity."),
            new ActivityRecommendation(20, 50, new HashSet<Skill>{Skill.Spear, Skill.Armor}, "Equipment: Kill the lugians in the Gotrok Raider Camp at 67.9S, 32.8E for the Lugian Pauldron and Blade of the Heart and take them to Master Ulkas in Livak Tukal for the Helm of the Crag and Spear of Purity."),
            new ActivityRecommendation(20, 50, new HashSet<Skill>{Skill.Axe, Skill.Dagger, Skill.Mace, Skill.Spear, Skill.Staff, Skill.Sword, Skill.UnarmedCombat, Skill.Bow, Skill.Crossbow, Skill.ThrownWeapon, Skill.WarMagic, Skill.LifeMagic}, "Equipment: Venture into the Tumerok Training Camps near Dryreach, acquire Tumerok Banners and trade them in at the Army Recruiter located in capital cities for an Assault Weapon."),

            new ActivityRecommendation(20, 35, Skill.Armor, new HashSet<string>{"PickedUpBroodMatronTail", "PickedUpBroodMatronTarsus", "PickedUpBroodMatronTibia", "PickedUpBroodQueenCarapace", "PickedUpBroodQueenClaw", "PickedUpBroodQueenCrest", "PickedUpBroodQueenFemur", "PickedUpBroodQueenHead", "PickedUpBroodQueenMetathorax"}, "Fellowship - Equipment: Explore the Olthoi Brood Hives at 51.2N 48.1E or 44.2N, 66.2E for the Lesser Olthoi Armor."),

            // Higher
            new ActivityRecommendation(35, 999, "OlthoiHunting4", "XP: Kill Olthois in An Olthoi Soldier Nest in the Marescent Plateau at 45.2N, 76.3W and bring a Soldier Pincer to Behdo Yii in Redspire."),
            new ActivityRecommendation(40, 999, "OlthoiHunting5", "XP: Kill Olthois in the Ancient Empyrean Grotto in the Marescent Plateau at 52.6N, 73.1W and bring a Legionary Pincer to Behdo Yii in Redspire."),
            new ActivityRecommendation(50, 999, "OlthoiHunting6", "XP: Kill Olthois in the Lair of the Eviscerators in the Marescent Plateau at 53.7N, 76.6W and bring an Eviscerator Pincer to Behdo Yii in Redspire."),
            new ActivityRecommendation(70, 999, "OlthoiHunting7", "XP: Kill Olthois in the Olthoi Warrior Nest in the Marescent Plateau at 46.9N, 81.2W and bring a Warrior Pincer to Behdo Yii in Redspire."),
            new ActivityRecommendation(80, 999, "OlthoiHunting8", "XP: Kill Olthois in the Mutilator Tunnels in the Marescent Plateau at 52.8N, 78.1W and bring a Mutilator Pincer to Behdo Yii in Redspire."),
            new ActivityRecommendation(30, 999, "TuskGoldenbackPickup", "XP: Kill Tuskers in the Tusker Cavern in Alphus Lassel at 1.0N, 96.9E and bring a Goldenback Tusker Tusk to Brighteyes in Oolutanga's Refuge. You can get to Alphus lassel via the Tusker Temples: 10.5S 65.6E for levels 1-30, 59.8N 28.4E for levels 20-50 and 0.7N 68.1W for levels 40+."),
            new ActivityRecommendation(35, 999, "TuskRedeemerPickup", "XP: Kill Tuskers in the Tusker Abode in Alphus Lassel at 3.2S, 94.9E and bring a Tusker Redeemer Tusk to Brighteyes in Oolutanga's Refuge. You can get to Alphus lassel via the Tusker Temples: 10.5S 65.6E for levels 1-30, 59.8N 28.4E for levels 20-50 and 0.7N 68.1W for levels 40+."),
            new ActivityRecommendation(40, 999, "TuskLiberatorPickup", "XP: Kill Tuskers in the Tusker Habitat in Alphus Lassel at 0.5S, 95.9E and bring a Tusker Liberator Tusk to Brighteyes in Oolutanga's Refuge. You can get to Alphus lassel via the Tusker Temples: 10.5S 65.6E for levels 1-30, 59.8N 28.4E for levels 20-50 and 0.7N 68.1W for levels 40+."),
            new ActivityRecommendation(45, 999, "TuskSlavePickup", "XP: Kill Tuskers in the Tusker Quarters in Alphus Lassel at 2.3S, 95.6E and bring a Tusker Slave Tusk to Brighteyes in Oolutanga's Refuge. You can get to Alphus lassel via the Tusker Temples: 10.5S 65.6E for levels 1-30, 59.8N 28.4E for levels 20-50 and 0.7N 68.1W for levels 40+."),
            new ActivityRecommendation(50, 999, "TuskGuardPickup", "XP: Kill Tuskers in the Tusker Barracks in Alphus Lassel at 0.3S, 90.8E and bring a Tusker Guard Tusk to Brighteyes in Oolutanga's Refuge. You can get to Alphus lassel via the Tusker Temples: 10.5S 65.6E for levels 1-30, 59.8N 28.4E for levels 20-50 and 0.7N 68.1W for levels 40+."),
            new ActivityRecommendation(55, 999, "TuskSilverPickup", "XP: Kill Tuskers in the Tusker Pits in Alphus Lassel at 1.3N, 91.8E and bring a Silver Tusker Tusk to Brighteyes in Oolutanga's Refuge. You can get to Alphus lassel via the Tusker Temples: 10.5S 65.6E for levels 1-30, 59.8N 28.4E for levels 20-50 and 0.7N 68.1W for levels 40+."),
            new ActivityRecommendation(60, 999, "TuskArmoredPickup", "XP: Kill Tuskers in the Tusker Armory in Alphus Lassel at 0.0N, 89.4E and bring a Armored Tusker Tusk to Brighteyes in Oolutanga's Refuge. You can get to Alphus lassel via the Tusker Temples: 10.5S 65.6E for levels 1-30, 59.8N 28.4E for levels 20-50 and 0.7N 68.1W for levels 40+."),
            new ActivityRecommendation(65, 999, "TuskRampagerPickup", "XP: Kill Tuskers in the Tusker Holding in Alphus Lassel at 3.5S, 85.3E and bring a Rampager Tusk to Brighteyes in Oolutanga's Refuge. You can get to Alphus lassel via the Tusker Temples: 10.5S 65.6E for levels 1-30, 59.8N 28.4E for levels 20-50 and 0.7N 68.1W for levels 40+."),
            new ActivityRecommendation(70, 999, "TuskPlatedPickup", "XP: Kill Tuskers in the Tusker Tunnels in Alphus Lassel at 0.4N, 86.4E and bring a Plated Tusker Tusk to Brighteyes in Oolutanga's Refuge. You can get to Alphus lassel via the Tusker Temples: 10.5S 65.6E for levels 1-30, 59.8N 28.4E for levels 20-50 and 0.7N 68.1W for levels 40+."),
            new ActivityRecommendation(80, 999, "TuskAssailerPickup", "XP: Kill Tuskers in the Tusker Honeycombs in Alphus Lassel at 1.3S, 86.9E and bring a Assailer Tusk to Brighteyes in Oolutanga's Refuge. You can get to Alphus lassel via the Tusker Temples: 10.5S 65.6E for levels 1-30, 59.8N 28.4E for levels 20-50 and 0.7N 68.1W for levels 40+."),
            new ActivityRecommendation(100, 999, "TuskDevastatorPickup", "XP: Kill Tuskers in the Tusker Lacuna in Alphus Lassel at 9.9S, 90.7E and bring a Devastator Tusk to Brighteyes in Oolutanga's Refuge. You can get to Alphus lassel via the Tusker Temples: 10.5S 65.6E for levels 1-30, 59.8N 28.4E for levels 20-50 and 0.7N 68.1W for levels 40+."),
        };

        [CommandHandler("recs", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Recommend activities appropriate to the character.")]
        public static void HandleRecommend(Session session, params string[] parameters)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat($"Unknown command: recs", ChatMessageType.Help));
                return;
            }

            var level = session.Player?.Level ?? 1;
            var minLevel = Math.Max(level - (int)Math.Ceiling(level * 0.1f), 1);
            var maxLevel = level + (int)Math.Ceiling(level * 0.2f);
            if (level > 100)
                maxLevel = int.MaxValue;
            var explorationList = DatabaseManager.World.GetExplorationSitesByLevelRange(minLevel, maxLevel, level);

            var validRecommendations = BuildRecommendationList(session.Player);

            if (validRecommendations.Count == 0 && explorationList.Count == 0)
                session.Network.EnqueueSend(new GameMessageSystemChat("No recommendations at the moment.", ChatMessageType.WorldBroadcast));
            else
            {
                session.Network.EnqueueSend(new GameMessageSystemChat("Activity Recommendations:", ChatMessageType.WorldBroadcast));
                foreach (var recommendation in validRecommendations)
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat(recommendation.RecommendationText, ChatMessageType.WorldBroadcast));
                }

                foreach (var entry in explorationList)
                {
                    string amountAdjetive;
                    if (entry.CreatureCount < 10)
                        amountAdjetive = "a few ";
                    else if (entry.CreatureCount < 25)
                        amountAdjetive = "";
                    else if (entry.CreatureCount < 50)
                        amountAdjetive = "some ";
                    else if (entry.CreatureCount < 75)
                        amountAdjetive = "quite a few ";
                    else
                        amountAdjetive = "a lot of ";

                    string entryName;
                    string entryDirections;
                    var entryLandblock = DatabaseManager.World.GetLandblockDescriptionsByLandblock((ushort)entry.Landblock).FirstOrDefault();
                    if (entryLandblock != null)
                    {
                        entryName = entryLandblock.Name;
                        if (entryLandblock.MicroRegion != "")
                            entryDirections = $"{entryLandblock.Directions} {entryLandblock.Reference} in {entryLandblock.MicroRegion}";
                        else if (entryLandblock.MacroRegion != "" && entryLandblock.MacroRegion != "Dereth")
                            entryDirections = $"{entryLandblock.Directions} {entryLandblock.Reference} in {entryLandblock.MacroRegion}";
                        else
                            entryDirections = $"{entryLandblock.Directions} {entryLandblock.Reference}";
                    }
                    else
                    {
                        entryName = $"unknown location({entry.Landblock})";
                        entryDirections = "at an unknown location";
                    }

                    var msg = $"Hunting Grounds: {entryName} {entryDirections}. Expect to find {amountAdjetive}{entry.ContentDescription}.";
                    session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.WorldBroadcast));
                }
            }
        }

        [CommandHandler("rec", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Recommend an activity appropriate to the character.")]
        public static void HandleSingleRecommendation(Session session, params string[] parameters)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat($"Unknown command: rec", ChatMessageType.Help));
                return;
            }

            SingleRecommendation(session, false);
        }

        public static void SingleRecommendation(Session session, bool failSilently)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
                return;

            var validRecommendations = BuildRecommendationList(session.Player);

            var level = session.Player?.Level ?? 1;
            var minLevel = Math.Max(level - (int)Math.Ceiling(level * 0.1f), 1);
            var maxLevel = level + (int)Math.Ceiling(level * 0.2f);
            if (level > 100)
                maxLevel = int.MaxValue;
            var explorationList = DatabaseManager.World.GetExplorationSitesByLevelRange(minLevel, maxLevel, level);

            if (validRecommendations.Count != 0)
            {
                if (ThreadSafeRandom.Next(1, 4) != 4 || explorationList.Count == 0)
                {
                    var recommendation = validRecommendations[ThreadSafeRandom.Next(0, validRecommendations.Count - 1)];
                    session.Network.EnqueueSend(new GameMessageSystemChat($"Activity Recommendation:\n{recommendation.RecommendationText}", ChatMessageType.WorldBroadcast));
                    return;
                }
            }

            if (explorationList.Count == 0)
            {
                if (!failSilently)
                    session.Network.EnqueueSend(new GameMessageSystemChat("No recommendations at the moment.", ChatMessageType.WorldBroadcast));
            }
            else
            {
                var entry = explorationList[ThreadSafeRandom.Next(0, explorationList.Count - 1)];

                string amountAdjetive;
                if (entry.CreatureCount < 10)
                    amountAdjetive = "a few ";
                else if (entry.CreatureCount < 25)
                    amountAdjetive = "";
                else if (entry.CreatureCount < 50)
                    amountAdjetive = "some ";
                else if (entry.CreatureCount < 75)
                    amountAdjetive = "quite a few ";
                else
                    amountAdjetive = "a lot of ";

                string entryName;
                string entryDirections;
                var entryLandblock = DatabaseManager.World.GetLandblockDescriptionsByLandblock((ushort)entry.Landblock).FirstOrDefault();
                if (entryLandblock != null)
                {
                    entryName = entryLandblock.Name;
                    if (entryLandblock.MicroRegion != "")
                        entryDirections = $"{entryLandblock.Directions} {entryLandblock.Reference} in {entryLandblock.MicroRegion}";
                    else if (entryLandblock.MacroRegion != "" && entryLandblock.MacroRegion != "Dereth")
                        entryDirections = $"{entryLandblock.Directions} {entryLandblock.Reference} in {entryLandblock.MacroRegion}";
                    else
                        entryDirections = $"{entryLandblock.Directions} {entryLandblock.Reference}";
                }
                else
                {
                    entryName = $"unknown location({entry.Landblock})";
                    entryDirections = "at an unknown location";
                }

                var msg = $"Activity Recommendation:\nHunting Grounds: {entryName} {entryDirections}. Expect to find {amountAdjetive}{entry.ContentDescription}.";
                session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.WorldBroadcast));
            }
        }

        public static List<ActivityRecommendation> BuildRecommendationList(Player player)
        {
            if (player == null)
                return new List<ActivityRecommendation>();

            var validRecommendations = new List<ActivityRecommendation>();
            foreach (var recommendation in Recommendations)
            {
                if (recommendation.IsApplicable(player))
                {
                    validRecommendations.Add(recommendation);
                }
            }

            return validRecommendations;
        }

        [CommandHandler("hotdungeons", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Lists all currently active Hot Dungeons.")]
        public static void HandleHotDungeons(Session session, params string[] parameters)
        {
            CommandHandlerHelper.WriteOutputInfo(session, HotDungeonManager.GetStatusMessage(), ChatMessageType.Broadcast);
        }

        [CommandHandler("xptracker", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0, "Return XP tracking information.", "<reset>")]
        public static void HandleXpTracker(Session session, params string[] parameters)
        {
            bool reset = false;
            if (parameters.Length > 0)
                reset = parameters[0].ToLower() == "reset";

            if (!reset)
            {
                if (!session.Player.XpTrackerStartTimestamp.HasValue || !session.Player.XpTrackerTotalXp.HasValue)
                {
                    session.Player.XpTrackerStartTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
                    session.Player.XpTrackerTotalXp = 0;
                    session.Network.EnqueueSend(new GameMessageSystemChat($"XP tracking has been enabled for your character.\n", ChatMessageType.Broadcast));
                    return;
                }

                var currUnixTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
                var durationSeconds = currUnixTimestamp - session.Player.XpTrackerStartTimestamp.Value;

                if (session.Player.XpTrackerTotalXp.Value > 0 && durationSeconds > 0)
                {
                    var durationTimespan = TimeSpan.FromSeconds(durationSeconds);
                    var xpPerSecond = session.Player.XpTrackerTotalXp.Value / (double)(durationSeconds);
                    var xpPerHour = xpPerSecond * 60 * 60;
                    var msg = $"You've earned {session.Player.XpTrackerTotalXp.Value:N0} experience in {FormatTimespan(durationTimespan)} at a rate of {xpPerHour:N0} experience per hour.";
                    session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.Broadcast));
                }
                else
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat("No XP has been tracked for your character yet.", ChatMessageType.Broadcast));
                }
            }
            else
            {
                session.Player.XpTrackerStartTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
                session.Player.XpTrackerTotalXp = 0;
                session.Network.EnqueueSend(new GameMessageSystemChat($"Your character's xp tracking data has been reset.\n", ChatMessageType.Broadcast));
            }
        }

        [CommandHandler("tar", AccessLevel.Player, CommandHandlerFlag.None, 0, "Show current T.A.R. experience multipliers", "<creatureType>")]
        public static void HandleRest(Session session, params string[] parameters)
        {
            if (parameters.Length > 0 && Enum.TryParse(parameters[0], true, out CreatureType creatureType))
            {
                session.Player.CampManager.GetCurrentCampBonus(creatureType, out var typeCampBonus, out var areaCampBonus, out var restCampBonus, out var typeRecovery, out var areaRecovery, out var restRecovery);
                CommandHandlerHelper.WriteOutputInfo(session, $"Current T.A.R. experience multipliers:\n   Type({creatureType}): {(typeCampBonus * 100).ToString("0")}%{(typeCampBonus != 1 ? $" - Estimated recovery time: {FormatTimespan(typeRecovery)}" : "")}\n   Area: {(areaCampBonus * 100).ToString("0")}%{(areaCampBonus != 1 ? $" - Estimated recovery time: {FormatTimespan(areaRecovery)}" : "")}\n   Rest: {(restCampBonus * 100).ToString("0")}%{(restCampBonus != 1 ? $" - Estimated recovery time: {FormatTimespan(restRecovery)}" : "")}");
            }
            else
            {
                session.Player.CampManager.GetCurrentCampBonus(CreatureType.Invalid, out _, out var areaCampBonus, out var restCampBonus, out var typeRecovery, out var areaRecovery, out var restRecovery);
                CommandHandlerHelper.WriteOutputInfo(session, $"Current T.A.R. experience multipliers:\n   Area: {(areaCampBonus * 100).ToString("0")}%{(areaCampBonus != 1 ? $" - Estimated recovery time: {FormatTimespan(areaRecovery)}" : "")}\n   Rest: {(restCampBonus * 100).ToString("0")}%{(restCampBonus != 1 ? $" - Estimated recovery time: {FormatTimespan(restRecovery)}" : "")}");
            }
        }

        /// <summary>
        /// List online players within the character's allegiance.
        /// </summary>
        [CommandHandler("who", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0, "List online players within the character's allegiance.")]
        public static void HandleWho(Session session, params string[] parameters)
        {
            if (!PropertyManager.GetBool("command_who_enabled").Item)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat("The command \"who\" is not currently enabled on this server.", ChatMessageType.Broadcast));
                return;
            }

            var selfMonarchId = AllegianceManager.GetVerifiedMonarchId(session.Player);
            if (selfMonarchId == null)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat("You must be in an allegiance to use this command.", ChatMessageType.Broadcast));
                return;
            }

            if (DateTime.UtcNow - session.Player.PrevWho < TimeSpan.FromMinutes(1))
            {
                session.Network.EnqueueSend(new GameMessageSystemChat("You have used this command too recently!", ChatMessageType.Broadcast));
                return;
            }

            session.Player.PrevWho = DateTime.UtcNow;

            StringBuilder message = new StringBuilder();
            message.Append("Allegiance Members: \n");


            uint playerCounter = 0;
            foreach (var player in PlayerManager.GetAllOnline().OrderBy(p => p.Name))
            {
                if (AllegianceManager.GetVerifiedMonarchId(player) == selfMonarchId)
                {
                    message.Append($"{player.Name} - Level {player.Level}\n");
                    playerCounter++;
                }
            }

            message.Append("Total: " + playerCounter + "\n");

            CommandHandlerHelper.WriteOutputInfo(session, message.ToString(), ChatMessageType.Broadcast);
        }

        public static string FormatTimespan(TimeSpan timespan)
        {
            string returnText = "";
            if (timespan.TotalMinutes < 2)
            {
                if (timespan.Seconds > 1)
                    returnText = $"{timespan.Seconds} seconds";
                else if (timespan.Seconds == 1)
                    returnText = $"{timespan.Seconds} second";

                if (timespan.Minutes > 0)
                    returnText = $"{timespan.Minutes} minute" + (returnText.Length > 0 ? $" {returnText}" : "");
            }
            else
            {
                if (timespan.Minutes > 1)
                    returnText = $"{timespan.Minutes} minutes";
                else if (timespan.Minutes == 1)
                    returnText = $"{timespan.Minutes} minute";

                int totalHours = (int)Math.Floor(timespan.TotalHours);
                if (totalHours > 0)
                {
                    if (timespan.Hours > 1)
                        returnText = $"{timespan.Hours} hours" + (returnText.Length > 0 ? $" {returnText}" : "");
                    else if (timespan.Hours == 1)
                        returnText = $"{timespan.Hours} hour" + (returnText.Length > 0 ? $" {returnText}" : "");
                }

                int totalDays = (int)Math.Floor(timespan.TotalDays);
                if (totalDays > 1)
                    returnText = $"{totalDays} days" + (returnText.Length > 0 ? $" {returnText}" : "");
                else if (totalDays > 0)
                    returnText = $"{totalDays} day" + (returnText.Length > 0 ? $" {returnText}" : "");
            }

            return returnText;
        }

        public class ActivityRecommendation
        {
            public HashSet<Skill> Skills = new HashSet<Skill>();
            public int MinLevel = 0;
            public int MaxLevel = int.MaxValue;
            public int MinSkill = 0;
            public int MaxSkill = int.MaxValue;
            public HashSet<string> QuestFlags = new HashSet<string>();
            public string RecommendationText;

            public ActivityRecommendation(int minLevel, int maxLevel, string recommendation)
                : this(minLevel, maxLevel, new HashSet<Skill>(), 0, int.MaxValue, new HashSet<string>(), recommendation) { }

            public ActivityRecommendation(int minLevel, int maxLevel, Skill skill, string recommendation)
                : this(minLevel, maxLevel, new HashSet<Skill> { skill }, 0, int.MaxValue, new HashSet<string>(), recommendation) { }

            public ActivityRecommendation(int minLevel, int maxLevel, HashSet<Skill> skills, string recommendation)
                : this(minLevel, maxLevel, skills, 0, int.MaxValue, new HashSet<string>(), recommendation) { }

            public ActivityRecommendation(int minLevel, int maxLevel, string questFlag, string recommendation)
                : this(minLevel, maxLevel, new HashSet<Skill>(), 0, int.MaxValue, new HashSet<string> { questFlag }, recommendation) { }

            public ActivityRecommendation(int minLevel, int maxLevel, HashSet<string> questFlags, string recommendation)
                : this(minLevel, maxLevel, new HashSet<Skill>(), 0, int.MaxValue, questFlags, recommendation) { }

            public ActivityRecommendation(int minLevel, int maxLevel, Skill skill, int minSkill, int maxSkill, string recommendation)
                : this(minLevel, maxLevel, new HashSet<Skill> { skill }, minSkill, maxSkill, new HashSet<string>(), recommendation) { }

            public ActivityRecommendation(int minLevel, int maxLevel, Skill skill, string questFlag, string recommendation)
                : this(minLevel, maxLevel, new HashSet<Skill> { skill }, 0, int.MaxValue, new HashSet<string> { questFlag }, recommendation) { }

            public ActivityRecommendation(int minLevel, int maxLevel, Skill skill, HashSet<string> questFlags, string recommendation)
                : this(minLevel, maxLevel, new HashSet<Skill> { skill }, 0, int.MaxValue, questFlags, recommendation) { }

            public ActivityRecommendation(int minLevel, int maxLevel, Skill skill, int minSkill, int maxSkill, string questFlag, string recommendation)
                : this(minLevel, maxLevel, new HashSet<Skill> { skill }, minSkill, maxSkill, new HashSet<string> { questFlag }, recommendation) { }

            public ActivityRecommendation(int minLevel, int maxLevel, HashSet<Skill> skills, int minSkill, int maxSkill, HashSet<string> questFlags, string recommendation)
            {
                MinLevel = minLevel;
                MaxLevel = maxLevel;
                Skills = skills;
                MinSkill = minSkill;
                MaxSkill = maxSkill;
                QuestFlags = questFlags;
                RecommendationText = recommendation;
            }

            public bool IsApplicable(Player player)
            {
                if (player.Level < MinLevel || player.Level > MaxLevel)
                    return false;

                foreach (var questFlag in QuestFlags)
                {
                    if (!player.QuestManager.CanSolve(questFlag))
                        return false;
                }

                if (Skills.Count == 0)
                    return true;

                foreach (var skill in Skills)
                {
                    var playerSkill = player.GetCreatureSkill(player.ConvertToMoASkill(skill));
                    if (playerSkill.AdvancementClass == SkillAdvancementClass.Trained || playerSkill.AdvancementClass == SkillAdvancementClass.Specialized)
                    {
                        if (playerSkill.Current >= MinSkill && playerSkill.Current <= MaxSkill)
                            return true;
                    }
                }

                return false;
            }
        }

        [CommandHandler("fi", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Resends all visible items and creatures to the client")]
        [CommandHandler("fixinvisible", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Resends all visible items and creatures to the client")]
        public static void HandleFixInvisible(Session session, params string[] parameters)
        {
            var knownObjects = session.Player.GetKnownObjects();
            foreach (var entry in knownObjects)
            {
                session.Player.TrackObject(entry, true);
            }
        }

        // flagtinker
        [CommandHandler("flagtinker", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Permanently designates this character as a Tinker. " +
            "Requirements: character must be level 1 and the account must not already have a Tinker character. " +
            "All crafting skills will be auto-specialized and maxed. All offensive combat skills will be removed. " +
            "Tinker characters cannot train or specialize new skills and do not suffer vitae on death. This is irreversible.")]
        public static void HandleFlagTinker(Session session, params string[] parameters)
        {
            var player = session.Player;

            // Already a Tinker: re-run to apply the latest Tinker upgrades (e.g. Arcane Lore
            // specialization, trinket cantrips) for characters flagged before those were added.
            // The eligibility checks below are for initial designation only and are skipped here.
            if (player.IsTinker)
            {
                player.FlagAsTinker();
                return;
            }

            // Must be level 1
            if ((player.Level ?? 0) > 1)
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat("Only a level 1 character may be designated as a Tinker.", ChatMessageType.Broadcast));
                return;
            }

            // One Tinker per account
            var accountPlayers = PlayerManager.GetAccountPlayers(session.AccountId);
            if (accountPlayers != null && accountPlayers.Values.Any(p => p.IsTinker))
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat("Your account already has a Tinker character. Only one Tinker is allowed per account.", ChatMessageType.Broadcast));
                return;
            }

            player.FlagAsTinker();
        }

        // mule
        [CommandHandler("mule", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Grants you a My Mule summon gem, or searches your mule storage for items matching a spell regex.",
            "search <pattern>\n" +
            "Searches whichever mule vendor window you currently have open (your own or someone else's -- anyone can search a mule, same as anyone can already view one) for items whose spells match <pattern>, a .NET regular expression matched case-insensitively against the item's spell names. That mule's buy window is filtered down to just the matches. Run with no pattern (\"/mule search\"), or just double-click the mule to reopen it, to clear the filter and go back to the normal listing.\n" +
            "Examples: \"/mule search legendary.*legendary\" (double legendary gear), \"/mule search legendary (frost|flame|acid)\" (any of several cantrips), \"/mule search legendary frost.*legendary acid\" (both cantrips present).\n" +
            "With no parameters, grants a My Mule summon gem if you don't already have one.")]
        public static void HandleMule(Session session, params string[] parameters)
        {
            var player = session.Player;

            if (parameters.Length > 0 && parameters[0].Equals("search", StringComparison.OrdinalIgnoreCase))
            {
                var pattern = string.Join(" ", parameters.Skip(1));
                player.SearchMuleInventory(pattern);
                return;
            }

            if (player.GetNumInventoryItemsOfWCID(MuleInfo.GemWeenieClassId) > 0)
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat("You already have your My Mule gem.", ChatMessageType.Broadcast));
                return;
            }

            var gem = ACE.Server.Factories.WorldObjectFactory.CreateNewWorldObject(MuleInfo.GemWeenieClassId);

            if (gem == null)
            {
                log.Error($"[MULE] /mule: couldn't create gem weenie {MuleInfo.GemWeenieClassId} for {player.Name}");
                return;
            }

            if (player.TryCreateInInventoryWithNetworking(gem, out _, true))
                player.Session.Network.EnqueueSend(new GameMessageSystemChat("You receive your My Mule gem.", ChatMessageType.Broadcast));
            else
                gem.Destroy();
        }

        [CommandHandler("allowres", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Toggles allowing resurrection attempts.")]
        [CommandHandler("allowress", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Toggles allowing resurrection attempts.")]
        public static void HandleAllowRess(Session session, params string[] parameters)
        {
            var newSetting = !session.Player.GetCharacterOption(CharacterOption.AllowRessAttempts);
            session.Player.SetCharacterOption(CharacterOption.AllowRessAttempts, newSetting);

            if (newSetting)
                CommandHandlerHelper.WriteOutputInfo(session, $"You are now accepting resurrection attempts.", ChatMessageType.Broadcast);
            else
                CommandHandlerHelper.WriteOutputInfo(session, $"You are no longer accepting resurrection attempts.", ChatMessageType.Broadcast);
        }

        [CommandHandler("smartsalvage", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Configures the smart salvage system.")]
        public static void HandleSmartSalvage(Session session, params string[] parameters)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat($"Unknown command: smartsalvage", ChatMessageType.Help));
                return;
            }

            var param0 = "help";
            if (parameters.Length > 0)
                param0 = parameters[0].ToLower();

            if (param0 == "help")
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage system usage:", ChatMessageType.Broadcast);
                CommandHandlerHelper.WriteOutputInfo(session, $"   /SmartSalvage status to show current settings.", ChatMessageType.Broadcast);
                CommandHandlerHelper.WriteOutputInfo(session, $"   /SmartSalvage <on/off> to enable/disable the smart salvage system.", ChatMessageType.Broadcast);
                CommandHandlerHelper.WriteOutputInfo(session, $"   /SmartSalvage avoidInscripted <on/off> to enable/disable avoiding salvaging inscripted items.", ChatMessageType.Broadcast);
                CommandHandlerHelper.WriteOutputInfo(session, $"   /SmartSalvage mode <whitelist/blacklist> to switch between filter modes: in blacklist mode materials in the filter won't be salvaged, in whitelist mode only materials in the filter will be salvaged.", ChatMessageType.Broadcast);
                CommandHandlerHelper.WriteOutputInfo(session, $"   /SmartSalvage add <material> to add that material to the filter.", ChatMessageType.Broadcast);
                CommandHandlerHelper.WriteOutputInfo(session, $"   /SmartSalvage remove <material> to remove that material from the filter.", ChatMessageType.Broadcast);
                CommandHandlerHelper.WriteOutputInfo(session, $"   /SmartSalvage clear to remove all materials from the filter.", ChatMessageType.Broadcast);
            }
            else if (param0 == "status")
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage system status: {(session.Player.UseSmartSalvageSystem ? "enabled" : "disabled")}.", ChatMessageType.Broadcast);
                CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage avoid inscripted items: {(session.Player.SmartSalvageAvoidInscripted ? "enabled" : "disabled")}.", ChatMessageType.Broadcast);
                CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage filter mode: {(session.Player.SmartSalvageIsWhitelist ? "whitelist" : "blacklist")}.", ChatMessageType.Broadcast);
                if (session.Player.SmartSalvageFilter == null)
                    CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage filter: empty.", ChatMessageType.Broadcast);
                else
                {
                    var filters = (session.Player.SmartSalvageFilter ?? "").Split(",").ToList();
                    string filtersText = "";
                    bool first = true;
                    foreach (var filter in filters)
                    {
                        if (!first)
                            filtersText += ", ";
                        if (int.TryParse(filter, out var materialId))
                        {
                            filtersText += RecipeManager.GetMaterialName((MaterialType)materialId);
                            first = false;
                        }
                    }
                    CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage filter: {filtersText}", ChatMessageType.Broadcast);
                }
            }
            else if (param0 == "on")
            {
                if (!session.Player.UseSmartSalvageSystem)
                {
                    session.Player.UseSmartSalvageSystem = true;
                    CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage is now enabled.", ChatMessageType.Broadcast);
                }
                else
                    CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage is already enabled.", ChatMessageType.Broadcast);
            }
            else if (param0 == "off")
            {
                if (!session.Player.UseSmartSalvageSystem)
                {
                    session.Player.UseSmartSalvageSystem = true;
                    CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage is now disabled.", ChatMessageType.Broadcast);
                }
                else
                    CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage is already disabled.", ChatMessageType.Broadcast);
            }
            else if (param0 == "avoidinscripted")
            {
                if (parameters.Length < 2)
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"Could not parse command, check \"/SmartSalvage help\" for usage instructions.", ChatMessageType.Broadcast);
                    return;
                }

                var param1 = parameters[1].ToLower();
                if (param1 == "on")
                {
                    if (!session.Player.SmartSalvageAvoidInscripted)
                    {
                        session.Player.SmartSalvageAvoidInscripted = true;
                        CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage will now avoid salvaging inscripted items.", ChatMessageType.Broadcast);
                    }
                    else
                        CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage is already avoiding salvaging inscripted items.", ChatMessageType.Broadcast);
                }
                else if (param1 == "off")
                {
                    if (session.Player.UseSmartSalvageSystem)
                    {
                        session.Player.UseSmartSalvageSystem = false;
                        CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage will no longer avoid salvaging inscripted items.", ChatMessageType.Broadcast);
                    }
                    else
                        CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage is already not avoiding salvaging inscripted items.", ChatMessageType.Broadcast);
                }
                else
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"Could not parse command, check \"/SmartSalvage help\" for usage instructions.", ChatMessageType.Broadcast);
                    return;
                }
            }
            else if (param0 == "mode")
            {
                if (parameters.Length < 2)
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"Could not parse command, check \"/SmartSalvage help\" for usage instructions.", ChatMessageType.Broadcast);
                    return;
                }

                var param1 = parameters[1].ToLower();
                if (param1 == "blacklist")
                {
                    if (session.Player.SmartSalvageIsWhitelist)
                    {
                        session.Player.SmartSalvageIsWhitelist = false;
                        CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage will now avoid salvaging materials in the filter.", ChatMessageType.Broadcast);
                    }
                    else
                        CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage is already avoiding salvaging materials in the filter.", ChatMessageType.Broadcast);
                }
                else if (param1 == "whitelist")
                {
                    if (!session.Player.SmartSalvageIsWhitelist)
                    {
                        session.Player.SmartSalvageIsWhitelist = true;
                        CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage will now only salvage materials in the filter.", ChatMessageType.Broadcast);
                    }
                    else
                        CommandHandlerHelper.WriteOutputInfo(session, $"Smart salvage is already salvaging only materials in the filter.", ChatMessageType.Broadcast);
                }
                else
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"Could not parse command, check \"/SmartSalvage help\" for usage instructions.", ChatMessageType.Broadcast);
                    return;
                }
            }
            else if (param0 == "add")
            {
                if (parameters.Length < 2)
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"Missing material type to add to the salvage filter", ChatMessageType.Broadcast);
                    return;
                }

                if (!Enum.TryParse(parameters[1], true, out MaterialType material))
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"Unable to add {parameters[1]} to the salvage filter.", ChatMessageType.Broadcast);
                    return;
                }

                var filters = (session.Player.SmartSalvageFilter ?? "").Split(",").ToList();
                var searchString = ((int)material).ToString();
                var friendlyName = RecipeManager.GetMaterialName(material);
                if (filters.Contains(searchString))
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"{friendlyName} is already included in the salvage filter.", ChatMessageType.Broadcast);
                    return;
                }

                filters.Add(searchString);
                session.Player.SmartSalvageFilter = string.Join(",", filters);
                CommandHandlerHelper.WriteOutputInfo(session, $"Added {friendlyName} to the salvage filter.", ChatMessageType.Broadcast);
            }
            else if (param0 == "remove")
            {
                if (parameters.Length < 2)
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"Missing material type to remove from the salvage filter", ChatMessageType.Broadcast);
                    return;
                }

                if (!Enum.TryParse(parameters[1], true, out MaterialType material))
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"Unable to add {parameters[1]} to the salvage filter.", ChatMessageType.Broadcast);
                    return;
                }

                var filters = (session.Player.SmartSalvageFilter ?? "").Split(",").ToList();
                var searchString = ((int)material).ToString();
                var friendlyName = RecipeManager.GetMaterialName(material);
                if (filters.Contains(searchString))
                {
                    filters.Remove(searchString);
                    session.Player.SmartSalvageFilter = string.Join(",", filters);

                    CommandHandlerHelper.WriteOutputInfo(session, $"Removed {friendlyName} from the salvage filter.", ChatMessageType.Broadcast);
                    return;
                }
                CommandHandlerHelper.WriteOutputInfo(session, $"{friendlyName} is not included in the salvage filter.", ChatMessageType.Broadcast);
            }
            else if (param0 == "clear")
            {
                session.Player.SmartSalvageFilter = null;
                CommandHandlerHelper.WriteOutputInfo(session, $"All materials removed from the salvage filter.", ChatMessageType.Broadcast);
            }
            else
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Could not parse command, check \"/SmartSalvage help\" for usage instructions.", ChatMessageType.Broadcast);
                return;
            }
        }

        [CommandHandler("food", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Show the character's current hunger status.")]
        [CommandHandler("hunger", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Show the character's current hunger status.")]
        public static void HandleHunger(Session session, params string[] parameters)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat($"Unknown command: food", ChatMessageType.Help));
                return;
            }

            var player = session.Player;

            var health = player.ExtraHealthRegenPool ?? 0;
            var stamina = player.ExtraStaminaRegenPool ?? 0;
            var mana = player.ExtraManaRegenPool ?? 0;
            var max = Creature.MaxRegenPoolValue;

            var healthFullness = "";
            if (health >= max)
                healthFullness = $"Completely Full";
            else if (health >= max * 0.75f)
                healthFullness = $"Pretty Full";
            else if (health >= max * 0.5f)
                healthFullness = $"Satiated";
            else if (health >= max * 0.25f)
                healthFullness = $"Hungry";
            else
                healthFullness = $"Very hungry";

            var staminaFullness = "";
            if (stamina >= max)
                staminaFullness = $"Completely Full";
            else if (stamina >= max * 0.75f)
                staminaFullness = $"Pretty Full";
            else if (stamina >= max * 0.5f)
                staminaFullness = $"Satiated";
            else if (stamina >= max * 0.25f)
                staminaFullness = $"Hungry";
            else
                staminaFullness = $"Very hungry";

            var manaFullness = "";
            if (mana >= max)
                manaFullness = $"Completely Full";
            else if (mana >= max * 0.75f)
                manaFullness = $"Pretty Full";
            else if (mana >= max * 0.5f)
                manaFullness = $"Satiated";
            else if (mana >= max * 0.25f)
                manaFullness = $"Hungry";
            else
                manaFullness = $"Very hungry";

            CommandHandlerHelper.WriteOutputInfo(session, $"Health Food: {healthFullness} ({health:N0}/{max:N0})", ChatMessageType.Broadcast);
            CommandHandlerHelper.WriteOutputInfo(session, $"Stamina Food: {staminaFullness} ({stamina:N0}/{max:N0})", ChatMessageType.Broadcast);
            CommandHandlerHelper.WriteOutputInfo(session, $"Mana Food: {manaFullness} ({mana:N0}/{max:N0})", ChatMessageType.Broadcast);
        }

        [CommandHandler("Exploration", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "")]
        [CommandHandler("Exp", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "")]
        public static void HandleExploration(Session session, params string[] parameters)
        {
            var player = session?.Player;

            if (player == null)
                return;

            var hasAssignments = false;
            var assignment1Complete = false;
            var assignment2Complete = false;
            var assignment3Complete = false;
            if (player.Exploration1LandblockId != 0 && player.Exploration1Description.Length > 0)
            {
                hasAssignments = true;
                assignment1Complete = player.Exploration1LandblockReached && player.Exploration1KillProgressTracker <= 0 && player.Exploration1MarkerProgressTracker <= 0;
            }
            if (player.Exploration2LandblockId != 0 && player.Exploration2Description.Length > 0)
            {
                hasAssignments = true;
                assignment2Complete = player.Exploration2LandblockReached && player.Exploration2KillProgressTracker <= 0 && player.Exploration2MarkerProgressTracker <= 0;
            }
            if (player.Exploration3LandblockId != 0 && player.Exploration3Description.Length > 0)
            {
                hasAssignments = true;
                assignment3Complete = player.Exploration3LandblockReached && player.Exploration3KillProgressTracker <= 0 && player.Exploration3MarkerProgressTracker <= 0;
            }

            if (!hasAssignments)
                CommandHandlerHelper.WriteOutputInfo(session, "No ongoing exploration assignments at the moment.");
            else
            {
                var msg1 = "";
                var msg2 = "";
                var msg3 = "";
                if (player.Exploration1LandblockId != 0 && player.Exploration1Description.Length > 0)
                    msg1 = $"{player.Exploration1Description} {(assignment1Complete ? "Complete!" : $"Reached: {(player.Exploration1LandblockReached ? "Yes" : "No")}. Kills remaining: {player.Exploration1KillProgressTracker} Markers remaining: {player.Exploration1MarkerProgressTracker}")}";
                if (player.Exploration2LandblockId != 0 && player.Exploration2Description.Length > 0)
                    msg2 = $"{player.Exploration2Description} {(assignment2Complete ? "Complete!" : $"Reached: {(player.Exploration2LandblockReached ? "Yes" : "No")}. Kills remaining: {player.Exploration2KillProgressTracker} Markers remaining: {player.Exploration2MarkerProgressTracker}")}";
                if (player.Exploration3LandblockId != 0 && player.Exploration3Description.Length > 0)
                    msg3 = $"{player.Exploration3Description} {(assignment3Complete ? "Complete!" : $"Reached: {(player.Exploration3LandblockReached ? "Yes" : "No")}. Kills remaining: {player.Exploration3KillProgressTracker} Markers remaining: {player.Exploration3MarkerProgressTracker}")}";

                var count = 0;
                CommandHandlerHelper.WriteOutputInfo(session, "Exploration Assignments:");
                if (msg1.Length > 0)
                {
                    count++;
                    CommandHandlerHelper.WriteOutputInfo(session, $"\n\n{count:N0}. {msg1}");
                }
                if (msg2.Length > 0)
                {
                    count++;
                    CommandHandlerHelper.WriteOutputInfo(session, $"\n\n{count:N0}. {msg2}");
                }
                if (msg3.Length > 0)
                {
                    count++;
                    CommandHandlerHelper.WriteOutputInfo(session, $"\n\n{count:N0}. {msg3}");
                }
            }
        }

        [CommandHandler("HotDungeon", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "")]
        [CommandHandler("Hot", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "")]
        public static void HandleHotDungeon(Session session, params string[] parameters)
        {
            ShowHotDungeon(session, false);
        }

        public static void ShowHotDungeon(Session session, bool failSilently, ulong discordChannel = 0)
        {
            var msg = HotDungeonManager.GetStatusMessage();
            if (HotDungeonManager.ActiveDungeons.Count == 0 && failSilently)
                return;

            if (discordChannel == 0)
                CommandHandlerHelper.WriteOutputInfo(session, msg);
            else
                DiscordChatBridge.SendMessage(discordChannel, msg);
        }


        [CommandHandler("FireSaleTown", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "")]
        [CommandHandler("FireSale", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "")]
        public static void HandleFireSaleTown(Session session, params string[] parameters)
        {
            ShowFireSaleTown(session, false);
        }

        public static void ShowFireSaleTown(Session session, bool failSilently, ulong discordChannel = 0)
        {
            if (EventManager.FireSaleTownName == "")
            {
                if (!failSilently)
                {
                    var msg = "There's no towns with an ongoing fire sale at the moment.";
                    if (discordChannel == 0)
                        CommandHandlerHelper.WriteOutputInfo(session, msg);
                    else
                        DiscordChatBridge.SendMessage(discordChannel, msg);
                }
            }
            else
            {
                var timeRemaining = TimeSpan.FromSeconds(EventManager.NextFireSaleTownEnd - Time.GetUnixTime()).GetFriendlyString();
                var msg = $"{EventManager.FireSaleTownDescription} Time Remaining: {timeRemaining}.";
                if (discordChannel == 0)
                    CommandHandlerHelper.WriteOutputInfo(session, msg);
                else
                    DiscordChatBridge.SendMessage(discordChannel, msg);
            }
        }

        public struct LeaderboardEntry
        {
            public string Name;
            public int Level;
            public string KillerName;
            public int KillerLevel;
            public bool WasPvP;
            public int HardcoreKills;
            public int PKKills;
            public long XP;
            public bool Living;
            public bool isPK;
        }

        public static List<LeaderboardEntry> PrepareLeaderboard(GameplayModes gameplayMode, bool onlyLiving)
        {
            var living = PlayerManager.FindAllByGameplayMode(gameplayMode);

            var leaderboard = new List<LeaderboardEntry>();
            foreach (var entry in living)
            {
                if (entry.Account.AccessLevel > 0)
                    continue;

                var level = entry.GetProperty(PropertyInt.Level) ?? 1;

                var leaderboardEntry = new LeaderboardEntry();
                leaderboardEntry.Living = true;
                leaderboardEntry.Name = entry.GetProperty(PropertyString.Name);
                leaderboardEntry.Level = level;
                leaderboardEntry.XP = entry.GetProperty(PropertyInt64.TotalExperience) ?? 0;
                leaderboardEntry.HardcoreKills = entry.GetProperty(PropertyInt.PlayerKillsPkl) ?? 0;
                leaderboardEntry.PKKills = entry.GetProperty(PropertyInt.PlayerKillsPk) ?? 0;
                leaderboardEntry.isPK = entry.GetProperty(PropertyInt.PlayerKillerStatus) == (int)PlayerKillerStatus.PKLite || entry.GetProperty(PropertyInt.PlayerKillerStatus) == (int)PlayerKillerStatus.PK;

                leaderboard.Add(leaderboardEntry);
            }

            if (!onlyLiving)
            {
                var obituaryEntries = DatabaseManager.Shard.BaseDatabase.GetCharacterObituaryByGameplayMode(gameplayMode);
                foreach (var entry in obituaryEntries)
                {
                    if (entry.GameplayMode == (int)gameplayMode)
                    {
                        var leaderboardEntry = new LeaderboardEntry();
                        leaderboardEntry.Living = false;
                        leaderboardEntry.Name = entry.CharacterName;
                        leaderboardEntry.Level = entry.CharacterLevel;
                        leaderboardEntry.KillerName = entry.KillerName;
                        leaderboardEntry.KillerLevel = entry.KillerLevel;
                        leaderboardEntry.WasPvP = entry.WasPvP;
                        leaderboardEntry.XP = entry.XP;
                        leaderboardEntry.HardcoreKills = entry.Kills;
                        leaderboardEntry.isPK = gameplayMode == GameplayModes.HardcorePK;

                        leaderboard.Add(leaderboardEntry);
                    }
                }
            }

            return leaderboard;
        }

        /// <summary>
        /// List top 10 Hardcore characters by total XP
        /// </summary>
        [CommandHandler("HCXP", AccessLevel.Player, CommandHandlerFlag.None, "List top 10 Hardcore characters by total XP.", "HCXP [pk|npk] [alltime|living]")]
        public static void HandleLeaderboardHCXP(Session session, params string[] parameters)
        {
            if (session != null)
            {
                if (session.AccessLevel == AccessLevel.Player && DateTime.UtcNow - session.Player.PrevLeaderboardHCXPCommandRequestTimestamp < TimeSpan.FromMinutes(1))
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat("You have used this command too recently!", ChatMessageType.Broadcast));
                    return;
                }
                session.Player.PrevLeaderboardHCXPCommandRequestTimestamp = DateTime.UtcNow;
            }

            bool IsPkLeaderboard = true;
            bool onlyLiving = false;
            foreach (var par in parameters)
            {
                if (par == "npk")
                    IsPkLeaderboard = false;
                else if (par == "living")
                    onlyLiving = true;
            }

            ulong discordChannel = 0;
            if (parameters.Length > 3 && parameters[2] == "discord")
                ulong.TryParse(parameters[3], out discordChannel);

            var leaderboard = PrepareLeaderboard(IsPkLeaderboard ? GameplayModes.HardcorePK : GameplayModes.HardcoreNPK, onlyLiving).OrderByDescending(b => b.XP).ToList();

            StringBuilder message = new StringBuilder();
            message.Append($"Hardcore {(IsPkLeaderboard ? "PK" : "NPK")} {(onlyLiving ? "Living" : "All-Time")} Characters by XP: \n");
            message.Append("-----------------------\n");
            uint playerCounter = 1;
            foreach (var entry in leaderboard)
            {
                var label = playerCounter < 10 ? $" {playerCounter}." : $"{playerCounter}.";
                var deathStatus = onlyLiving ? "" : $"{(entry.Living ? " - Living" : $" - Killed by {entry.KillerName}{(entry.KillerLevel > 0 && entry.WasPvP ? $"(Level {entry.KillerLevel})" : "")}")}";
                message.Append($"{label} {entry.Name} - Level {entry.Level}{deathStatus}\n");
                playerCounter++;

                if (playerCounter > 10)
                    break;
            }
            message.Append("-----------------------\n");

            if (discordChannel == 0)
                CommandHandlerHelper.WriteOutputInfo(session, message.ToString(), ChatMessageType.Broadcast);
            else
                DiscordChatBridge.SendMessage(discordChannel, $"`{message.ToString()}`");
        }

        [CommandHandler("TopNPC", AccessLevel.Player, CommandHandlerFlag.None, "List top 10 NPCs by total kills.", "TopNPC [minLevel] [maxLevel]")]
        public static void HandleLeaderboardTopNPC(Session session, params string[] parameters)
        {
            if (session != null)
            {
                if (session.AccessLevel == AccessLevel.Player && DateTime.UtcNow - session.Player.PrevLeaderboardHCXPCommandRequestTimestamp < TimeSpan.FromMinutes(1))
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat("You have used this command too recently!", ChatMessageType.Broadcast));
                    return;
                }
                session.Player.PrevLeaderboardHCXPCommandRequestTimestamp = DateTime.UtcNow;
            }

            int minLevel = 1;
            if (parameters.Length > 0)
                int.TryParse(parameters[0], out minLevel);

            int maxLevel = 275;
            if (parameters.Length > 1)
                int.TryParse(parameters[1], out maxLevel);

            ulong discordChannel = 0;
            if (parameters.Length > 3 && parameters[2] == "discord")
                ulong.TryParse(parameters[3], out discordChannel);

            var leaderboard = new Dictionary<string, int>();
            var obituaryEntries = DatabaseManager.Shard.BaseDatabase.GetCharacterObituary();
            foreach (var entry in obituaryEntries)
            {
                if (entry.CharacterLevel < minLevel || entry.CharacterLevel > maxLevel)
                    continue;

                if (!entry.WasPvP)
                {
                    if (!leaderboard.TryGetValue(entry.KillerName, out var kills))
                        leaderboard.Add(entry.KillerName, 1);
                    else
                        leaderboard[entry.KillerName]++;
                }
            }

            var sorted = from entry in leaderboard orderby entry.Value descending select entry;

            StringBuilder message = new StringBuilder();
            message.Append($"Top Character Killers - Levels {minLevel} to {maxLevel}:\n");
            message.Append("-----------------------\n");
            uint counter = 1;
            foreach (var entry in sorted)
            {
                var label = counter < 10 ? $" {counter}." : $"{counter}.";
                message.Append($"{label} {entry.Key} - {entry.Value} kill{(entry.Value != 1 ? "s" : "")}\n");
                counter++;

                if (counter > 10)
                    break;
            }
            message.Append("-----------------------\n");

            if (discordChannel == 0)
                CommandHandlerHelper.WriteOutputInfo(session, message.ToString(), ChatMessageType.Broadcast);
            else
                DiscordChatBridge.SendMessage(discordChannel, $"`{message.ToString()}`");
        }

        [CommandHandler("HCTopNPC", AccessLevel.Player, CommandHandlerFlag.None, "List top 10 NPCs by total kills.", "HCTopNPC [minLevel] [maxLevel]")]
        public static void HandleLeaderboardHCTopNPC(Session session, params string[] parameters)
        {
            if (session != null)
            {
                if (session.AccessLevel == AccessLevel.Player && DateTime.UtcNow - session.Player.PrevLeaderboardHCXPCommandRequestTimestamp < TimeSpan.FromMinutes(1))
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat("You have used this command too recently!", ChatMessageType.Broadcast));
                    return;
                }
                session.Player.PrevLeaderboardHCXPCommandRequestTimestamp = DateTime.UtcNow;
            }

            int minLevel = 1;
            if (parameters.Length > 0)
                int.TryParse(parameters[0], out minLevel);

            int maxLevel = 275;
            if (parameters.Length > 1)
                int.TryParse(parameters[1], out maxLevel);

            ulong discordChannel = 0;
            if (parameters.Length > 3 && parameters[2] == "discord")
                ulong.TryParse(parameters[3], out discordChannel);

            var leaderboard = new Dictionary<string, int>();
            var obituaryEntries = DatabaseManager.Shard.BaseDatabase.GetCharacterObituaryByGameplayMode(GameplayModes.HardcoreNPK);
            obituaryEntries.AddRange(DatabaseManager.Shard.BaseDatabase.GetCharacterObituaryByGameplayMode(GameplayModes.HardcorePK));
            foreach (var entry in obituaryEntries)
            {
                if (entry.CharacterLevel < minLevel || entry.CharacterLevel > maxLevel)
                    continue;

                if (!entry.WasPvP)
                {
                    if (!leaderboard.TryGetValue(entry.KillerName, out var kills))
                        leaderboard.Add(entry.KillerName, 1);
                    else
                        leaderboard[entry.KillerName]++;
                }
            }

            var sorted = from entry in leaderboard orderby entry.Value descending select entry;

            StringBuilder message = new StringBuilder();
            message.Append($"Top Hardcore Character Killers - Levels {minLevel} to {maxLevel}:\n");
            message.Append("-----------------------\n");
            uint counter = 1;
            foreach (var entry in sorted)
            {
                var label = counter < 10 ? $" {counter}." : $"{counter}.";
                message.Append($"{label} {entry.Key} - {entry.Value} kill{(entry.Value != 1 ? "s" : "")}\n");
                counter++;

                if (counter > 10)
                    break;
            }
            message.Append("-----------------------\n");

            if (discordChannel == 0)
                CommandHandlerHelper.WriteOutputInfo(session, message.ToString(), ChatMessageType.Broadcast);
            else
                DiscordChatBridge.SendMessage(discordChannel, $"`{message.ToString()}`");
        }

        /// <summary>
        /// List top 10 Solo Self-Found characters by total XP
        /// </summary>
        [CommandHandler("TopSSF", AccessLevel.Player, CommandHandlerFlag.None, "List top 10 Solo Self-Found characters by total XP.", "TopSSF")]
        public static void HandleLeaderboardSSF(Session session, params string[] parameters)
        {
            if (session != null)
            {
                if (session.AccessLevel == AccessLevel.Player && DateTime.UtcNow - session.Player.PrevLeaderboardSSFCommandRequestTimestamp < TimeSpan.FromMinutes(1))
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat("You have used this command too recently!", ChatMessageType.Broadcast));
                    return;
                }
                session.Player.PrevLeaderboardSSFCommandRequestTimestamp = DateTime.UtcNow;
            }

            ulong discordChannel = 0;
            if (parameters.Length > 1 && parameters[0] == "discord")
                ulong.TryParse(parameters[1], out discordChannel);

            var leaderboard = PrepareLeaderboard(GameplayModes.SoloSelfFound, true).OrderByDescending(b => b.XP).ToList();

            StringBuilder message = new StringBuilder();
            message.Append($"Solo Self-Found Characters by XP: \n");
            message.Append("-----------------------\n");
            uint playerCounter = 1;
            foreach (var entry in leaderboard)
            {
                if (entry.XP > 0)
                {
                    var label = playerCounter < 10 ? $" {playerCounter}." : $"{playerCounter}.";
                    message.Append($"{label} {entry.Name} - Level {entry.Level}\n");
                    playerCounter++;

                    if (playerCounter > 10)
                        break;
                }
            }
            message.Append("-----------------------\n");

            if (discordChannel == 0)
                CommandHandlerHelper.WriteOutputInfo(session, message.ToString(), ChatMessageType.Broadcast);
            else
                DiscordChatBridge.SendMessage(discordChannel, $"`{message.ToString()}`");
        }

        /// <summary>
        /// List top 10 characters by total XP
        /// </summary>
        [CommandHandler("TopXP", AccessLevel.Player, CommandHandlerFlag.None, "List top 10 characters by total XP.", "TopXP")]
        public static void HandleLeaderboardLevel(Session session, params string[] parameters)
        {
            if (session != null)
            {
                if (session.AccessLevel == AccessLevel.Player && DateTime.UtcNow - session.Player.PrevLeaderboardXPCommandRequestTimestamp < TimeSpan.FromMinutes(1))
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat("You have used this command too recently!", ChatMessageType.Broadcast));
                    return;
                }
                session.Player.PrevLeaderboardXPCommandRequestTimestamp = DateTime.UtcNow;
            }

            ulong discordChannel = 0;
            if (parameters.Length > 1 && parameters[0] == "discord")
                ulong.TryParse(parameters[1], out discordChannel);

            var leaderboard = PrepareLeaderboard(GameplayModes.Regular, true).OrderByDescending(b => b.XP).ToList();

            StringBuilder message = new StringBuilder();
            message.Append($"Top Characters by XP: \n");
            message.Append("-----------------------\n");
            uint playerCounter = 1;
            foreach (var entry in leaderboard)
            {
                if (entry.XP > 0)
                {
                    var label = playerCounter < 10 ? $" {playerCounter}." : $"{playerCounter}.";
                    var pkStatus = entry.isPK ? "(PK)" : "";
                    message.Append($"{label} {entry.Name} - Level {entry.Level}{pkStatus}\n");
                    playerCounter++;

                    if (playerCounter > 10)
                        break;
                }
            }
            message.Append("-----------------------\n");

            if (discordChannel == 0)
                CommandHandlerHelper.WriteOutputInfo(session, message.ToString(), ChatMessageType.Broadcast);
            else
                DiscordChatBridge.SendMessage(discordChannel, $"`{message.ToString()}`");
        }

        /// <summary>
        /// List top 10 characters by total kills
        /// </summary>
        [CommandHandler("TopPvP", AccessLevel.Player, CommandHandlerFlag.None, "List top 10 characters by total kills.", "TopPvP")]
        public static void HandleLeaderboardPvP(Session session, params string[] parameters)
        {
            if (session != null)
            {
                if (session.AccessLevel == AccessLevel.Player && DateTime.UtcNow - session.Player.PrevLeaderboardPvPCommandRequestTimestamp < TimeSpan.FromMinutes(1))
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat("You have used this command too recently!", ChatMessageType.Broadcast));
                    return;
                }
                session.Player.PrevLeaderboardPvPCommandRequestTimestamp = DateTime.UtcNow;
            }

            ulong discordChannel = 0;
            if (parameters.Length > 1 && parameters[0] == "discord")
                ulong.TryParse(parameters[1], out discordChannel);

            var leaderboard = PrepareLeaderboard(GameplayModes.Regular, true).OrderByDescending(b => b.PKKills).ToList();

            StringBuilder message = new StringBuilder();
            message.Append($"Top Characters by Kills: \n");
            message.Append("-----------------------\n");
            uint playerCounter = 1;
            foreach (var entry in leaderboard)
            {
                if (entry.PKKills > 0)
                {
                    var label = playerCounter < 10 ? $" {playerCounter}." : $"{playerCounter}.";
                    var pkStatus = entry.isPK ? "(PK)" : "";
                    message.Append($"{label} {entry.Name} - Level {entry.Level}{pkStatus} - {entry.PKKills} kill{(entry.PKKills != 1 ? "s" : "")}\n");
                    playerCounter++;

                    if (playerCounter > 10)
                        break;
                }
            }
            message.Append("-----------------------\n");

            if (discordChannel == 0)
                CommandHandlerHelper.WriteOutputInfo(session, message.ToString(), ChatMessageType.Broadcast);
            else
                DiscordChatBridge.SendMessage(discordChannel, $"`{message.ToString()}`");
        }

        /// <summary>
        /// Reports on the top 10 Hardcore characters by PvP kills
        /// </summary>
        [CommandHandler("HCPvP", AccessLevel.Player, CommandHandlerFlag.None, "Reports on the top 10 Hardcore characters by PvP kills.", "HCPvP [alltime|living]")]
        public static void HandleLeaderboardHCPvP(Session session, params string[] parameters)
        {
            if (session != null)
            {
                if (session.AccessLevel == AccessLevel.Player && DateTime.UtcNow - session.Player.PrevLeaderboardHCPvPCommandRequestTimestamp < TimeSpan.FromMinutes(1))
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat("You have used this command too recently!", ChatMessageType.Broadcast));
                    return;
                }
                session.Player.PrevLeaderboardHCPvPCommandRequestTimestamp = DateTime.UtcNow;
            }

            bool onlyLiving = false;
            if (parameters.Length > 0 && parameters[0] == "living")
                onlyLiving = true;

            ulong discordChannel = 0;
            if (parameters.Length > 2 && parameters[1] == "discord")
                ulong.TryParse(parameters[2], out discordChannel);

            var living = PlayerManager.FindAllByGameplayMode(GameplayModes.HardcorePK);

            var leaderboard = PrepareLeaderboard(GameplayModes.HardcorePK, onlyLiving).OrderByDescending(b => b.HardcoreKills).ToList();

            leaderboard = leaderboard.OrderByDescending(b => b.HardcoreKills).ToList();

            StringBuilder message = new StringBuilder();
            message.Append($"Hardcore {(onlyLiving ? "Living" : "All-Time")} PvP Leaderboard:\n");
            message.Append("-------------------------\n");
            uint playerCounter = 1;
            foreach (var entry in leaderboard)
            {
                if (entry.HardcoreKills > 0)
                {
                    var label = playerCounter < 10 ? $" {playerCounter}." : $"{playerCounter}.";
                    var deathStatus = onlyLiving ? "" : $"{(entry.Living ? " - Living" : $" - Killed by {entry.KillerName}{(entry.KillerLevel > 0 && entry.WasPvP ? $"(Level {entry.KillerLevel})" : "")}")}";
                    message.Append($"{label} {entry.Name} - Level {entry.Level} - {entry.HardcoreKills} kill{(entry.HardcoreKills != 1 ? "s" : "")}{deathStatus}\n");
                    playerCounter++;

                    if (playerCounter > 10)
                        break;
                }
            }
            message.Append("-------------------------\n");

            if (discordChannel == 0)
                CommandHandlerHelper.WriteOutputInfo(session, message.ToString(), ChatMessageType.Broadcast);
            else
                DiscordChatBridge.SendMessage(discordChannel, $"`{message.ToString()}`");
        }

        [CommandHandler("OfflineSwear", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 1, "Swear allegiance to an offline character on the same account.", "OfflineSwear <PatronName>")]
        public static void HandleOfflineSwear(Session session, params string[] parameters)
        {
            var patronName = string.Join(" ", parameters);

            var onlinePlayer = PlayerManager.GetOnlinePlayer(patronName);
            var offlinePlayer = PlayerManager.GetOfflinePlayer(patronName, false);
            if (onlinePlayer != null)
            {
                if (onlinePlayer.Account.AccountId != session.AccountId)
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"The target character must be on the same account as this character!");
                    return;
                }
                else
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"That character is not offline!");
                    return;
                }
            }
            else if (offlinePlayer == null)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Could not find a character with that name!");
                return;
            }
            else if (offlinePlayer.Account.AccountId != session.AccountId)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"The target character must be on the same account as this character!");
                return;
            }
            else if (offlinePlayer.IsPendingDeletion || offlinePlayer.IsDeleted)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"The target must not be a deleted character!");
                return;
            }
            else
                session.Player.OfflineSwearAllegiance(offlinePlayer.Guid.Full);
        }

        [CommandHandler("AutoFillComps", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 1, "Automatically adds components to fillcomps according to the current spellbook.", "AutoFillComps <Amount Multiplier|Clear>")]
        public static void HandleAutoFillComps(Session session, params string[] parameters)
        {
            var player = session.Player;
            if (player == null)
                return;

            if (parameters[0].ToLower() == "clear")
            {
                player.Character.ClearFillComponents(player.CharacterDatabaseLock);
            }
            else
            {
                if (!int.TryParse(parameters[0], out var multiplier))
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"The syntax of the command is incorrect.\nUsage: @AutoFillComps <Amount Multiplier|Clear>");
                    return;
                }

                multiplier = Math.Clamp(multiplier, 0, 5000);

                foreach (var spell in player.Biota.GetKnownSpellsIds(player.BiotaDatabaseLock))
                {
                    Spell spellEntity = new Spell(spell);
                    foreach (var componentId in spellEntity.Formula.Components)
                    {
                        int amount = Math.Min(10 * multiplier, 5000);
                        if (SpellFormula.SpellComponentsTable.SpellComponents.TryGetValue(componentId, out var spellComponent))
                        {
                            if (spellComponent.Type == (int)SpellComponentsTable.Type.Scarab || spellComponent.Type == (int)SpellComponentsTable.Type.Talisman)
                                amount = Math.Min(3 * multiplier, 5000);

                            var compWcid = Spell.GetComponentWCID(componentId);
                            if (SpellComponent.IsValid(compWcid))
                            {
                                player.Character.TryRemoveFillComponent(compWcid, out _, player.CharacterDatabaseLock);
                                player.Character.AddFillComponent(compWcid, (uint)amount, player.CharacterDatabaseLock, out _);
                            }
                        }
                    }
                }
            }

            CommandHandlerHelper.WriteOutputInfo(session, $"Fillcomps values updated. Please relog to see the updated values.");
        }


        [CommandHandler("tickets", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0, "Displays a count of your remaining Arcanum Portal Network tickets")]
        public static void HandleTickets(Session session, params string[] parameters)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat($"Unknown command: Tickets", ChatMessageType.Help));
                return;
            }

            session.Network.EnqueueSend(new GameMessageSystemChat($"You have {session.Player.QuestManager.GetCurrentSolves("ArcanumPortalAccess")} tickets remaining for the Arcanum Portal Network.", ChatMessageType.Broadcast));
        }

        [CommandHandler("Where", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0, "Shows information about your current location")]
        public static void HandleWhere(Session session, params string[] parameters)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat($"Unknown command: Where", ChatMessageType.Help));
                return;
            }

            var player = session.Player;
            if (player == null)
                return;

            var landblockDescription = DatabaseManager.World.GetLandblockDescriptionsByLandblock(player.CurrentLandblock.Id.Landblock).FirstOrDefault();

            if (landblockDescription != null)
                CommandHandlerHelper.WriteOutputInfo(session, $"Current Location: {landblockDescription.Name}\nDirections: {landblockDescription.Directions}\nReference: {landblockDescription.Reference}\nMacro Region: {landblockDescription.MacroRegion}\nMicro Region: {landblockDescription.MicroRegion}");
            else
                CommandHandlerHelper.WriteOutputInfo(session, $"You are at an unknown location.");
        }

        [CommandHandler("FollowRoad", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0, "Follows the nearest road")]
        public static void HandleFollowRoad(Session session, params string[] parameters)
        {
            if (Common.ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat($"Unknown command: FollowRoad", ChatMessageType.Help));
                return;
            }

            var player = session.Player;
            if (player == null)
                return;

            var landblock = player.CurrentLandblock;

            if (landblock == null)
                return;

            var forwardPosition = player.Location.InFrontOf(LandDefs.CellLength * 1.5);

            var distanceToRoad = landblock.GetDistanceToNearestRoad(forwardPosition, out var roadPosition, player.Location);

            var previousRoadPositions = new List<uint>();
            previousRoadPositions.Add(roadPosition.Cell);
            if (roadPosition != null && distanceToRoad < LandDefs.CellLength * 2)
            {
                var dir = Math.Abs((int)Math.Floor(player.Location.PhysPosition().heading_diff(roadPosition.PhysPosition())));
                var heightDiff = Math.Abs(player.Location.PositionZ - roadPosition.PositionZ);
                var counter = 0;
                if (dir < 3 && heightDiff < 25)
                {
                    for (int maxSections = 0; maxSections < 30; maxSections++)
                    {
                        forwardPosition = forwardPosition.InFrontOf(LandDefs.CellLength * 1.5);
                        landblock.GetDistanceToNearestRoad(forwardPosition, out var nextRoadPosition, roadPosition);

                        if (nextRoadPosition == null)
                            break;

                        dir = Math.Abs((int)Math.Floor(player.Location.PhysPosition().heading_diff(nextRoadPosition.PhysPosition())));
                        heightDiff += Math.Abs(player.Location.PositionZ - roadPosition.PositionZ);
                        if (!previousRoadPositions.Contains(nextRoadPosition.Cell) && dir < 3 && heightDiff < 25)
                        {
                            session.Network.EnqueueSend(new GameMessageSystemChat($"{nextRoadPosition.ToLOCString()}", ChatMessageType.Help));
                            counter++;
                            previousRoadPositions.Add(nextRoadPosition.Cell);
                            roadPosition = nextRoadPosition;
                        }
                        else
                            break;
                    }
                }

                player.CreateMoveToChain(roadPosition, (success) =>
                {
                    if (success)
                    {
                        var actionChain = new ActionChain();
                        actionChain.AddDelaySeconds(0.1);
                        actionChain.AddAction(session.Player, () => HandleFollowRoad(session));
                        actionChain.EnqueueChain();
                    }
                }, 2);
            }
            else
                session.Network.EnqueueSend(new GameMessageSystemChat($"Can't find a road to follow.", ChatMessageType.Help));
        }

        [CommandHandler("fso", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Fixes stuck item in the offhand.")]
        [CommandHandler("fixStuckOffhand", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Fixes stuck items in the offhand.")]
        public static void HandleFixStuckOffhand(Session session, params string[] parameters)
        {
            session.Network.EnqueueSend(new GameMessageSystemChat("Attempting to fix stuck offhand.", ChatMessageType.Broadcast));

            session.Player.FixStuckEquippedItemIcon(EquipMask.Shield);
        }

        //private const int renameBaseCost = 200;
        //private const int renameMaxCost = 20000;
        //// buyrename <New Name>
        //[CommandHandler("buyrename", AccessLevel.Player, CommandHandlerFlag.None, 1,
        //    "Purchase a character rename. The first rename is free, second rename costs 200 PK trophies, and the cost of the 3rd rename and above grows exponentially, capped at 20k.",
        //    "< New Name >")]
        //public static void HandleBuyRename(Session session, params string[] parameters)
        //{
        //    if (!CheckPlayerCommandRateLimit(session))
        //        return;

        //    if(parameters.Length < 1)
        //    {
        //        CommandHandlerHelper.WriteOutputInfo(session, $"Invalid parameters: please provide a new character name. Usage: /BuyRename <NewCharacterName>", ChatMessageType.Broadcast);
        //        return;
        //    }

        //    int renameCost = CalculateRenameCost(session.Player.CharacterRenameCount);
        //    bool isFirstRenameUsed = session.Player.CharacterRenameCount > 0;
        //    var numPkTrophiesInInventory = session.Player.GetNumInventoryItemsOfWCID(CustomWeenieId.PkTrophy);
        //    if(isFirstRenameUsed && numPkTrophiesInInventory < renameCost)
        //    {
        //        CommandHandlerHelper.WriteOutputInfo(session, $"Your character has previously been renamed {session.Player.CharacterRenameCount} times. Renaming your character costs {renameCost} PK trophies. You don't have enough PK trophies in your inventory to cover the cost.", ChatMessageType.Broadcast);
        //        return;
        //    }

        //    var newName = string.Join(" ", parameters).Trim();
        //    var oldName = session.Player.Name;

        //    if (oldName.StartsWith("+"))
        //        oldName = oldName.Substring(1);
        //    if (newName.StartsWith("+"))
        //        newName = newName.Substring(1);

        //    newName = newName.First().ToString().ToUpper() + newName.Substring(1);

        //    //Verify the new name is not in the taboo table
        //    if (PropertyManager.GetBool("taboo_table").Item && DatManager.PortalDat.TabooTable.ContainsBadWord(newName.ToLowerInvariant()))
        //    {
        //        CommandHandlerHelper.WriteOutputInfo(session, $"Error, unable to rename your character to \"{newName}\" as that name is not allowed per the taboo table.", ChatMessageType.Broadcast);
        //        return;
        //    }

        //    if (PropertyManager.GetBool("creature_name_check").Item && DatabaseManager.World.IsCreatureNameInWorldDatabase(newName))
        //    {
        //        CommandHandlerHelper.WriteOutputInfo(session, $"Error, unable to rename your character to \"{newName}\" as that name matches a creature name.", ChatMessageType.Broadcast);
        //        return;
        //    }

        //    //Verify the new name has only alpha characters, apostrophies or dashes, and isn't more than 32 characters
        //    if(newName.Length > 32)
        //    {
        //        CommandHandlerHelper.WriteOutputInfo(session, $"Error, unable to rename your character to \"{newName}\" as that name exceeds the maximum 32 character limit.", ChatMessageType.Broadcast);
        //        return;
        //    }

        //    var hasInvalidChars = false;
        //    var hasAtLeastOneLetter = false;
        //    foreach(Char c in newName)
        //    {
        //        if(!Char.IsLetter(c) && c != '\'' && c != '-' && c != ' ')
        //        {
        //            hasInvalidChars = true;
        //        }

        //        if(Char.IsLetter(c))
        //        {
        //            hasAtLeastOneLetter = true;
        //        }
        //    }

        //    if (hasInvalidChars || !hasAtLeastOneLetter)
        //    {
        //        CommandHandlerHelper.WriteOutputInfo(session, $"Error, unable to rename your character to \"{newName}\" as that name contains invalid characters for a player name.  Player names may only contain characters A-Z, spaces, apostrophes or dashes and must contain at least one A-Z character.", ChatMessageType.Broadcast);
        //        return;
        //    }

        //    var onlinePlayer = PlayerManager.GetOnlinePlayer(oldName);
        //    if (onlinePlayer != null)
        //    {
        //        DatabaseManager.Shard.IsCharacterNameAvailable(newName, isAvailable =>
        //        {
        //            if (!isAvailable)
        //            {
        //                CommandHandlerHelper.WriteOutputInfo(session, $"Error, a player named \"{newName}\" already exists.", ChatMessageType.Broadcast);
        //                return;
        //            }

        //            //Check if the player has sufficient funds to purchase the rename
        //            numPkTrophiesInInventory = session.Player.GetNumInventoryItemsOfWCID(CustomWeenieId.PkTrophy);
        //            if (isFirstRenameUsed && numPkTrophiesInInventory < renameCost)
        //            {
        //                CommandHandlerHelper.WriteOutputInfo(session, $"Your character has previously been renamed {session.Player.CharacterRenameCount} times. Renaming your character costs {renameCost} PK trophies. You don't have enough PK trophies in your inventory to cover the cost.", ChatMessageType.Broadcast);
        //                return;
        //            }
        //            else
        //            {
        //                if (isFirstRenameUsed)
        //                {
        //                    if (session.Player.TryConsumeFromInventoryWithNetworking(CustomWeenieId.PkTrophy, renameCost))
        //                    {
        //                        CommandHandlerHelper.WriteOutputInfo(session, $"{renameCost} PK trophies have been removed from your inventory", ChatMessageType.Broadcast);
        //                    }
        //                    else
        //                    {
        //                        CommandHandlerHelper.WriteOutputInfo(session, $"Error: failed consuming {renameCost} PK trophies from your inventory. Please try again or contact an admin for support.", ChatMessageType.Broadcast);

        //                        //Log this failure to the audit log
        //                        PlayerManager.BroadcastToAuditChannel(session.Player, $"Error: player {session.Player.Name} used /BuyRename command, and was verified to have enough PK trophies, but failed to consume the PK trophies with TryConsumeFromInventoryWithNetworking.");
        //                        return;
        //                    }
        //                }
        //            }

        //            onlinePlayer.Character.Name = newName;
        //            onlinePlayer.CharacterChangesDetected = true;
        //            onlinePlayer.Name = newName;
        //            onlinePlayer.CharacterRenameCount += 1;
        //            onlinePlayer.SavePlayerToDatabase();

        //            CommandHandlerHelper.WriteOutputInfo(session, $"Player named \"{oldName}\" renamed to \"{newName}\" successfully!", ChatMessageType.Broadcast);

        //            PlayerManager.BroadcastToAuditChannel(session.Player, $"Player {oldName} used /BuyRename command to rename themselves to {newName} for a cost of {renameCost} PK Trophies.");

        //            onlinePlayer.Session.LogOffPlayer();
        //        });
        //    }
        //}

        //private static int CalculateRenameCost(int renameCount)
        //{
        //    return renameCount == 0 ? 0
        //         : renameCount == 1 ? 200
        //         : Math.Min((int)(renameBaseCost * Math.Pow(1.35, renameCount - 1)), renameMaxCost);
        //}

        private const int titleBaseCost = 200;
        // buytitle <New Title>
        [CommandHandler("buytitle", AccessLevel.Player, CommandHandlerFlag.None, 1,
            "Purchase a custom title for your character for PK trophies",
            "< New Title >")]
        public static void HandleBuyTitle(Session session, params string[] parameters)
        {
            if (!CheckPlayerCommandRateLimit(session))
                return;

            if (parameters.Length < 1)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Invalid parameters: please provide a new character title. Usage: /BuyTitle <NewCharacterTitle>", ChatMessageType.Broadcast);
                return;
            }

            var numPkTrophiesInInventory = session.Player.GetNumInventoryItemsOfWCID(CustomWeenieId.PkTrophy);
            if (numPkTrophiesInInventory < titleBaseCost)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Buying a custom title for your character costs {titleBaseCost} PK trophies. You don't have enough PK trophies in your inventory to cover the cost.", ChatMessageType.Broadcast);
                return;
            }

            var newTitle = string.Join(" ", parameters).Trim();

            if (newTitle.StartsWith("+"))
                newTitle = newTitle.Substring(1);

            //Verify the new title is not in the taboo table
            if (PropertyManager.GetBool("taboo_table").Item && DatManager.PortalDat.TabooTable.ContainsBadWord(newTitle.ToLowerInvariant()))
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Error, unable to set your character title to \"{newTitle}\" as that name is not allowed per the taboo table.", ChatMessageType.Broadcast);
                return;
            }

            //Verify the new title has only alpha characters, apostrophies or dashes, and isn't more than 32 characters
            if (newTitle.Length > 32)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Error, unable to set your character title to \"{newTitle}\" as that name exceeds the maximum 32 character limit.", ChatMessageType.Broadcast);
                return;
            }

            var hasInvalidChars = false;
            foreach (Char c in newTitle)
            {
                if (!Char.IsLetter(c) && c != '\'' && c != '-' && c != ' ')
                {
                    hasInvalidChars = true;
                }
            }

            if (hasInvalidChars)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Error, unable to set your character title to \"{newTitle}\" as that name contains invalid characters for a player title.  Player titles may only contain characters A-Z, spaces, apostrophes or dashes.", ChatMessageType.Broadcast);
                return;
            }

            var onlinePlayer = session.Player;
            if (onlinePlayer != null)
            {
                //Check if the player has sufficient funds to purchase the rename
                numPkTrophiesInInventory = session.Player.GetNumInventoryItemsOfWCID(CustomWeenieId.PkTrophy);
                if (numPkTrophiesInInventory < titleBaseCost)
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"Buying a custom title for your character costs {titleBaseCost} PK trophies. You don't have enough PK trophies in your inventory to cover the cost.", ChatMessageType.Broadcast);
                    return;
                }
                else
                {
                    if (session.Player.TryConsumeFromInventoryWithNetworking(CustomWeenieId.PkTrophy, titleBaseCost))
                    {
                        CommandHandlerHelper.WriteOutputInfo(session, $"{titleBaseCost} PK trophies have been removed from your inventory", ChatMessageType.Broadcast);
                        onlinePlayer.SetProperty(PropertyString.Template, newTitle);
                        onlinePlayer.RemoveProperty(PropertyInt.CharacterTitleId);
                        onlinePlayer.CharacterChangesDetected = true;
                        onlinePlayer.SavePlayerToDatabase();
                        CommandHandlerHelper.WriteOutputInfo(session, $"Your title has been changed to \"{newTitle}\" successfully!", ChatMessageType.Broadcast);
                    }
                    else
                    {
                        CommandHandlerHelper.WriteOutputInfo(session, $"Error: failed consuming {titleBaseCost} PK trophies from your inventory. Please try again or contact an admin for support.", ChatMessageType.Broadcast);

                        //Log this failure to the audit log
                        PlayerManager.BroadcastToAuditChannel(session.Player, $"Error: player {session.Player.Name} used /BuyTitle command, and was verified to have enough PK trophies, but failed to consume the PK trophies with TryConsumeFromInventoryWithNetworking.");
                        return;
                    }
                }
            }
        }

        public static bool CheckPlayerCommandRateLimit(Session session, int limitSeconds = 3)
        {
            if (session == null)
                return false;

            if (session.Player.LastPlayerCommandTimestamp.HasValue && Time.GetDateTimeFromTimestamp(session.Player.LastPlayerCommandTimestamp.Value) > DateTime.UtcNow.AddSeconds(-1 * limitSeconds))
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"To prevent abuse, you can only issue this player command every {limitSeconds} seconds. Please try again later.");
                return false;
            }
            else
            {
                session.Player.LastPlayerCommandTimestamp = Time.GetUnixTime(DateTime.UtcNow);
                return true;
            }
        }

        [CommandHandler("ForceLogoffStuckCharacter", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, "Force log off of a character that's stuck in game.  Is only allowed when initiated from a character that is on the same account as the target character.", "<stuck character name>")]
        public static void HandleForceLogoffStuckCharacter(Session session, params string[] parameters)
        {
            if (!CheckPlayerCommandRateLimit(session))
                return;

            var playerName = "";
            if (parameters.Length > 0)
                playerName = string.Join(" ", parameters);

            if (string.IsNullOrEmpty(playerName))
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Invalid parameters, please provide a player name for the character that needs to be logged off.");
                return;
            }

            var plr = PlayerManager.FindByName(playerName);
            if (plr == null)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Unable to force log off for {playerName}: Player not found.");
                return;
            }

            var target = PlayerManager.GetOnlinePlayer(plr.Guid);
            if (target == null)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Unable to force log off for {plr.Name}: Player is not online.");
                return;
            }

            // Verify the target is not the current player
            if (session.Player.Guid == target.Guid)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Unable to force log off for {plr.Name}: You cannot target yourself, please try with a different character on the same account.");
                return;
            }

            // Verify the target is on the same account as the current player
            if (session.AccountId != target.Account.AccountId)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Unable to force log off for {plr.Name}: Target must be within the same account as the player who issues the logoff command. Please reach out for admin support.");
                return;
            }

            DeveloperCommands.HandleForceLogoff(session, parameters);
        }

        #region Arena

        [CommandHandler("arena", AccessLevel.Player, CommandHandlerFlag.None, 1,
            "The arena command is used to join an arena event or get information about arena statistics")]
        public static void HandleArena(Session session, params string[] parameters)
        {
            log.Debug($"HandleArena called for player = {session.Player?.Name}, params = {string.Join(" ", parameters)}");

            if (!CheckPlayerCommandRateLimit(session))
                return;

            if (parameters.Count() < 1)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Invalid parameters.  See the arena help file below for valid parameters.");
                parameters[0] = "help";
            }

            var actionType = parameters[0];

            switch (actionType?.ToLower())
            {
                case "join":

                    string eventType = "1v1";
                    string param2 = string.Empty;
                    if (parameters.Length > 1)
                    {
                        eventType = parameters[1];

                        if (!ArenaManager.IsValidEventType(eventType))
                        {
                            CommandHandlerHelper.WriteOutputInfo(session, $"Invalid parameters.  The Join command does not support the event type {eventType}. Proper syntax is as follows...\n  To join a 1v1 arena match: /arena join\n  To join a specific type of arena match, replace eventType with the string code for the type of match you want to join, such as 1v1, 2v2, ffa or tugak. : /arena join eventType\n  To get your current character's stats: /arena stats\n  To get a named character's stats, replace characterName with the target character's name: /arena stats characterName");
                            return;
                        }

                        if (parameters.Length > 2)
                        {
                            param2 = parameters[2];
                        }
                    }

                    if (eventType.ToLower().Equals("group"))
                    {
                        Fellowship firstPlayerFellowship = session.Player.Fellowship;
                        if (firstPlayerFellowship != null)
                        {
                            if (firstPlayerFellowship.FellowshipMembers.Count() < 3)
                            {
                                CommandHandlerHelper.WriteOutputInfo(session, $"You must have a fellowship with at least 3 members to queue for a group fight");
                                return;
                            }

                            List<string> failureMessages = new List<string>();
                            Guid teamGuid = Guid.NewGuid();
                            int maxOpposingTeamSize = int.TryParse(param2, out int result) ? result : 9;
                            if (maxOpposingTeamSize > 9)
                                maxOpposingTeamSize = 9;
                            if (maxOpposingTeamSize < 3)
                                maxOpposingTeamSize = 3;

                            foreach (var fellowMemberId in firstPlayerFellowship.FellowshipMembers.Keys.OrderBy(x => x == session.Player.CharacterTitleId))
                            {
                                var fellowMemberPlayer = PlayerManager.GetOnlinePlayer(fellowMemberId);
                                if (fellowMemberPlayer == null)
                                    continue;

                                string queueResultMsg = JoinArenaQueue(fellowMemberPlayer, eventType.ToLower(), out bool queueIsSuccess, teamGuid, maxOpposingTeamSize);
                                if (!queueIsSuccess)
                                    failureMessages.Add($"{fellowMemberPlayer.Character.Name}: {queueResultMsg}");
                            }

                            if (failureMessages.Count() > 0)
                            {
                                ArenaManager.RemoveTeamFromQueue(teamGuid);

                                string returnMessage = "Your team failed to queue for the following reasons...\n\n";
                                foreach (var msg in failureMessages)
                                    returnMessage += msg + "\n";

                                CommandHandlerHelper.WriteOutputInfo(session, returnMessage);
                                return;
                            }
                            else
                            {
                                var successMessage = "Your team has successfully queued for a group arena match with the following team members. Please ensure your entire team remains elegible as an online player killer who is not PK tagged.\n\n";
                                var globalMessage = $"{session.Player.Character.Name} has queued a new team of {firstPlayerFellowship.FellowshipMembers.Count()} players for a group arena match, accepting challenging teams with up to {maxOpposingTeamSize} players";
                                foreach (var fellowMemberId in firstPlayerFellowship.FellowshipMembers.Keys)
                                {
                                    var fellowMemberPlayer = PlayerManager.GetOnlinePlayer(fellowMemberId);
                                    if (fellowMemberPlayer == null)
                                        continue;
                                    successMessage += fellowMemberPlayer.Character.Name + "\n";
                                }
                                CommandHandlerHelper.WriteOutputInfo(session, successMessage);
                                PlayerManager.BroadcastToAll(new GameMessageSystemChat(globalMessage, ChatMessageType.Broadcast));
                                return;
                            }
                        }
                        else
                        {
                            CommandHandlerHelper.WriteOutputInfo(session, $"You must have a fellowship with at least 3 members to queue for a group fight");
                            return;
                        }

                        break;
                    }

                    string resultMsg = JoinArenaQueue(session.Player, eventType.ToLower(), out bool isSuccess);
                    if (resultMsg != null)
                    {
                        CommandHandlerHelper.WriteOutputInfo(session, resultMsg);
                        return;
                    }
                    break;

                case "cancel":

                    ArenaManager.PlayerCancel(session.Player.Character.Id);
                    break;

                case "forfeit":
                    CommandHandlerHelper.WriteOutputInfo(session, "Forfeit feature not yet supported, check back later");
                    break;

                case "observe":
                case "watch":

                    if (!PropertyManager.GetBool("arena_allow_observers").Item)
                    {
                        CommandHandlerHelper.WriteOutputInfo(session, $"The arena observer feature is currently disabled");
                        return;
                    }

                    if (parameters.Length != 2)
                    {
                        CommandHandlerHelper.WriteOutputInfo(session, $"Invalid parameters. The {actionType} command requires an EventID parameter to specify which event to join as an observer. Use the \"/arena info\" command to list all active arena events, including their EventID values.\nUsage: To watch an arena event as an observer /arena watch EventID");
                        return;
                    }

                    int eventID = 0;
                    string eventIdParam = parameters[1];
                    try
                    {
                        eventID = int.Parse(eventIdParam);
                    }
                    catch (Exception)
                    {
                        CommandHandlerHelper.WriteOutputInfo(session, $"Invalid parameters. Invalid EventID value {eventIdParam}\nThe {actionType} command requires an EventID parameter to specify which event to join as an observer. Use the \"/arena info\" command to list all active arena events, including their EventID values.\nUsage: To watch an arena event as an observer /arena watch EventID");
                        return;
                    }

                    var arenaEvent = ArenaManager.GetActiveEvents().FirstOrDefault(x => x.Id == eventID);
                    if (arenaEvent != null)
                    {
                        ArenaManager.ObserveEvent(session.Player, eventID);
                    }
                    else
                    {
                        CommandHandlerHelper.WriteOutputInfo(session, $"Invalid parameters. EventID {eventIdParam} does not correspond to an active arena event\nThe {actionType} command requires an EventID parameter to specify which event to join as an observer. Use the \"/arena info\" command to list all active arena events, including their EventID values.\nUsage: To watch an arena event as an observer /arena watch EventID");
                        return;
                    }
                    break;

                case "info":

                    var queuedPlayers = ArenaManager.GetQueuedPlayers();
                    var queuedOnes = queuedPlayers.Where(x => x.EventType.ToLower().Equals("1v1"));
                    var queuedTwos = queuedPlayers.Where(x => x.EventType.ToLower().Equals("2v2"));
                    var queuedFFA = queuedPlayers.Where(x => x.EventType.ToLower().Equals("ffa"));
                    var queuedGroup = queuedPlayers.Where(x => x.EventType.ToLower().Equals("group"));
                    var queuedTugak = queuedPlayers.Where(x => x.EventType.ToLower().Equals("tugak"));
                    var longestOnesWait = queuedOnes.Count() > 0 ? (DateTime.Now - queuedOnes.Min(x => x.CreateDateTime)) : new TimeSpan(0);
                    var longestTwosWait = queuedTwos.Count() > 0 ? (DateTime.Now - queuedTwos.Min(x => x.CreateDateTime)) : new TimeSpan(0);
                    var longestFFAWait = queuedFFA.Count() > 0 ? (DateTime.Now - queuedFFA.Min(x => x.CreateDateTime)) : new TimeSpan(0);
                    var longestTugakWait = queuedTugak.Count() > 0 ? (DateTime.Now - queuedTugak.Min(x => x.CreateDateTime)) : new TimeSpan(0);

                    string queueInfo = $"Current Arena Queues\n  1v1: {queuedOnes.Count()} players queued with longest wait at {string.Format("{0:%h}h {0:%m}m {0:%s}s", longestOnesWait)}\n  2v2: {queuedTwos.Count()} players queued, with longest wait at {string.Format("{0:%h}h {0:%m}m {0:%s}s", longestTwosWait)}\n  FFA: {queuedFFA.Count()} players queued, with longest wait at {string.Format("{0:%h}h {0:%m}m {0:%s}s", longestFFAWait)}\n  Tugak: {queuedTugak.Count()} players queued, with longest wait at {string.Format("{0:%h}h {0:%m}m {0:%s}s", longestTugakWait)}\n  Group:";

                    var queuedGroupTeams = queuedGroup.Select(x => x.TeamGuid).Distinct();
                    foreach (var queuedTeam in queuedGroupTeams)
                    {
                        var teamMembers = queuedGroup.Where(x => x.TeamGuid == queuedTeam);
                        var leader = teamMembers.OrderBy(x => x.CreateDateTime).First();
                        queueInfo += $"\n\n    Team Leader: {leader.CharacterName}\n    Num Players: {teamMembers.Count()}\n    Max Opponents: {leader.MaxOpposingTeamSize}\n    Time Queued: {String.Format("{0:%h}h {0:%m}m {0:%s}s", DateTime.Now - leader.CreateDateTime)}";
                    }

                    var activeEvents = ArenaManager.GetActiveEvents();
                    var eventsOnes = activeEvents.Where(x => x.EventType.ToLower().Equals("1v1"));
                    var eventsTwos = activeEvents.Where(x => x.EventType.ToLower().Equals("2v2"));
                    var eventsFFA = activeEvents.Where(x => x.EventType.ToLower().Equals("ffa"));
                    var eventsGroup = activeEvents.Where(x => x.EventType.ToLower().Equals("group"));
                    var eventsTugak = activeEvents.Where(x => x.EventType.ToLower().Equals("tugak"));

                    string onesEventInfo = eventsOnes.Count() == 0 ? "No active events" : "";
                    foreach (var ev in eventsOnes)
                        onesEventInfo += $"\n    EventID: {(ev.Id < 1 ? "Pending" : ev.Id.ToString())}\n    Arena: {ArenaManager.GetArenaNameByLandblock(ev.Location)}\n    Players:\n    {ev.PlayersDisplay}\n    Time Remaining: {ev.TimeRemainingDisplay}\n";

                    string twosEventInfo = eventsTwos.Count() == 0 ? "No active events" : "";
                    foreach (var ev in eventsTwos)
                        twosEventInfo += $"\n    EventID: {(ev.Id < 1 ? "Pending" : ev.Id.ToString())}\n    Arena: {ArenaManager.GetArenaNameByLandblock(ev.Location)}\n    Players:\n    {ev.PlayersDisplay}\n    Time Remaining: {ev.TimeRemainingDisplay}\n";

                    string ffaEventInfo = eventsFFA.Count() == 0 ? "No active events" : "";
                    foreach (var ev in eventsFFA)
                        ffaEventInfo += $"\n    EventID: {(ev.Id < 1 ? "Pending" : ev.Id.ToString())}\n    Arena: {ArenaManager.GetArenaNameByLandblock(ev.Location)}\n    Players:\n    {ev.PlayersDisplay}\n    Time Remaining: {ev.TimeRemainingDisplay}\n";

                    string tugakEventInfo = eventsTugak.Count() == 0 ? "No active events" : "";
                    foreach (var ev in eventsTugak)
                        tugakEventInfo += $"\n    EventID: {(ev.Id < 1 ? "Pending" : ev.Id.ToString())}\n    Arena: {ArenaManager.GetArenaNameByLandblock(ev.Location)}\n    Players:\n    {ev.PlayersDisplay}\n    Time Remaining: {ev.TimeRemainingDisplay}\n";

                    string groupEventInfo = eventsGroup.Count() == 0 ? "No active events" : "";
                    foreach (var ev in eventsGroup)
                        groupEventInfo += $"\n    EventID: {(ev.Id < 1 ? "Pending" : ev.Id.ToString())}\n    Arena: {ArenaManager.GetArenaNameByLandblock(ev.Location)}\n    Players:\n    {ev.PlayersDisplay}\n    Time Remaining: {ev.TimeRemainingDisplay}\n";

                    string eventInfo = $"Active Arena Matches:\n  1v1: {onesEventInfo}\n  2v2: {twosEventInfo}\n  FFA: {ffaEventInfo}\n  Tugak: {tugakEventInfo}\n  Group: {groupEventInfo}\n";

                    CommandHandlerHelper.WriteOutputInfo(session, $"*********\n{queueInfo}\n\n{eventInfo}\n*********\n");
                    break;

                case "stats":

                    string returnMsg2;
                    if (parameters.Count() >= 2)
                    {
                        string playerParam = "";
                        for (int i = 1; i < parameters.Length; i++)
                            playerParam += i == 1 ? parameters[i] : $" {parameters[i]}";

                        var targetPlayer = PlayerManager.GetAllPlayers().FirstOrDefault(x => x.Name.ToLower().Equals(playerParam.ToLower()));
                        if (targetPlayer != null)
                        {
                            var targetOnlinePlayer = PlayerManager.GetOnlinePlayer(targetPlayer.Guid);
                            var targetOfflinePlayer = PlayerManager.GetOfflinePlayer(targetPlayer.Guid);
                            returnMsg2 = GetArenaStats(targetOnlinePlayer != null ? targetOnlinePlayer.Character.Id : (targetOfflinePlayer != null ? targetOfflinePlayer.Biota.Id : 0), targetPlayer.Name);
                        }
                        else
                        {
                            returnMsg2 = $"Unable to find a player named {playerParam}";
                        }
                    }
                    else
                    {
                        returnMsg2 = GetArenaStats(session.Player.Character.Id, session.Player.Character.Name);
                    }

                    CommandHandlerHelper.WriteOutputInfo(session, returnMsg2);
                    break;

                case "rank":

                    StringBuilder rankReturnMsg = new StringBuilder();
                    string eventTypeParam = "";
                    if (parameters.Count() >= 2)
                        eventTypeParam = parameters[1];

                    bool validParam = eventTypeParam.ToLower().Equals("1v1")      ||
                                      eventTypeParam.ToLower().Equals("2v2")      ||
                                      eventTypeParam.ToLower().Equals("2v2team")  ||
                                      eventTypeParam.ToLower().Equals("ffa")      ||
                                      eventTypeParam.ToLower().Equals("tugak");

                    if (!validParam)
                    {
                        CommandHandlerHelper.WriteOutputInfo(session, "Invalid Event Type Parameter\nUsage: /arena rank {eventType}\nValid types: 1v1, 2v2, 2v2team, ffa, tugak");
                        break;
                    }

                    if (eventTypeParam.ToLower().Equals("2v2team"))
                    {
                        var topTeams = DatabaseManager.Log.GetArenaTopRankedTeams();
                        rankReturnMsg.Append("***** Top Ten 2v2 Teams *****\n\n");
                        for (int i = 0; i < topTeams.Count; i++)
                        {
                            var t = topTeams[i];
                            rankReturnMsg.Append($"  Rank #{i + 1} - {t.TeamName}\n");
                            rankReturnMsg.Append($"  ELO: {t.Elo.ToString("n0")}\n");
                            rankReturnMsg.Append($"  Matches: {t.TotalMatches}  Wins: {t.TotalWins}  Losses: {t.TotalLosses}  Survived: {t.TotalSurvived}\n\n");
                        }
                        rankReturnMsg.Append("**********\n");
                    }
                    else
                    {
                        List<ACE.Database.Models.Log.ArenaCharacterStats> topTen = DatabaseManager.Log.GetArenaTopRankedByEventType(eventTypeParam.ToLower());
                        bool isEloMode = eventTypeParam.ToLower().Equals("1v1") || eventTypeParam.ToLower().Equals("2v2");

                        rankReturnMsg.Append($"***** Top Ten {eventTypeParam} Players *****\n\n");
                        for (int i = 0; i < topTen.Count; i++)
                        {
                            var currStats = topTen[i];
                            rankReturnMsg.Append($"  Rank #{i + 1} - {currStats.CharacterName}\n");
                            if (isEloMode)
                                rankReturnMsg.Append($"  ELO: {currStats.Elo.ToString("n0")}\n");
                            else
                                rankReturnMsg.Append($"  Points: {currStats.RankPoints.ToString("n0")}\n");
                            rankReturnMsg.Append($"  Matches: {currStats.TotalMatches}  Wins: {currStats.TotalWins}  Draws: {currStats.TotalDraws}  Losses: {currStats.TotalLosses}\n\n");
                        }
                        rankReturnMsg.Append("**********\n");
                    }

                    CommandHandlerHelper.WriteOutputInfo(session, rankReturnMsg.ToString());
                    break;

                default:
                    CommandHandlerHelper.WriteOutputInfo(session, $"Arena Commands...\n\n  To join a 1v1 arena match: /arena join\n\n  To join a specific type of arena match: /arena join eventType\n  (replace eventType with the string code for the type of match you want to join; 1v1, 2v2, FFA, Tugak or Group)\n\n  To leave an arena queue or stop observing a match: /arena cancel\n\n  To get info about players in an arena queue and active arena matches: /arena info\n\n  To get your current character's stats: /arena stats\n\n  To get a named character's stats: /arena stats characterName\n  (replace characterName with the target character's name)\n\n  To get rank leaderboard by event type: /arena rank eventType\n  (replace eventType with the string code for the type of match you want ranking for; 1v1, 2v2, 2v2team, Tugak or FFA)\n\n  To watch a match as a silent observer: /arena watch EventID\n  (use /arena info to get the EventID of an active arena match and use that value in the command)\n\n  To get this help file: /arena help\n");
                    return;
            }
        }

        private static string JoinArenaQueue(Player player, string eventType, out bool isSuccess, Guid? teamGuid = null, int maxOpposingTeamSize = 9)
        {
            // Resolve the monarch from the allegiance object (the raw Monarch property can be left
            // stale on a detached character); fall back to the player's own id when genuinely unsworn.
            uint? monarchId = null;
            string monarchName = player.Name;
            var playerAllegiance = AllegianceManager.GetAllegiance(player);
            if (playerAllegiance != null && playerAllegiance.MonarchId.HasValue && playerAllegiance.Members.ContainsKey(player.Guid))
            {
                monarchId = playerAllegiance.MonarchId;
                monarchName = playerAllegiance.Monarch.Player.Name;
            }

            var blacklistString = PropertyManager.GetString("arenas_blacklist").Item;
            if (!string.IsNullOrEmpty(blacklistString))
            {
                var blacklist = blacklistString.Split(',');
                foreach (var charIdString in blacklist)
                {
                    if (uint.TryParse(charIdString, out uint charId) && (player.Character.Id == charId || monarchId == charId))
                    {
                        isSuccess = false;
                        return "You are not permitted to join Arena events.  Please contact an admin if you believe this is in error.";
                    }
                }
            }

            if (player.IsTinker)
            {
                isSuccess = false;
                return "Tinker characters cannot join arena events.";
            }

            var minLevel = PropertyManager.GetLong("arenas_min_level").Item;
            if (player.Level < minLevel)
            {
                isSuccess = false;
                return $"You must be at least level {minLevel} to join an arena match";
            }

            if (player.IsArenaObserver ||
                player.IsPendingArenaObserver ||
                player.CloakStatus == CloakStatus.On)
            {
                isSuccess = false;
                return $"You cannot join an arena queue while you're watching an arena event. Use /arena cancel to stop watching the current event before you queue.";
            }

            if (!player.IsPK)
            {
                isSuccess = false;
                return $"You cannot join an arena queue until you are in a PK state";
            }

            if (player.PKTimerActive)
            {
                isSuccess = false;
                return $"You cannot join an arena queue while you are PK tagged";
            }

            string returnMsg;
            if (!ArenaManager.AddPlayerToQueue(
                player.Character.Id,
                player.Character.Name,
                player.Level,
                eventType,
                monarchId.HasValue ? monarchId.Value : player.Character.Id,
                monarchName,
                player.Session.EndPointC2S?.Address?.ToString(),
                out returnMsg,
                teamGuid,
                maxOpposingTeamSize))
            {
                isSuccess = false;
                return returnMsg;
            }

            isSuccess = true;
            return $"You have successfully joined the {eventType} arena queue";
        }

        private static string GetArenaStats(uint characterId, string characterName)
        {
            return DatabaseManager.Log.GetArenaStatsByCharacterId(characterId, characterName);
        }

        #endregion Arena

        #region Season

        [CommandHandler("season", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Season information, leaderboards, and rewards.",
            "Usage:\n" +
            "  /season status        — Season day, level cap, and your XP budgets\n" +
            "  /season top           — #1 leader in each category\n" +
            "  /season top <cat>     — Top 10 for a specific category\n" +
            "  /season stats         — Your rank in every leaderboard category\n" +
            "  /season stats <name>  — Another player's leaderboard standings\n" +
            "  /season rewards       — Collect unclaimed weekly milestone reward items\n" +
            "  /season info          — Category list and descriptions\n" +
            "  /season help          — Full help and category aliases")]
        public static void HandleSeason(Session session, params string[] parameters)
        {
            if (session?.Player == null) return;
            if (!CheckPlayerCommandRateLimit(session)) return;

            var sub = parameters.Length > 0 ? parameters[0].ToLower() : "status";

            switch (sub)
            {
                case "status":
                    HandleSeasonStatus(session);
                    break;

                case "":
                case "info":
                    HandleSeasonInfo(session);
                    break;

                case "top":
                case "leaderboard":
                    if (!CheckSeasonCommandRateLimit(session)) return;
                    var topCat = parameters.Length > 1
                        ? SeasonConfig.ResolveAlias(parameters[1])
                        : null;
                    HandleSeasonTop(session, topCat);
                    break;

                case "stats":
                    if (!CheckSeasonCommandRateLimit(session)) return;
                    var statsName = parameters.Length > 1 ? parameters[1] : null;
                    HandleSeasonStats(session, statsName);
                    break;

                case "claim":
                case "rewards":
                    HandleSeasonClaim(session);
                    break;

                case "help":
                    HandleSeasonHelp(session);
                    break;

                default:
                    // Allow /season <alias> as shorthand for /season top <alias>
                    var aliasCheck = SeasonConfig.ResolveAlias(sub);
                    if (aliasCheck != null)
                    {
                        if (!CheckSeasonCommandRateLimit(session)) return;
                        HandleSeasonTop(session, aliasCheck);
                    }
                    else
                    {
                        CommandHandlerHelper.WriteOutputInfo(session,
                            $"Unknown sub-command \"{sub}\". Type /season help for usage.");
                    }
                    break;
            }
        }

        private static void HandleSeasonStatus(Session session)
        {
            if (!PropertyManager.GetBool("rolling_level_cap_enabled").Item ||
                PropertyManager.GetLong("rolling_level_cap_start_timestamp").Item <= 0)
            {
                CommandHandlerHelper.WriteOutputInfo(session, "The season has not started yet.");
                return;
            }

            var  player   = session.Player;
            long xpCap    = RollingLevelCapManager.GetCurrentXpCap();
            int  day      = RollingLevelCapManager.GetCurrentSeasonDay();
            int  levelCap = RollingLevelCapManager.GetDisplayLevelCap(xpCap);

            var sb = new StringBuilder();
            sb.AppendLine("------- Season Status -------");
            // Display is 1-based: opening day is "Day 1" even though GetCurrentSeasonDay is 0-based.
            sb.AppendLine($"  Day:         {day + 1}");

            if (levelCap > 126)
                sb.AppendLine($"  Level Cap:   {levelCap}  (XP-equivalent; in-game level caps at 126)");
            else
                sb.AppendLine($"  Level Cap:   {levelCap}");

            sb.AppendLine($"  XP Cap:      {xpCap:N0}");

            // Progress of the player's lifetime total XP toward the current season cap.
            long totalXp = player.TotalExperience ?? 0;
            if (xpCap > 0)
            {
                double capPct    = Math.Min(100.0, totalXp * 100.0 / xpCap);
                string capStatus = capPct >= 100.0 ? " [AT CAP]" : $" ({capPct:F1}%)";
                sb.AppendLine($"  To Cap:      {totalXp:N0} / {xpCap:N0}{capStatus}");
            }

            // Time until next cap advance — rounded to nearest minute
            var timeUntil = RollingLevelCapManager.GetTimeUntilNextCapIncrease();
            if (timeUntil == TimeSpan.Zero)
                sb.AppendLine($"  Next Advance: season cap is frozen");
            else
            {
                int totalMinutes = (int)Math.Round(timeUntil.TotalMinutes);
                sb.AppendLine($"  Next Advance: {totalMinutes / 60}h {totalMinutes % 60}m");
            }

            // XP rate modifier
            var xpModifier = PropertyManager.GetDouble("xp_modifier").Item;
            sb.AppendLine($"  XP Rate:     {xpModifier:F2}x");

            // Catch-up boost — extra XP for characters sitting below the cap threshold.
            if (PropertyManager.GetBool("catchup_xp_enabled").Item && xpCap > 0)
            {
                var catchUp   = RollingLevelCapManager.GetCatchUpXpMultiplier(totalXp);
                var threshold = Math.Min(1.0, PropertyManager.GetDouble("catchup_xp_threshold").Item);

                if (catchUp > 1.0)
                    sb.AppendLine($"  Catch-Up:    {catchUp:F2}x  (all XP you earn, while below {threshold * 100:F0}% of cap)");
                else
                    sb.AppendLine($"  Catch-Up:    none  (only below {threshold * 100:F0}% of cap)");
            }

            // Per-category XP budgets.
            // If the rolling cap has advanced since the player's last XP award the lazy
            // reset inside UpdateXpAndLevel hasn't fired yet — the stored bucket values
            // are stale.  Detect this and project what the budgets will look like after
            // the reset so players don't see a falsely-exhausted display.
            double monsterRatio = PropertyManager.GetDouble("daily_monster_xp_category_ratio").Item;
            double questRatio   = PropertyManager.GetDouble("daily_quest_xp_category_ratio").Item;
            double pvpRatio     = PropertyManager.GetDouble("daily_pvp_xp_category_ratio").Item;

            bool pendingReset = xpCap > 0 && player.CapPreviousXpCap != xpCap;

            long monsterUsed, questUsed, pvpUsed;
            long monsterBudget, questBudget, pvpBudget;

            if (pendingReset)
            {
                // Buckets will be zeroed and new budgets computed on the next XP award.
                monsterUsed = questUsed = pvpUsed = 0;
                long xpRemainingAtReset = Math.Max(0L, xpCap - (player.TotalExperience ?? 0));
                monsterBudget = xpRemainingAtReset > 0 ? (long)(xpRemainingAtReset * monsterRatio) : (long)(xpCap * monsterRatio);
                questBudget   = xpRemainingAtReset > 0 ? (long)(xpRemainingAtReset * questRatio)   : (long)(xpCap * questRatio);
                pvpBudget     = xpRemainingAtReset > 0 ? (long)(xpRemainingAtReset * pvpRatio)     : (long)(xpCap * pvpRatio);
            }
            else
            {
                monsterUsed   = player.CapMonsterXp;
                questUsed     = player.CapQuestXp;
                pvpUsed       = player.CapPvpXp;
                monsterBudget = player.CapDailyMaxMonsterCat > 0 ? player.CapDailyMaxMonsterCat : (long)(xpCap * monsterRatio);
                questBudget   = player.CapDailyMaxQuestCat   > 0 ? player.CapDailyMaxQuestCat   : (long)(xpCap * questRatio);
                pvpBudget     = player.CapDailyMaxPvpCat     > 0 ? player.CapDailyMaxPvpCat     : (long)(xpCap * pvpRatio);
            }

            sb.AppendLine();
            if (pendingReset)
                sb.AppendLine("  XP Budgets (pending reset — will apply on next XP award):");
            else
                sb.AppendLine("  XP Budgets (reset each time cap advances):");
            sb.AppendLine(FormatSeasonBudgetLine("  Monster", monsterUsed, monsterBudget));
            sb.AppendLine(FormatSeasonBudgetLine("  Quest  ", questUsed,   questBudget));
            sb.AppendLine(FormatSeasonBudgetLine("  PK     ", pvpUsed,     pvpBudget));
            sb.Append("-----------------------------");

            CommandHandlerHelper.WriteOutputInfo(session, sb.ToString());
        }

        private static string FormatSeasonBudgetLine(string label, long earned, long budget)
        {
            if (budget <= 0)
                return $"{label}:  --";
            double pct    = Math.Min(100.0, earned * 100.0 / budget);
            string status = pct >= 100.0 ? " [FULL]" : $" ({pct:F1}%)";
            return $"{label}:  {earned:N0} / {budget:N0}{status}";
        }

        #endregion Season

        // ====================================================================
        #region Season Leaderboard

        private static readonly Dictionary<uint, DateTime> _seasonCommandTimestamps = new();
        private const int SeasonCommandCooldownSeconds = 60;

        private static bool CheckSeasonCommandRateLimit(Session session)
        {
            if (session == null) return false;
            var charId = session.Player.Guid.Full;
            var now    = DateTime.UtcNow;
            if (_seasonCommandTimestamps.TryGetValue(charId, out var last)
                && (now - last).TotalSeconds < SeasonCommandCooldownSeconds)
            {
                CommandHandlerHelper.WriteOutputInfo(session,
                    $"You can only use this command every {SeasonCommandCooldownSeconds} seconds. Please wait a moment.");
                return false;
            }
            _seasonCommandTimestamps[charId] = now;
            return true;
        }

        private static void HandleSeasonInfo(Session session)
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════");
            sb.AppendLine("         SEASON LEADERBOARDS       ");
            sb.AppendLine("═══════════════════════════════════");
            sb.AppendLine("Categories:");
            sb.AppendLine("  Arena:  1v1 Arena, 2v2 Arena, FFA Arena, Tugak Arena, Group Arena");
            sb.AppendLine("          Arena Wins, Arena Matches");
            sb.AppendLine("  World:  PK Kills, K/D Ratio (min 10 kills), Kill Streak, Bounty Hunter");
            sb.AppendLine("  Overall: Season Champion  (weighted rank-points across all categories)");
            sb.AppendLine();
            sb.AppendLine("Commands:");
            sb.AppendLine("  /season top [category]  — View top 10 for a category");
            sb.AppendLine("  /season stats [name]    — View your (or another player's) standings");
            sb.AppendLine("  /season rewards         — Collect unclaimed weekly milestone rewards");
            sb.AppendLine("  /season help            — Category aliases and full usage");
            sb.AppendLine();
            sb.AppendLine("Weekly Milestone: Every Sunday the top 10 in each category earn rewards.");
            sb.AppendLine("Use /season rewards to collect reward items from past milestones.");
            CommandHandlerHelper.WriteOutputInfo(session, sb.ToString());
        }

        private static void HandleSeasonTop(Session session, string category)
        {
            if (category == null)
            {
                // No category — print 1 leader per category as a summary
                var sb = new StringBuilder();
                sb.AppendLine("════════ Season Leaders ════════");
                foreach (var cat in SeasonConfig.ScoredCategories)
                {
                    var top = SeasonManager.GetTopForCategory(cat, 1);
                    var name  = top.Count > 0 ? top[0].CharacterName : "(none)";
                    var score = top.Count > 0 ? top[0].ScoreDisplay   : "-";
                    sb.AppendLine($"  {SeasonConfig.GetCategoryDisplayName(cat),-18} {name,-20} {score}");
                }

                // Overall
                var overallTop = SeasonManager.GetTopForCategory(SeasonConfig.Cat_Overall, 1);
                var oName  = overallTop.Count > 0 ? overallTop[0].CharacterName : "(none)";
                var oScore = overallTop.Count > 0 ? overallTop[0].ScoreDisplay   : "-";
                sb.AppendLine($"  {"Season Champion",-18} {oName,-20} {oScore}");

                sb.AppendLine();
                sb.AppendLine("Use /season top <category> for the full top 10.  /season help for aliases.");
                CommandHandlerHelper.WriteOutputInfo(session, sb.ToString());
                return;
            }

            var entries = SeasonManager.GetTopForCategory(category, 10);
            var displayName = SeasonConfig.GetCategoryDisplayName(category);

            var sb2 = new StringBuilder();
            sb2.AppendLine($"════ Top 10: {displayName} ════");

            if (entries.Count == 0)
            {
                sb2.AppendLine("  No entries yet.");
            }
            else
            {
                foreach (var e in entries)
                {
                    var rankLabel = e.Rank < 10 ? $" {e.Rank}." : $"{e.Rank}.";
                    sb2.AppendLine($"  {rankLabel} {e.CharacterName,-22} {e.ScoreDisplay}");
                }
            }

            CommandHandlerHelper.WriteOutputInfo(session, sb2.ToString());
        }

        private static void HandleSeasonStats(Session session, string targetName)
        {
            uint   charId;
            string charName;

            if (string.IsNullOrWhiteSpace(targetName))
            {
                charId   = session.Player.Guid.Full;
                charName = session.Player.Name;
            }
            else
            {
                var found = PlayerManager.FindByName(targetName);
                if (found == null)
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"Player \"{targetName}\" not found.");
                    return;
                }
                charId   = found.Guid.Full;
                charName = found.Name;
            }

            var standing = SeasonManager.GetPlayerStanding(charId, charName);

            var sb = new StringBuilder();
            sb.AppendLine($"════ Season Standings: {charName} ════");

            foreach (var cat in SeasonConfig.ScoredCategories)
            {
                if (!standing.CategoryStandings.TryGetValue(cat, out var entry))
                    continue;

                var rankStr  = entry.Rank > 0 ? $"Rank {entry.Rank,4}" : "   Unranked";
                var scoreStr = entry.ScoreDisplay ?? "0";
                sb.AppendLine($"  {SeasonConfig.GetCategoryDisplayName(cat),-18} {rankStr}  {scoreStr}");
            }

            if (standing.CategoryStandings.TryGetValue(SeasonConfig.Cat_Overall, out var overall))
            {
                var oRank = overall.Rank > 0 ? $"Rank {overall.Rank,4}" : "   Unranked";
                sb.AppendLine($"  {"Season Champion",-18} {oRank}  {overall.ScoreDisplay}");
            }

            CommandHandlerHelper.WriteOutputInfo(session, sb.ToString());
        }

        private static void HandleSeasonClaim(Session session)
        {
            SeasonManager.ClaimRewards(session.Player);
        }

        #region Allegiance Hometown

        [CommandHandler("ah", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Recalls to a random allegiance-owned hometown. Use /ahtown <town name> to recall to a specific town.")]
        public static void HandleAllegianceHometownRecall(Session session, params string[] parameters)
        {
            var player = session.Player;

            if (player.IsOlthoiPlayer)
            {
                player.SendTransientError("Olthoi cannot use this command.");
                return;
            }

            if (player.PKTimerActive && ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
            {
                session.Network.EnqueueSend(new GameEventWeenieError(session, WeenieError.YouHaveBeenInPKBattleTooRecently));
                return;
            }

            if (player.RecallsDisabled)
            {
                session.Network.EnqueueSend(new GameEventWeenieError(session, WeenieError.ExitTrainingAcademyToUseCommand));
                return;
            }

            if (player.TooBusyToRecall)
            {
                session.Network.EnqueueSend(new GameEventWeenieError(session, WeenieError.YoureTooBusy));
                return;
            }

            if (player.Allegiance == null)
            {
                session.Network.EnqueueSend(new GameEventWeenieError(session, WeenieError.YouAreNotInAllegiance));
                return;
            }

            var monarchId = AllegianceManager.GetVerifiedMonarchId(player) ?? player.Guid.Full;
            var ownedTownIds = AllegianceHometownManager.GetOwnedTownIds(monarchId);

            if (ownedTownIds.Count == 0)
            {
                player.SendTransientError("Your allegiance does not own any hometowns. Use a bind stone to claim one.");
                return;
            }

            // Random owned town
            var ownedList = ownedTownIds.ToList();
            var idx = ThreadSafeRandom.Next(0, ownedList.Count - 1);
            var entry = ACE.Server.Entity.AllegianceHometown.AllegianceHometownRegistry.GetById(ownedList[idx]);
            if (entry == null)
            {
                player.SendTransientError("Failed to find hometown data. Please try again.");
                return;
            }
            ACE.Entity.Position destination = entry.BindstonePosition;

            // Perform recall animation then teleport
            if (player.CombatMode != CombatMode.NonCombat)
            {
                var updateCombatMode = new GameMessagePrivateUpdatePropertyInt(player, ACE.Entity.Enum.Properties.PropertyInt.CombatMode, (int)CombatMode.NonCombat);
                player.SetCombatMode(CombatMode.NonCombat);
                session.Network.EnqueueSend(updateCombatMode);
            }

            player.EnqueueBroadcast(new GameMessageSystemChat($"{player.Name} is recalling to an allegiance hometown.", ChatMessageType.Recall), WorldObjects.WorldObject.LocalBroadcastRange, ChatMessageType.Recall);
            player.SendMotionAsCommands(MotionCommand.AllegianceHometownRecall, MotionStance.NonCombat);

            var startPos = new ACE.Entity.Position(player.Location);
            player.IsBusy = true;

            var animLength = DatLoader.DatManager.PortalDat.ReadFromDat<DatLoader.FileTypes.MotionTable>(player.MotionTableId)
                .GetAnimationLength(MotionCommand.AllegianceHometownRecall);

            var chain = new ActionChain();
            chain.AddDelaySeconds(animLength);
            chain.AddAction(player, () =>
            {
                player.IsBusy = false;

                if (startPos.SquaredDistanceTo(player.Location) > WorldObjects.Player.RecallMoveThresholdSq)
                {
                    session.Network.EnqueueSend(new GameEventWeenieError(session, WeenieError.YouHaveMovedTooFar));
                    return;
                }

                if (player.Allegiance == null)
                {
                    session.Network.EnqueueSend(new GameEventWeenieError(session, WeenieError.YouAreNotInAllegiance));
                    return;
                }

                player.Teleport(destination);
            });
            chain.EnqueueChain();
        }

        [CommandHandler("ahtown", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 1,
            "Recalls to a specific allegiance-owned hometown by name.",
            "<town name>")]
        public static void HandleAllegianceHometownRecallNamed(Session session, params string[] parameters)
        {
            var player = session.Player;

            if (player.IsOlthoiPlayer)
            {
                player.SendTransientError("Olthoi cannot use this command.");
                return;
            }

            if (player.PKTimerActive && ConfigManager.Config.Server.WorldRuleset != Common.Ruleset.CustomDM)
            {
                session.Network.EnqueueSend(new GameEventWeenieError(session, WeenieError.YouHaveBeenInPKBattleTooRecently));
                return;
            }

            if (player.RecallsDisabled)
            {
                session.Network.EnqueueSend(new GameEventWeenieError(session, WeenieError.ExitTrainingAcademyToUseCommand));
                return;
            }

            if (player.TooBusyToRecall)
            {
                session.Network.EnqueueSend(new GameEventWeenieError(session, WeenieError.YoureTooBusy));
                return;
            }

            if (player.Allegiance == null)
            {
                session.Network.EnqueueSend(new GameEventWeenieError(session, WeenieError.YouAreNotInAllegiance));
                return;
            }

            var monarchId    = AllegianceManager.GetVerifiedMonarchId(player) ?? player.Guid.Full;
            var ownedTownIds = AllegianceHometownManager.GetOwnedTownIds(monarchId);

            if (ownedTownIds.Count == 0)
            {
                player.SendTransientError("Your allegiance does not own any hometowns. Use a bind stone to claim one.");
                return;
            }

            var townArg = string.Join(" ", parameters).Trim();
            var match = ACE.Server.Entity.AllegianceHometown.AllegianceHometownRegistry.All.Values
                .FirstOrDefault(t => ownedTownIds.Contains(t.TownId) &&
                    t.TownName.StartsWith(townArg, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                player.SendTransientError($"Your allegiance does not own a hometown matching '{townArg}'. Use /towns to see owned towns.");
                return;
            }

            var destination = match.BindstonePosition;

            if (player.CombatMode != CombatMode.NonCombat)
            {
                var updateCombatMode = new GameMessagePrivateUpdatePropertyInt(player, ACE.Entity.Enum.Properties.PropertyInt.CombatMode, (int)CombatMode.NonCombat);
                player.SetCombatMode(CombatMode.NonCombat);
                session.Network.EnqueueSend(updateCombatMode);
            }

            player.EnqueueBroadcast(new GameMessageSystemChat($"{player.Name} is recalling to an allegiance hometown.", ChatMessageType.Recall), WorldObjects.WorldObject.LocalBroadcastRange, ChatMessageType.Recall);
            player.SendMotionAsCommands(MotionCommand.AllegianceHometownRecall, MotionStance.NonCombat);

            var startPos = new ACE.Entity.Position(player.Location);
            player.IsBusy = true;

            var animLength = DatLoader.DatManager.PortalDat.ReadFromDat<DatLoader.FileTypes.MotionTable>(player.MotionTableId)
                .GetAnimationLength(MotionCommand.AllegianceHometownRecall);

            var chain = new ActionChain();
            chain.AddDelaySeconds(animLength);
            chain.AddAction(player, () =>
            {
                player.IsBusy = false;

                if (startPos.SquaredDistanceTo(player.Location) > WorldObjects.Player.RecallMoveThresholdSq)
                {
                    session.Network.EnqueueSend(new GameEventWeenieError(session, WeenieError.YouHaveMovedTooFar));
                    return;
                }

                if (player.Allegiance == null)
                {
                    session.Network.EnqueueSend(new GameEventWeenieError(session, WeenieError.YouAreNotInAllegiance));
                    return;
                }

                player.Teleport(destination);
            });
            chain.EnqueueChain();
        }

        [CommandHandler("towns", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Lists all capturable hometowns and their current ownership status.")]
        public static void HandleTowns(Session session, params string[] parameters)
        {
            var player = session.Player;
            var monarchId = AllegianceManager.GetVerifiedMonarchId(player) ?? player.Guid.Full;

            var sb = new StringBuilder();
            sb.AppendLine("════════ Allegiance Hometowns ════════");

            foreach (var entry in ACE.Server.Entity.AllegianceHometown.AllegianceHometownRegistry.All.Values
                .OrderBy(t => t.TownName))
            {
                var town = AllegianceHometownManager.GetTown(entry.TownId);
                if (town == null) continue;

                string ownerPart;
                if (!town.OwnerMonarchId.HasValue)
                    ownerPart = "Unowned";
                else if (town.OwnerMonarchId == monarchId)
                    ownerPart = $"Owned by YOUR allegiance ({town.OwnerAllegianceName})";
                else
                    ownerPart = $"Owned by {town.OwnerAllegianceName}";

                string conflictPart = "";
                if (town.ConflictPhase == 1)
                    conflictPart = " [Phase 1 Conflict]";
                else if (town.ConflictPhase == 2)
                    conflictPart = " [PHASE 2 — Bind Stone Under Attack!]";
                else if (AllegianceHometownManager.IsTownProtected(entry.TownId))
                    conflictPart = " [Protected]";

                sb.AppendLine($"  {entry.TownName}: {ownerPart}{conflictPart}");
            }

            sb.AppendLine();
            sb.AppendLine($"Your allegiance owns {AllegianceHometownManager.GetOwnedTownCount(monarchId)} town(s). Use /ah to recall to a random town or /ahtown <name> for a specific one.");
            CommandHandlerHelper.WriteOutputInfo(session, sb.ToString());
        }

        #endregion Allegiance Hometown

        private static void HandleSeasonHelp(Session session)
        {
            var sb = new StringBuilder();
            sb.AppendLine("════════ /season Help ════════");
            sb.AppendLine("Sub-commands:");
            sb.AppendLine("  /season              — Season overview & your best category rank");
            sb.AppendLine("  /season info         — Category descriptions");
            sb.AppendLine("  /season top          — #1 leader summary for all categories");
            sb.AppendLine("  /season top <cat>    — Full top 10 for a specific category");
            sb.AppendLine("  /season stats        — Your standings across all categories");
            sb.AppendLine("  /season stats <name> — Another player's standings");
            sb.AppendLine("  /season rewards      — Collect any unclaimed weekly milestone rewards");
            sb.AppendLine();
            sb.AppendLine("Category aliases for /season top <cat>:");
            sb.AppendLine("  1v1,  2v2,  ffa,  tugak,  group");
            sb.AppendLine("  arena-wins | wins,  arena-matches | matches | veteran");
            sb.AppendLine("  bounty | bountyhunter,  pk-kills | reaper | kills");
            sb.AppendLine("  pk-kd | kd | ratio | precision,  pk-streak | streak | unstoppable");
            sb.AppendLine("  overall | champion");
            sb.AppendLine();
            sb.AppendLine("Weekly Milestone: Every Sunday the top 10 in each category are");
            sb.AppendLine("snapshotted. Use /season rewards to collect reward items.");
            CommandHandlerHelper.WriteOutputInfo(session, sb.ToString());
        }

        #endregion Season Leaderboard
    }
}
