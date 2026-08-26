using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;

using log4net;

using ACE.Database;

namespace ACE.Server.Managers
{
    public static class PropertyManager
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // caching internally to the server
        private static readonly ConcurrentDictionary<string, ConfigurationEntry<bool>> CachedBooleanSettings = new ConcurrentDictionary<string, ConfigurationEntry<bool>>();
        private static readonly ConcurrentDictionary<string, ConfigurationEntry<long>> CachedLongSettings = new ConcurrentDictionary<string, ConfigurationEntry<long>>();
        private static readonly ConcurrentDictionary<string, ConfigurationEntry<double>> CachedDoubleSettings = new ConcurrentDictionary<string, ConfigurationEntry<double>>();
        private static readonly ConcurrentDictionary<string, ConfigurationEntry<string>> CachedStringSettings = new ConcurrentDictionary<string, ConfigurationEntry<string>>();

        private static Timer _workerThread;

        /// <summary>
        /// Initializes the PropertyManager.
        /// Run this only once per server instance.
        /// </summary>
        /// <param name="loadDefaultValues">Should we use the DefaultPropertyManager to load the default properties for keys?</param>
        public static void Initialize(bool loadDefaultValues = true)
        {
            if (loadDefaultValues)
                DefaultPropertyManager.LoadDefaultProperties();

            LoadPropertiesFromDB();

            if (Program.IsRunningInContainer && !GetString("content_folder").Equals("/ace/Content"))
                ModifyString("content_folder", "/ace/Content");

            _workerThread = new Timer(300000);
            _workerThread.Elapsed += DoWork;
            _workerThread.AutoReset = true;
            _workerThread.Start();
        }


        /// <summary>
        /// Loads the variables from the database directly into the cache.
        /// </summary>
        private static void LoadPropertiesFromDB()
        {
            foreach (var i in DatabaseManager.ShardConfig.GetAllBools())
                CachedBooleanSettings[i.Key] = new ConfigurationEntry<bool>(false, i.Value, i.Description);

            foreach (var i in DatabaseManager.ShardConfig.GetAllLongs())
                CachedLongSettings[i.Key] = new ConfigurationEntry<long>(false, i.Value, i.Description);

            foreach (var i in DatabaseManager.ShardConfig.GetAllDoubles())
                CachedDoubleSettings[i.Key] = new ConfigurationEntry<double>(false, i.Value, i.Description);

            foreach (var i in DatabaseManager.ShardConfig.GetAllStrings())
                CachedStringSettings[i.Key] = new ConfigurationEntry<string>(false, i.Value, i.Description);
        }

        /// <summary>
        /// Resyncs the variables with the database manually.
        /// Disables the timer so that the elapsed event cannot run during the update operation.
        /// </summary>
        public static void ResyncVariables()
        {
            _workerThread.Stop();

            DoWork(null, null);

            _workerThread.Start();
        }

        /// <summary>
        /// Stops updating the cached store from the database.
        /// </summary>
        public static void StopUpdating()
        {
            if (_workerThread != null)
                _workerThread.Stop();
        }


        /// <summary>
        /// Retrieves a boolean property from the cache or database
        /// </summary>
        /// <param name="key">The string key for the property</param>
        /// <param name="fallback">The value to return if the property cannot be found.</param>
        /// <param name="cacheFallback">Whether or not the fallback property should be cached.</param>
        /// <returns>A boolean value representing the property</returns>
        public static Property<bool> GetBool(string key, bool fallback = false, bool cacheFallback = true)
        {
            // first, check the cache. If the key exists in the cache, grab it regardless of its modified value
            // then, check the database. if the key exists in the database, grab it and cache it
            // finally, set it to a default of false.
            if (CachedBooleanSettings.ContainsKey(key))
                return new Property<bool>(CachedBooleanSettings[key].Item, CachedBooleanSettings[key].Description);

            var dbValue = DatabaseManager.ShardConfig.GetBool(key);

            bool useFallback = dbValue?.Value == null;

            var value = dbValue?.Value ?? fallback;

            if (!useFallback || cacheFallback)
                CachedBooleanSettings[key] = new ConfigurationEntry<bool>(useFallback, value, dbValue?.Description);

            return new Property<bool>(value, dbValue?.Description);
        }

        /// <summary>
        /// Modifies a boolean value in the cache and marks it for being synced on the next cycle.
        /// </summary>
        /// <param name="key">The string key for the property</param>
        /// <param name="newVal">The value to replace the old value with</param>
        /// <returns>true if the property was modified, false if no property exists with the given key</returns>
        public static bool ModifyBool(string key, bool newVal)
        {
            if (!DefaultPropertyManager.DefaultBooleanProperties.ContainsKey(key))
                return false;

            if (CachedBooleanSettings.ContainsKey(key))
                CachedBooleanSettings[key].Modify(newVal);
            else
                CachedBooleanSettings[key] = new ConfigurationEntry<bool>(true, newVal, DefaultPropertyManager.DefaultBooleanProperties[key].Description);

            return true;
        }

        public static void ModifyBoolDescription(string key, string description)
        {
            if (CachedBooleanSettings.ContainsKey(key))
                CachedBooleanSettings[key].ModifyDescription(description);
            else
                log.Warn($"Attempted to modify {key} which did not exist in the BOOL cache.");
        }

        /// <summary>
        /// Retreives an integer property from the cache or database
        /// </summary>
        /// <param name="key">The string key for the property</param>
        /// <param name="fallback">The value to return if the property cannot be found.</param>
        /// <param name="cacheFallback">Whether or not the fallback property should be cached</param>
        /// <returns>An integer value representing the property</returns>
        public static Property<long> GetLong(string key, long fallback = 0, bool cacheFallback = true)
        {
            if (CachedLongSettings.ContainsKey(key))
                return new Property<long>(CachedLongSettings[key].Item, CachedLongSettings[key].Description);

            var dbValue = DatabaseManager.ShardConfig.GetLong(key);

            bool useFallback = dbValue?.Value == null;

            var value = dbValue?.Value ?? fallback;

            if (!useFallback || cacheFallback)
                CachedLongSettings[key] = new ConfigurationEntry<long>(useFallback, value, dbValue?.Description);

            return new Property<long>(value, dbValue?.Description);
        }

        /// <summary>
        /// Modifies an integer value in the cache and marks it for being synced on the next cycle.
        /// </summary>
        /// <param name="key">The string key for the property</param>
        /// <param name="newVal">The value to replace the old value with</param>
        /// <returns>true if the property was modified, false if no property exists with the given key</returns>
        public static bool ModifyLong(string key, long newVal)
        {
            if (!DefaultPropertyManager.DefaultLongProperties.ContainsKey(key))
                return false;

            if (CachedLongSettings.ContainsKey(key))
                CachedLongSettings[key].Modify(newVal);
            else
                CachedLongSettings[key] = new ConfigurationEntry<long>(true, newVal, DefaultPropertyManager.DefaultLongProperties[key].Description);
            return true;
        }

        public static void ModifyLongDescription(string key, string description)
        {
            if (CachedLongSettings.ContainsKey(key))
                CachedLongSettings[key].ModifyDescription(description);
            else
                log.Warn($"Attempted to modify {key} which did not exist in the LONG cache.");
        }

        /// <summary>
        /// Retrieves a float property from the cache or database
        /// </summary>
        /// <param name="key">The string key for the property</param>
        /// <param name="fallback">The value to return if the property cannot be found.</param>
        /// <param name="cacheFallback">Whether or not the fallpack property should be cached</param>
        /// <returns>A float value representing the property</returns>
        public static Property<double> GetDouble(string key, double fallback = 0.0f, bool cacheFallback = true)
        {
            if (CachedDoubleSettings.ContainsKey(key))
                return new Property<double>(CachedDoubleSettings[key].Item, CachedDoubleSettings[key].Description);

            var dbValue = DatabaseManager.ShardConfig.GetDouble(key);

            bool useFallback = dbValue?.Value == null;

            var value = dbValue?.Value ?? fallback;

            if (!useFallback || cacheFallback)
                CachedDoubleSettings[key] = new ConfigurationEntry<double>(useFallback, value, dbValue?.Description);

            return new Property<double>(value, dbValue?.Description);
        }

        /// <summary>
        /// Modifies a float value in the cache and marks it for being synced on the next cycle.
        /// </summary>
        /// <param name="key">The string key for the property</param>
        /// <param name="newVal">The value to replace the old value with</param>
        public static bool ModifyDouble(string key, double newVal, bool init = false)
        {
            if (!DefaultPropertyManager.DefaultDoubleProperties.ContainsKey(key))
                return false;
            if (CachedDoubleSettings.ContainsKey(key))
                CachedDoubleSettings[key].Modify(newVal);
            else
                CachedDoubleSettings[key] = new ConfigurationEntry<double>(true, newVal, DefaultPropertyManager.DefaultDoubleProperties[key].Description);

            if (!init)
            {
                switch (key)
                {
                    case "cantrip_drop_rate":
                        Factories.Tables.CantripChance.ApplyNumCantripsMod();
                        break;
                    case "minor_cantrip_drop_rate":
                    case "major_cantrip_drop_rate":
                    case "epic_cantrip_drop_rate":
                    case "legendary_cantrip_drop_rate":
                        Factories.Tables.CantripChance.ApplyCantripLevelsMod();
                        break;
                }
            }
            return true;
        }

        public static void ModifyDoubleDescription(string key, string description)
        {
            if (CachedDoubleSettings.ContainsKey(key))
                CachedDoubleSettings[key].ModifyDescription(description);
            else
                log.Warn($"Attempted to modify the description of {key} which did not exist in the DOUBLE cache.");
        }

        /// <summary>
        /// Retreives a string property from the cache or database
        /// </summary>
        /// <param name="key">The string key for the property</param>
        /// <param name="fallback">The value to return if the property cannot be found.</param>
        /// <param name="cacheFallback">Whether or not the fallback value will be cached.</param>
        /// <returns>A string value representing the property</returns>
        public static Property<string> GetString(string key, string fallback = "", bool cacheFallback = true)
        {
            if (CachedStringSettings.ContainsKey(key))
                return new Property<string>(CachedStringSettings[key].Item, CachedStringSettings[key].Description);

            var dbValue = DatabaseManager.ShardConfig.GetString(key);

            bool useFallback = dbValue?.Value == null;

            var value = dbValue?.Value ?? fallback;

            if (!useFallback || cacheFallback)
                CachedStringSettings[key] = new ConfigurationEntry<string>(useFallback, value, dbValue?.Description);

            return new Property<string>(value, dbValue?.Description);
        }

        /// <summary>
        /// Modifies a string value in the cache and marks it for being synced on the next cycle
        /// </summary>
        /// <param name="key">The string key for the property</param>
        /// <param name="newVal">The value to replace the old value with</param>
        /// <returns>true if the property was modified, false if no property exists with the given key</returns>
        public static bool ModifyString(string key, string newVal)
        {
            if (!DefaultPropertyManager.DefaultStringProperties.ContainsKey(key))
                return false;

            if (CachedStringSettings.ContainsKey(key))
                CachedStringSettings[key].Modify(newVal);
            else
                CachedStringSettings[key] = new ConfigurationEntry<string>(true, newVal, DefaultPropertyManager.DefaultStringProperties[key].Description);
            return true;
        }

        public static void ModifyStringDescription(string key, string description)
        {
            if (CachedStringSettings.ContainsKey(key))
                CachedStringSettings[key].ModifyDescription(description);
            else
                log.Warn($"Attempted to modify {key} which did not exist in the STRING cache.");
        }


        /// <summary>
        /// Writes all of the updated boolean values from the cache into the database.
        /// </summary>
        private static void WriteBoolToDB()
        {
            foreach (var i in CachedBooleanSettings.Where(r => r.Value.Modified))
            {
                // this probably should be upsert. This does 2 queries per modified datapoint.
                // perhaps run a transaction to queue all the queries at once.
                if (DatabaseManager.ShardConfig.BoolExists(i.Key))
                    DatabaseManager.ShardConfig.SaveBool(new Database.Models.Shard.ConfigPropertiesBoolean { Key = i.Key, Value = i.Value.Item, Description = i.Value.Description });
                else
                    DatabaseManager.ShardConfig.AddBool(i.Key, i.Value.Item, i.Value.Description);
            }
        }

        /// <summary>
        /// Writes all of the updated integer values from the cache into the database.
        /// </summary>
        private static void WriteLongToDB()
        {
            foreach (var i in CachedLongSettings.Where(r => r.Value.Modified))
            {
                // todo: see boolean section for caveat in this approach
                if (DatabaseManager.ShardConfig.LongExists(i.Key))
                    DatabaseManager.ShardConfig.SaveLong(new Database.Models.Shard.ConfigPropertiesLong { Key = i.Key, Value = i.Value.Item, Description = i.Value.Description });
                else
                    DatabaseManager.ShardConfig.AddLong(i.Key, i.Value.Item, i.Value.Description);
            }
        }

        /// <summary>
        /// Writes all of the updated float values from the cache into the database.
        /// </summary>
        private static void WriteDoubleToDB()
        {
            foreach (var i in CachedDoubleSettings.Where(r => r.Value.Modified))
            {
                // todo: see boolean section for caveat in this approach
                if (DatabaseManager.ShardConfig.DoubleExists(i.Key))
                    DatabaseManager.ShardConfig.SaveDouble(new Database.Models.Shard.ConfigPropertiesDouble { Key = i.Key, Value = i.Value.Item, Description = i.Value.Description });
                else
                    DatabaseManager.ShardConfig.AddDouble(i.Key, i.Value.Item, i.Value.Description);
            }
        }

        /// <summary>
        /// Writes all of the updated string values from the cache into the database.
        /// </summary>
        private static void WriteStringToDB()
        {
            foreach (var i in CachedStringSettings.Where(r => r.Value.Modified))
            {
                // todo: see boolean section for caveat in this approach
                if (DatabaseManager.ShardConfig.StringExists(i.Key))
                    DatabaseManager.ShardConfig.SaveString(new Database.Models.Shard.ConfigPropertiesString { Key = i.Key, Value = i.Value.Item, Description = i.Value.Description });
                else
                    DatabaseManager.ShardConfig.AddString(i.Key, i.Value.Item, i.Value.Description);
            }
        }

        private static void DoWork(Object source, ElapsedEventArgs e)
        {
            var startTime = DateTime.UtcNow;

            // first, check for variables updated on the server-side. Write those to the DB.
            // then, compare variables to DB and update from DB as necessary. (needs to minimize r/w)

            WriteBoolToDB();
            WriteLongToDB();
            WriteDoubleToDB();
            WriteStringToDB();

            // next, we need to fetch all of the variables from the DB and compare them quickly.
            LoadPropertiesFromDB();

            log.DebugFormat("PropertyManager DoWork took {0:N0} ms", (DateTime.UtcNow - startTime).TotalMilliseconds);
        }
        public static string ListProperties()
        {
            string props = "Boolean properties:\n";
            foreach (var item in DefaultPropertyManager.DefaultBooleanProperties)
                props += string.Format("\t{0}: {1} (current is {2}, default is {3})\n", item.Key, item.Value.Description, GetBool(item.Key).Item, item.Value.Item);

            props += "\nLong properties:\n";
            foreach (var item in DefaultPropertyManager.DefaultLongProperties)
                props += string.Format("\t{0}: {1} (current is {2}, default is {3})\n", item.Key, item.Value.Description, GetLong(item.Key).Item, item.Value.Item);

            props += "\nDouble properties:\n";
            foreach (var item in DefaultPropertyManager.DefaultDoubleProperties)
                props += string.Format("\t{0}: {1} (current is {2}, default is {3})\n", item.Key, item.Value.Description, GetDouble(item.Key).Item, item.Value.Item);

            props += "\nString properties:\n";
            foreach (var item in DefaultPropertyManager.DefaultStringProperties)
                props += string.Format("\t{0}: {1} (default is hidden)\n", item.Key, item.Value.Description);

            return props;
        }
    }

    public struct Property<T>
    {
        public Property(T item, string description) : this()
        {
            Item = item;
            Description = description;
        }

        public T Item { get; }
        public string Description { get; }
    }

    class ConfigurationEntry<T>
    {
        public bool Modified;
        public T Item;
        public string Description;

        public ConfigurationEntry(bool modified, T item)
        {
            Modified = modified;
            Item = item;
        }

        public ConfigurationEntry(bool modified, T item, string description)
        {
            Modified = modified;
            Item = item;
            Description = description;
        }

        public void Modify(T item)
        {
            Item = item;
            Modified = true;
        }

        public void ModifyDescription(string description)
        {
            Description = description;
            Modified = true;
        }

        public override string ToString()
        {
            return Item + " " + Modified;
        }
    }

    public static class DefaultPropertyManager
    {
        private static ReadOnlyDictionary<A,V> DictOf<A, V>()
        {
            return new ReadOnlyDictionary<A, V>(new Dictionary<A, V>());
        }

        private static ReadOnlyDictionary<A, V> DictOf<A, V>(params (A, V)[] pairs)
        {
            return new ReadOnlyDictionary<A, V>(pairs.ToDictionary
            (
                tup => tup.Item1,
                tup => tup.Item2
            ));
        }

        public static void LoadDefaultProperties()
        {
            // Place any default properties to load in here

            //bool
            foreach (var item in DefaultBooleanProperties)
                PropertyManager.ModifyBool(item.Key, item.Value.Item);

            //float
            foreach (var item in DefaultDoubleProperties)
                PropertyManager.ModifyDouble(item.Key, item.Value.Item, true);

            //int
            foreach (var item in DefaultLongProperties)
                PropertyManager.ModifyLong(item.Key, item.Value.Item);

            //string
            foreach (var item in DefaultStringProperties)
                PropertyManager.ModifyString(item.Key, item.Value.Item);

            // Alternative ruleset's default overrides
            if (Common.ConfigManager.Config.Server.WorldRuleset == Common.Ruleset.Infiltration)
            {
                PropertyManager.ModifyBool("corpse_destroy_pyreals", false);
                PropertyManager.ModifyBool("item_dispel", true);
                PropertyManager.ModifyBool("vendor_shop_uses_generator", true);
                PropertyManager.ModifyBool("allow_xp_at_max_level", true);
                PropertyManager.ModifyBool("allow_fast_chug", false); // Having this on causes the drinking potion animation to get stuck mid-drink quite often.

                PropertyManager.ModifyLong("max_level", 126);

                PropertyManager.ModifyBool("show_dat_warning", true);
                PropertyManager.ModifyString("dat_older_warning_msg", "The location you are attempting to enter is not present in your data files.");
            }
            else if(Common.ConfigManager.Config.Server.WorldRuleset == Common.Ruleset.CustomDM)
            {
                PropertyManager.ModifyBool("corpse_destroy_pyreals", false);
                PropertyManager.ModifyBool("item_dispel", true);
                PropertyManager.ModifyBool("vendor_shop_uses_generator", true);
                PropertyManager.ModifyBool("increase_minimum_encounter_spawn_density", true);
                PropertyManager.ModifyBool("show_dot_messages", true);
                PropertyManager.ModifyBool("salvage_handle_overages", true);
                PropertyManager.ModifyBool("allow_fast_chug", false);
                PropertyManager.ModifyBool("allow_jump_loot", false);
                PropertyManager.ModifyBool("allow_pkl_bump", false);
                PropertyManager.ModifyBool("fix_chest_missing_inventory_window", true);
                PropertyManager.ModifyBool("runrate_add_hooks", true);
                PropertyManager.ModifyBool("house_15day_account", false);
                PropertyManager.ModifyBool("house_30day_cooldown", false);
                PropertyManager.ModifyBool("house_per_char", true);

                PropertyManager.ModifyLong("mansion_min_rank", 0);
                PropertyManager.ModifyLong("house_min_level", 0);
                PropertyManager.ModifyLong("fellowship_even_share_level", 80);

                PropertyManager.ModifyBool("show_dat_warning", true);
                PropertyManager.ModifyString("dat_older_warning_msg", "The location you are attempting to enter is not present in your data files.");

                PropertyManager.ModifyDouble("vendor_unique_rot_time", 1800);
                PropertyManager.ModifyDouble("quest_mindelta_rate", 0.2412);

                PropertyManager.ModifyBool("pathfinding", true);
            }
        }

        // ==================================================================================
        // To change these values for the server,
        // please use the /modifybool, /modifylong, /modifydouble, and /modifystring commands
        // ==================================================================================

        public static readonly ReadOnlyDictionary<string, Property<bool>> DefaultBooleanProperties =
            DictOf(
                ("account_login_boots_in_use", new Property<bool>(true, "if FALSE, oldest connection to account is not booted when new connection occurs")),
                ("enforce_account_ip_binding", new Property<bool>(true, "If true, each IP address may only be associated with one account. Accounts are auto-banned on a second IP change within one calendar month.")),
                ("advanced_combat_pets", new Property<bool>(false, "(non-retail function) If enabled, Combat Pets can cast spells")),
                ("advocate_fane_auto_bestow", new Property<bool>(false, "If enabled, Advocate Fane will automatically bestow new advocates to advocate_fane_auto_bestow_level")),
                ("aetheria_heal_color", new Property<bool>(false, "If enabled, changes the aetheria healing over time messages from the default retail red color to green")),
                ("disable_allegiance_whitelist", new Property<bool>(false, "If TRUE, the allegiance whitelist is bypassed and every player is treated as whitelisted (IsAllegianceWhitelisted always returns true), disabling all whitelist-gated features.")),
                ("allow_combat_mode_crafting", new Property<bool>(false, "If enabled, allows players to do crafting (recipes) from all stances. Forces players to NonCombat first, then continues to recipe action.")),
                ("allow_door_hold", new Property<bool>(true, "enables retail behavior where standing on a door while it is closing keeps the door as ethereal until it is free from collisions, effectively holding the door open for other players")),
                ("allow_fast_chug", new Property<bool>(true, "enables retail behavior where a player can consume food and drink faster than normal by breaking animation")),
                ("allow_jump_loot", new Property<bool>(true, "enables retail behavior where a player can quickly loot items while jumping, bypassing the 'crouch down' animation")),
                ("allow_negative_dispel_resist", new Property<bool>(true, "enables retail behavior where #-# negative dispels can be resisted")),
                ("allow_negative_rating_curve", new Property<bool>(true, "enables retail behavior where negative DRR from void dots didn't switch to the reverse rating formula, resulting in a possibly unintended curve that quickly ramps up as -rating goes down, eventually approaching infinity / divide by 0 for -100 rating. less than -100 rating would produce negative numbers.")),
                ("allow_pkl_bump", new Property<bool>(true, "enables retail behavior where /pkl checks for entry collisions, bumping the player position over if standing on another PKLite. This effectively enables /pkl door skipping from retail")),
                ("allow_summoning_killtask_multicredit", new Property<bool>(true, "enables retail behavior where a summoner can get multiple killtask credits from a monster")),
                ("allow_swear_to_lower_level", new Property<bool>(true, "enables retail behavior where a player may swear allegiance to a lower-level patron. No allegiance XP passes up until the patron surpasses the vassal's level (handled automatically via ExistedBeforeAllegianceXpChanges). If FALSE, swearing to a lower-level character is blocked.")),
                ("assess_creature_mod", new Property<bool>(false, "(non-retail function) If enabled, re-enables former skill formula, when assess creature skill is not trained or spec'ed")),
                ("attribute_augmentation_safety_cap", new Property<bool>(true, "if TRUE players are not able to use attribute augmentations if the innate value of the target attribute is >= 96. All normal restrictions to these augmentations still apply.")),
                ("chat_disable_general", new Property<bool>(false, "disable general global chat channel")),
                ("chat_disable_lfg", new Property<bool>(false, "disable lfg global chat channel")),
                ("chat_disable_olthoi", new Property<bool>(false, "disable olthoi global chat channel")),
                ("chat_disable_roleplay", new Property<bool>(false, "disable roleplay global chat channel")),
                ("chat_disable_trade", new Property<bool>(false, "disable trade global chat channel")),
                ("chat_echo_only", new Property<bool>(false, "global chat returns to sender only")),
                ("chat_echo_reject", new Property<bool>(false, "global chat returns to sender on reject")),
                ("chat_inform_reject", new Property<bool>(true, "global chat informs sender on reason for reject")),
                ("chat_log_abuse", new Property<bool>(false, "log abuse chat")),
                ("chat_log_admin", new Property<bool>(false, "log admin chat")),
                ("chat_log_advocate", new Property<bool>(false, "log advocate chat")),
                ("chat_log_allegiance", new Property<bool>(false, "log allegiance chat")),
                ("chat_log_audit", new Property<bool>(true, "log audit chat")),
                ("chat_log_debug", new Property<bool>(false, "log debug chat")),
                ("chat_log_fellow", new Property<bool>(false, "log fellow chat")),
                ("chat_log_general", new Property<bool>(false, "log general chat")),
                ("chat_log_global", new Property<bool>(false, "log global broadcasts")),
                ("chat_log_help", new Property<bool>(false, "log help chat")),
                ("chat_log_lfg", new Property<bool>(false, "log LFG chat")),
                ("chat_log_olthoi", new Property<bool>(false, "log olthoi chat")),
                ("chat_log_qa", new Property<bool>(false, "log QA chat")),
                ("chat_log_roleplay", new Property<bool>(false, "log roleplay chat")),
                ("chat_log_sentinel", new Property<bool>(false, "log sentinel chat")),
                ("chat_log_society", new Property<bool>(false, "log society chat")),
                ("chat_log_trade", new Property<bool>(false, "log trade chat")),
                ("chat_log_townchans", new Property<bool>(false, "log advocate town chat")),
                ("chat_requires_account_15days", new Property<bool>(false, "global chat privileges requires accounts to be 15 days or older")),
                ("chess_enabled", new Property<bool>(true, "if FALSE then chess will be disabled")),
                ("use_cloak_proc_custom_scale", new Property<bool>(false, "If TRUE, the calculation for cloak procs will be based upon the values set by the server oeprator.")),
                ("client_movement_formula", new Property<bool>(false, "If enabled, server uses DoMotion/StopMotion self-client movement methods instead of apply_raw_movement")),
                ("container_opener_name", new Property<bool>(false, "If enabled, when a player tries to open a container that is already in use by someone else, replaces 'someone else' in the message with the actual name of the player")),
                ("corpse_decay_tick_logging", new Property<bool>(false, "If ENABLED then player corpse ticks will be logged")),
                ("corpse_destroy_pyreals", new Property<bool>(true, "If FALSE then pyreals will not be completely destroyed on player death")),
                ("craft_exact_msg", new Property<bool>(false, "If TRUE, and player has crafting chance of success dialog enabled, shows them an additional message in their chat window with exact %")),
                ("creature_name_check", new Property<bool>(true, "if enabled, creature names in world database restricts player names during character creation")),
                ("creatures_drop_createlist_wield", new Property<bool>(false, "If FALSE then Wielded items in CreateList will not drop. Retail defaulted to TRUE but there are currently data errors")),
                ("equipmentsetid_enabled", new Property<bool>(true, "enable this to allow adding EquipmentSetIDs to loot armor")),
                ("equipmentsetid_name_decoration", new Property<bool>(false, "enable this to add the EquipmentSet name to loot armor name")),
                ("fastbuff", new Property<bool>(true, "If TRUE, enables the fast buffing trick from retail.")),
                ("fellow_busy_no_recruit", new Property<bool>(true, "if FALSE, fellows can be recruited while they are busy, different from retail")),
                ("fellow_kt_killer", new Property<bool>(true, "if FALSE, fellowship kill tasks will share with the fellowship, even if the killer doesn't have the quest")),
                ("fellow_kt_landblock", new Property<bool>(false, "if TRUE, fellowship kill tasks will share with landblock range (192 distance radius, or entire dungeon)")),
                ("fellow_quest_bonus", new Property<bool>(false, "if TRUE, applies EvenShare formula to fellowship quest reward XP (300% max bonus, defaults to false in retail)")),
                ("fix_chest_missing_inventory_window", new Property<bool>(false, "Very non-standard fix. This fixes an acclient bug where unlocking a chest, and then quickly opening it before the client has received the Locked=false update from server can result in the chest opening, but with the chest inventory window not displaying. Bug has a higher chance of appearing with more network latency.")),
                ("gateway_ties_summonable", new Property<bool>(true, "if disabled, players cannot summon ties from gateways. defaults to enabled, as in retail")),
                ("gearknight_core_plating", new Property<bool>(true, "if disabled, Gear Knight players are not required to use core plating devices for armor and clothing. defaults to enabled, as in retail")),
                ("house_15day_account", new Property<bool>(true, "if disabled, houses can be purchased with accounts created less than 15 days old")),
                ("house_30day_cooldown", new Property<bool>(true, "if disabled, houses can be purchased without waiting 30 days between each purchase")),
                ("house_hook_limit", new Property<bool>(true, "if disabled, house hook limits are ignored")),
                ("house_hookgroup_limit", new Property<bool>(true, "if disabled, house hook group limits are ignored")),
                ("house_per_char", new Property<bool>(false, "if TRUE, allows 1 house per char instead of 1 house per account")),
                ("house_purchase_requirements", new Property<bool>(true, "if disabled, requirements to purchase/rent house are not checked")),
                ("house_rent_enabled", new Property<bool>(true, "If FALSE then rent is not required")),
                ("iou_trades", new Property<bool>(false, "(non-retail function) If enabled, IOUs can be traded for objects that are missing in DB but added/restored later on")),
                ("item_dispel", new Property<bool>(false, "if enabled, allows players to dispel items. defaults to end of retail, where item dispels could only target creatures")),
                ("lifestone_broadcast_death", new Property<bool>(true, "if true, player deaths are additionally broadcast to other players standing near the destination lifestone")),
                ("local_server", new Property<bool>(false, "if true, bypasses the same-IP check for PK quest credit (useful for local/test servers where all players connect from the same IP)")),
                ("loot_quality_mod", new Property<bool>(true, "if FALSE then the loot quality modifier of a Death Treasure profile does not affect loot generation")),
                ("npc_hairstyle_fullrange", new Property<bool>(false, "if TRUE, allows generated creatures to use full range of hairstyles. Retail only allowed first nine (0-8) out of 51")),
                ("offline_xp_passup_limit", new Property<bool>(true, "if FALSE, allows unlimited xp to passup to offline characters in allegiances")),
                ("olthoi_play_disabled", new Property<bool>(false, "if false, allows players to create and play as olthoi characters")),
                ("override_encounter_spawn_rates", new Property<bool>(false, "if enabled, landblock encounter spawns are overidden by double properties below.")),
                ("permit_corpse_all", new Property<bool>(false, "If TRUE, /permit grants permittees access to all corpses of the permitter. Defaults to FALSE as per retail, where /permit only grants access to 1 locked corpse")),
                ("persist_movement", new Property<bool>(false, "If TRUE, persists autonomous movements such as turns and sidesteps through non-autonomous server actions. Retail didn't appear to do this, but some players may prefer this.")),
                ("pet_stow_replace", new Property<bool>(false, "pet stowing for different pet devices becomes a stow and replace. defaults to retail value of false")),
                ("player_config_command", new Property<bool>(false, "If enabled, players can use /config to change their settings via text commands")),
                ("player_receive_immediate_save", new Property<bool>(false, "if enabled, when the player receives items from an NPC, they will be saved immediately")),
                ("pk_server", new Property<bool>(false, "set this to TRUE for darktide servers")),
                ("pk_server_safe_training_academy", new Property<bool>(false, "set this to TRUE to disable pk fighting in training academy and time to exit starter town safely")),
                ("pkl_server", new Property<bool>(false, "set this to TRUE for pink servers")),
                ("quest_info_enabled", new Property<bool>(false, "toggles the /myquests player command")),
                ("rares_real_time", new Property<bool>(true, "allow for second chance roll based on an rng seeded timestamp for a rare on rare eligible kills that do not generate a rare, rares_max_seconds_between defines maximum seconds before second chance kicks in")),
                ("rares_real_time_v2", new Property<bool>(false, "chances for a rare to be generated on rare eligible kills are modified by the last time one was found per each player, rares_max_days_between defines maximum days before guaranteed rare generation")),
                ("runrate_add_hooks", new Property<bool>(false, "if TRUE, adds some runrate hooks that were missing from retail (exhaustion done, raise skill/attribute")),
                ("reportbug_enabled", new Property<bool>(false, "toggles the /reportbug player command")),
                ("require_spell_comps", new Property<bool>(true, "if FALSE, spell components are no longer required to be in inventory to cast spells. defaults to enabled, as in retail")),
                ("safe_spell_comps", new Property<bool>(false, "if TRUE, disables spell component burning for everyone")),
                ("salvage_handle_overages", new Property<bool>(false, "in retail, if 2 salvage bags were combined beyond 100 structure, the overages would be lost")),
                ("show_ammo_buff", new Property<bool>(false, "shows active enchantments such as blood drinker on equipped missile ammo during appraisal")),
                ("show_aura_buff", new Property<bool>(false, "shows active aura enchantments on wielded items during appraisal")),
                ("show_dat_warning", new Property<bool>(false, "if TRUE, will alert player (dat_warning_msg) when client attempts to download from server and boot them from game, disabled by default")),
                ("show_dot_messages", new Property<bool>(false, "enabled, shows combat messages for DoT damage ticks. defaults to disabled, as in retail")),
                ("show_first_login_gift", new Property<bool>(false, "if TRUE, will show on first login that the player earned bonus item (Blackmoor's Favor and/or Asheron's Benediction), disabled by default because msg is kind of odd on an emulator")),
                ("show_mana_conv_bonus_0", new Property<bool>(true, "if disabled, only shows mana conversion bonus if not zero, during appraisal of casting items")),
                ("smite_uses_takedamage", new Property<bool>(false, "if enabled, smite applies damage via TakeDamage")),

                // --- missile tracking experiments (each independently toggleable for A/B testing) ---
                ("missile_fresh_solution", new Property<bool>(false, "MISSILE FIX 1: if enabled, the projectile firing solution (velocity / spawn origin / orientation) is recalculated at the instant the projectile spawns, instead of before the turn + aim animation. removes 0.03-0.57s of staleness for bows, a flat 0.378s for thrown weapons, plus rotate time on repeat attacks")),
                ("missile_lead_fallback", new Property<bool>(false, "MISSILE FIX 2: if enabled, when the quartic intercept solver finds no solution against a moving target, fall back to the lateral solver (the one spell projectiles use) instead of silently dropping to a zero-lead stationary aim. affects thrown weapons past ~21-30m and bows past ~51-61m vs a fleeing target")),
                ("missile_lead_fallback_log", new Property<bool>(false, "MISSILE FIX 2 diagnostics: log every time the quartic intercept solver fails to find a solution, and which fallback was used. noisy - for testing only")),
                ("missile_aim_center_mass", new Property<bool>(false, "MISSILE FIX 4: if enabled, missile attacks against PLAYER targets aim at the center of the target's collision spheres rather than at the top of the head / the gap between spheres. raises lateral hit tolerance from 0.318m to 0.580m for high attacks, 0.386m to 0.540m for medium. does not affect monster targets, whose collision setups vary")),
                ("spellcast_recoil_queue", new Property<bool>(false, "if true, players can queue the next spell to cast during recoil animation")),
                ("spell_projectile_ethereal", new Property<bool>(false, "broadcasts all spell projectiles as ethereal to clients only, and manually send stop velocity on collision. can fix various issues with client missing target id.")),
                ("suicide_instant_death", new Property<bool>(false, "if enabled, @die command kills player instantly. defaults to disabled, as in retail")),
                ("taboo_table", new Property<bool>(true, "if enabled, taboo table restricts player names during character creation")),
                ("tailoring_intermediate_uieffects", new Property<bool>(false, "If true, tailoring intermediate icons retain the magical/elemental highlight of the original item")),
                ("trajectory_alt_solver", new Property<bool>(false, "use the alternate trajectory solver for missiles and spell projectiles")),
                ("universal_masteries", new Property<bool>(true, "if TRUE, matches end of retail masteries - players wielding almost any weapon get +5 DR, except if the weapon \"seems tough to master\". " +
                                                                 "if FALSE, players start with mastery of 1 melee and 1 ranged weapon type based on heritage, and can later re-select these 2 masteries")),
                ("unlimited_sequence_gaps", new Property<bool>(false, "upon startup, allows server to find all unused guids in a range instead of a set hard limit")),
                ("use_generator_rotation_offset", new Property<bool>(true, "enables or disables using the generator's current rotation when offseting relative positions")),
                ("use_portal_max_level_requirement", new Property<bool>(true, "disable this to ignore the max level restriction on portals")),
                ("use_turbine_chat", new Property<bool>(true, "enables or disables global chat channels (General, LFG, Roleplay, Trade, Olthoi, Society, Allegience)")),
                ("use_wield_requirements", new Property<bool>(true, "disable this to bypass wield requirements. mostly for dev debugging")),
                ("version_info_enabled", new Property<bool>(false, "toggles the /aceversion player command")),
                ("vendor_shop_uses_generator", new Property<bool>(false, "enables or disables vendors using generator system in addition to createlist to create artificial scarcity")),
                ("world_closed", new Property<bool>(false, "enable this to startup world as a closed to players world")),
                ("allow_xp_at_max_level", new Property<bool>(false, "enable this to allow players to continue earning xp after reaching max level")),
                ("block_vpn_connections", new Property<bool>(false, "enable this to block user sessions from IPs identified as VPN proxies")),
                ("increase_minimum_encounter_spawn_density", new Property<bool>(false, "enable this to increase the density of random encounters that spawn in low density landblocks")),
                ("command_who_enabled", new Property<bool>(true, "disable this to prevent players from listing online players in their allegiance")),
                ("enforce_player_movement", new Property<bool>(false, "enable this to enforce server side verification of player movement")),
                ("enforce_player_movement_speed", new Property<bool>(false, "enable this to enforce server side verification of player movement speed")),
                ("enforce_player_movement_kick", new Property<bool>(false, "enable this to kick players that fail movement verification too frenquently")),
                ("movement_violation_kick", new Property<bool>(false, "enable this to kick players when their movement suspicion score reaches the kick threshold (counter >= 10); score >= 50 always kicks regardless")),
                ("enforce_player_movement_avg", new Property<bool>(false, "enable sliding window average speed checks using both a 3s window and a 15s window; violations feed the suspicion scoring system")),
                ("enforce_player_movement_raycast", new Property<bool>(false, "enable geometry collision detection; flags position updates where the physics engine could not reach the requested position without passing through solid geometry")),
                ("enforce_player_jump_height", new Property<bool>(false, "enable jump height cap; flags jumps where the player reached a higher apex than their Strength/Jump skill allows")),
                ("enforce_player_door_collision", new Property<bool>(false, "Option N: flag players whose physics transition passed through a closed door; violations feed the suspicion scoring system")),
                ("enforce_player_spawn_collision", new Property<bool>(false, "Option K: flag players whose physics transition passed through a creature that spawned within the last 5 seconds (spawn ghost window)")),
                ("enforce_player_timing_regularity", new Property<bool>(false, "script detection: flag players whose inter-packet movement timing has a coefficient of variation below 0.04 over 12+ samples in 3+ seconds; human hands always jitter, scripts do not")),
                ("enforce_player_packet_rate", new Property<bool>(false, "script detection: flag players who send movement packets faster than movement_packet_rate_limit per second over a 2-second rolling window")),
                ("enforce_player_reversal_detection", new Property<bool>(false, "script detection: flag players who sustain several consecutive ~180 degree heading reversals (each pair of steps within 66 ms and within 10 degrees of 180); brief glitch-running produces the occasional flip, so only a sustained run scores. Score-only, no rubber-band")),
                ("movement_debug_chat", new Property<bool>(false, "anti-cheat diagnosis: when true, sends per-packet speed values and collision-grace accept/deny details to the violating player's chat window so you can observe which check fires and at what values; WARNING — disable immediately after diagnosis")),
                ("allow_PKs_to_go_NPK", new Property<bool>(true, "Allows PKs to go back to being NPKs by using the appropriate altar")),
                ("show_discord_chat_ingame", new Property<bool>(false, "Display messages posted to Discord in general chat")),
                ("allow_custom_gameplay_modes", new Property<bool>(true, "CustomDM: Allow creation of new characters using gameplay modes such as hardcore and solo self-found")),
                ("hardcore_death_keep_bonded", new Property<bool>(false, "Allow hardcore characters to keep their bonded equipment on death")),
                ("hardcore_death_keep_spells", new Property<bool>(false, "Allow hardcore characters to keep their spells on death")),
                ("hardcore_death_keep_housing", new Property<bool>(false, "Allow hardcore characters to keep their housing on death.")),
                ("hardcore_death_keep_allegiance", new Property<bool>(false, "Allow hardcore characters to keep their allegiance on death")),
                ("hardcore_pk_grant_ring_of_impermanency", new Property<bool>(false, "Give new hardcore PKs a ring that buffs their run and jump skills that they can wear up to level 20")),
                ("pathfinding", new Property<bool>(false, "CustomDM: Allows creatures to use pathfinding to navigate dungeons")),
                ("cmd_pop_show_current", new Property<bool>(true, "Allow the pop command to show current online population count")),
                ("cmd_pop_show_24_hours", new Property<bool>(true, "Allow the pop command to show the 24 hours unique IPs count")),
                ("cmd_pop_show_7_days", new Property<bool>(true, "Allow the pop command to show the 7 days unique IPs count")),
                ("bz_whispers_enabled", new Property<bool>(true, "CustomDM: Enables/Disables whispers from Bael'Zharon revealing the location of other PK players")),
                ("force_logout_materialization", new Property<bool>(true, "forces players to materialize before logging out if they are teleporting")),
                ("force_teleport_materialization", new Property<bool>(true, "forces players to remain in portal space for a duration after teleporting")),
                ("force_login_materialization", new Property<bool>(true, "forces players to remain in portal space for a duration after logging in")),
                ("recent_teleport_prevention", new Property<bool>(true, "prevents players from teleporting again immediately after materializing")),
                ("disable_arenas", new Property<bool>(false, "set to true to disable arena events")),
                ("disable_pvp_cleave", new Property<bool>(true, "disables melee cleave attacks from targeting players")),
                ("arena_allow_same_ip_match", new Property<bool>(false, "enable to allow two characters from the same IP to be matched in an arena event")),
                ("arena_allow_observers", new Property<bool>(true, "enable to allow players to watch arena matches as invisible observers")),
                ("tinker_lotto_enabled", new Property<bool>(false, "enables the tinkering lotto feature")),
                ("rolling_level_cap_enabled", new Property<bool>(false, "Enables the server-wide rolling level cap. When enabled, players cannot exceed the XP threshold for the current cap level. The cap starts at 15 and increases daily based on rolling_level_cap_start_timestamp.")),
                ("rolling_xp_modifier_enabled", new Property<bool>(false, "When true, RollingLevelCapManager automatically adjusts xp_modifier each day using a quadratic curve tied to season progression. Starts at 0.25 on day 0, reaches 1.0 at ~36% through the season (day ~44, level cap ~101), and climbs to rolling_xp_modifier_max at 80% (day 96). Requires rolling_level_cap_enabled.")),
                ("catchup_xp_enabled", new Property<bool>(true, "Enables the catch-up XP boost. Characters whose lifetime total XP is below catchup_xp_threshold of the current season XP cap earn boosted XP, scaled by how far behind the cap they are. Requires an active rolling level cap.")),
                ("hot_dungeon_enabled", new Property<bool>(false, "Enables the Hot Dungeons system on Infiltration servers. When enabled, up to 3 dungeons are periodically selected to offer bonus XP and loot.")),
                ("hot_dungeon_logout_timer", new Property<bool>(true, "When enabled, any player standing in an active Hot Dungeon is subject to a PK-style pending-logout timer (frozen in-game until hot_dungeon_logout_timer_seconds elapses). Recalls are not affected.")),
                ("dungeon_boss_enabled", new Property<bool>(false, "Enables random Dungeon Bosses. When enabled, normal monster spawns in an active Hot Dungeon or the Abandoned Mine have a small chance to be replaced by a scaled boss. Requires the Infiltration ruleset.")),
                ("turnto_use_heading_stealth", new Property<bool>(false, "If true, TurnTo motions between two PK players use an absolute heading instead of a target ID, to prevent War Detect style plugins from revealing the target.")),

                // Bounty Hunter system
                ("bounty_system_enabled",            new Property<bool>(true,  "Enable or disable the bounty hunter system entirely.")),
                ("writ_of_pursuit_enabled",          new Property<bool>(true,  "Enable or disable Writs of Pursuit (high-priority bounty targets).")),
                ("bounty_allow_all_locations",       new Property<bool>(true,  "If true, bounty targets are valid at any location (no landblock restriction). Recommended for ClassicPvP.")),
                ("bounty_allow_logged_out",          new Property<bool>(false, "If true, players who are logged out can still be bounty targets.")),
                ("bounty_pk_timer_active_enabled",   new Property<bool>(true,  "If true, the PK timer is extended when a hunter is near their bounty target.")),
                ("bounty_expirations_enabled",       new Property<bool>(true,  "If true, bounty contracts expire after bounty_expiration_time minutes."))
                );

        public static readonly ReadOnlyDictionary<string, Property<long>> DefaultLongProperties =
            DictOf(
                ("char_delete_time", new Property<long>(3600, "the amount of time in seconds a deleted character can be restored")),
                ("chat_requires_account_time_seconds", new Property<long>(0, "the amount of time in seconds an account is required to have existed for for global chat privileges")),
                ("chat_requires_player_age", new Property<long>(0, "the amount of time in seconds a player is required to have played for global chat privileges")),
                ("chat_requires_player_level", new Property<long>(0, "the level a player is required to have for global chat privileges")),
                ("corpse_spam_limit", new Property<long>(15, "the number of corpses a player is allowed to leave on a landblock at one time")),
                ("default_subscription_level", new Property<long>(1, "retail defaults to 1, 1 = standard subscription (same as 2 and 3), 4 grants ToD pre-order bonus item Asheron's Benediction")),
                ("fellowship_even_share_level", new Property<long>(50, "level when fellowship XP sharing is no longer restricted")),
                ("house_min_level", new Property<long>(-1, "overrides the default character level required to purchase a house. -1 uses the slumlord's value, 0 disables the level restriction entirely")),
                ("mansion_min_rank", new Property<long>(6, "overrides the default allegiance rank required to own a mansion")),
                ("max_chars_per_account", new Property<long>(11, "retail defaults to 11, client supports up to 20")),
                ("pk_timer", new Property<long>(20, "the number of seconds where a player cannot perform certain actions (ie. teleporting) after becoming involved in a PK battle")),
                ("hot_dungeon_logout_timer_seconds", new Property<long>(20, "the number of seconds a player is held in a frozen pending-logout state when logging out while inside an active Hot Dungeon (see hot_dungeon_logout_timer)")),
                ("player_save_interval", new Property<long>(300, "the number of seconds between automatic player saves")),
                ("rares_max_days_between", new Property<long>(45, "for rares_real_time_v2: the maximum number of days a player can go before a rare is generated on rare eligible creature kills")),
                ("rares_max_seconds_between", new Property<long>(5256000, "for rares_real_time: the maximum number of seconds a player can go before a second chance at a rare is allowed on rare eligible creature kills that did not generate a rare")),
                ("summoning_killtask_multicredit_cap", new Property<long>(2, "if allow_summoning_killtask_multicredit is enabled, the maximum # of killtask credits a player can receive from 1 kill")),
                ("teleport_visibility_fix", new Property<long>(0, "Fixes some possible issues with invisible players and mobs. 0 = default / disabled, 1 = players only, 2 = creatures, 3 = all world objects")),
                ("pvp_dispel_vuln_timer", new Property<long>(300, "the number of seconds where a player's dispel actions will not remove vulns after becoming involved in a PK battle")),
                ("jump_limit", new Property<long>(7, "the number of jumps you can do before being penalized")),
                ("jump_second_timer", new Property<long>(10, "the number of seconds cutoff for jumping")),
                ("jump_penalty_length", new Property<long>(5, "the number of seconds you're penalized after hitting the jump limits")),
                ("max_level", new Property<long>(275, "Set the max character level.")),
                ("discord_channel_id", new Property<long>(0, "Messages posted to this Discord channel will be shown in General Chat")),
                ("quest_mindelta_rate_shortest", new Property<long>(72000, "Quest min deltas below this won't be affected by quest_mindelta_rate, additionally modified min deltas that would fall under this value will be set to this value instead")),
                ("bz_whispers_min_pop", new Property<long>(5, "CustomDM: Minimum required online PK players for bz whispers to be sent")),
                ("bz_whispers_login_delay", new Property<long>(3600, "CustomDM: How long a player must remain online before being able to receive a bz whisper")),
                ("bz_whispers_interval", new Property<long>(600, "CustomDM: How often a player can receive a bz whisper")),
                ("minimum_portalspace_seconds", new Property<long>(3, "minimum number of seconds a player must be in portal space before exiting")),
                ("arenas_reward_min_level", new Property<long>(25, "the minimum level required to get arena rewards")),
                ("arenas_reward_min_age", new Property<long>(864000, "the minimum in-game age in seconds required to get arena rewards")),
                ("arenas_min_level", new Property<long>(25, "the minimum level required to join an arena queue")),
                ("pvp_chug_timer", new Property<long>(0, "the minimum time in milliseconds between chugs. if a chug is used within X milliseconds of a previous one, it will heal for 0. if value is set to 0 the feature is disabled.")),
                ("rolling_level_cap_start_timestamp", new Property<long>(0, "Unix timestamp of the day the rolling level cap period began (season day 0). Set this to enable the cap schedule. Cap starts at level 15 on this date.")),
                ("rolling_level_cap", new Property<long>(15, "[Deprecated] Previously stored level number; superseded by rolling_xp_cap. Kept so existing DB rows do not error.")),
                ("rolling_level_cap_timestamp", new Property<long>(0, "[Deprecated] Superseded by rolling_xp_cap_timestamp. Kept for DB compatibility.")),
                ("rolling_xp_cap", new Property<long>(354692, "The currently computed total-XP cap. Managed automatically by RollingLevelCapManager — do not set manually.")),
                ("rolling_xp_cap_timestamp", new Property<long>(0, "Unix timestamp of the last time rolling_xp_cap was recalculated. Managed automatically by RollingLevelCapManager.")),
                ("pvp_dmg_mod_preset_applied_level", new Property<long>(-1, "Level threshold of the last pvp_dmg_mod preset applied by RollingLevelCapManager. -1 means no preset has been applied yet. Managed automatically — do not set manually.")),
                ("season_max_xp", new Property<long>(80_000_000_000, "Total XP ceiling at the end of the season (day 120). Should be high enough that every player template can max all skills and attributes. Level 126 is reached at day 60; days 60-120 grow linearly from level-126 XP to this value.")),

                // Random Dungeon Bosses
                ("dungeon_boss_min_seconds_between", new Property<long>(1800, "Global minimum number of seconds between any two Dungeon Boss spawns. A boss cannot spawn anywhere until this many seconds have elapsed since the last boss spawned. Default 1800 (30 min).")),
                ("dungeon_boss_max_age_hours", new Property<long>(4, "Safety cap: if a Dungeon Boss has been tracked as active for longer than this many hours (e.g. its landblock unloaded without a death event), the manager releases its slot so the landblock and weenie can host another. Default 4.")),
                ("dungeon_boss_trophy_count", new Property<long>(10, "Number of PK Trophies awarded to each player who damaged a Dungeon Boss when it is slain. Trophies are Bonded and cannot be placed on the ground, so they go directly to inventory. 0 disables.")),
                ("dungeon_boss_box_count", new Property<long>(3, "Number of A Box rewards scattered on the ground around a Dungeon Boss when it is slain (contestable). A Box is not bonded so it can be floored. 0 disables.")),
                ("dungeon_boss_phial_count", new Property<long>(3, "Number of Phials of Bloody Tears awarded to each player who damaged a Dungeon Boss when it is slain. Phials are Bonded/Attuned and cannot be placed on the ground, so they go directly to inventory. 0 disables.")),
                ("movement_packet_rate_limit", new Property<long>(75, "script detection: maximum movement packets per second before enforce_player_packet_rate triggers; measured over a 2-second rolling window. Default 75: legitimate clients reach ~35/s on fast machines and during glitch-running; 75 leaves a safe margin without catching scripts (which flood at 100+/s). Do NOT set at or below ~40 — confirmed legitimate players have logged 32-34/s")),
                ("movement_avg_sustained_kick_windows", new Property<long>(3, "anti-cheat: number of CONSECUTIVE 15-second average-speed windows over the ceiling before a decisive kick, independent of the suspicion score. 3 = ~45 s of sustained over-ceiling speed (not achievable legitimately — glitch-runners never trip the ceiling). Blatant overages (>= 50% over) kick one window sooner. Lower = faster kick / marginally higher false-positive risk; do not set below 2")),

                // Bounty Hunter system
                ("bounty_expiration_time",                new Property<long>(60,         "Minutes until a bounty contract expires after purchase.")),
                ("bounty_cooldown_expiration_time",       new Property<long>(0,          "Minutes a hunter must wait after turning in a bounty before purchasing a new one (0 = no cooldown).")),
                ("bounty_cooldown_target_expiration_time", new Property<long>(30,        "Minutes before a previously-killed target can be purchased as a new bounty by the same hunter.")),
                ("bounty_minimum_player_level",           new Property<long>(100,        "Minimum character level required to be a bounty target. Set at or below max level (126) for ClassicPvP.")),
                ("season_cache_ttl_minutes",              new Property<long>(5,           "How long (minutes) the Season leaderboard cache is considered fresh before a DB refresh. Default 5.")),
                ("bounty_kill_streak_minimum",            new Property<long>(4,          "Minimum kill streak before a target is flagged as a streak target in bounty logic.")),
                ("pk_bounty_timer",                       new Property<long>(120,        "Seconds for the PK timer when a bounty hunter has their target in sight (overrides pk_timer).")),
                ("bounty_currency_wcid",                  new Property<long>(1000002,    "WCID of the currency used to purchase bounty contracts (from the NPC). Also refunded when a contract expires because the target is unavailable.")),
                ("bounty_currency_return_amount",         new Property<long>(25,         "Amount of bounty currency returned when a contract expires.")),
                ("bounty_completion_reward_amount",       new Property<long>(100,        "Amount of bounty currency awarded when a contract is successfully completed (refunds the purchase token cost).")),
                ("bounty_failed_reward_amount",           new Property<long>(100,        "Amount of bounty currency awarded to a bounty target who kills their hunter and turns in the failed contract looted from the hunter's corpse.")),
                ("bounty_wop_currency_wcid",              new Property<long>(0,          "WCID of the currency used for Writs of Pursuit rewards.")),
                ("bounty_wop_minimum_amount",            new Property<long>(200,        "Minimum reward amount that must be inscribed on a Writ of Pursuit for it to be accepted.")),
                ("bounty_location_currency_wcid",         new Property<long>(0,          "WCID of the currency consumed to use the location finder on a bounty contract.")),
                ("bounty_location_price_amount",          new Property<long>(25,         "Amount of location currency consumed per location finder use.")),
                ("bounty_max_contracts",                  new Property<long>(3,          "Maximum number of active bounty contracts a hunter can hold at once.")),

                // Allegiance Hometown
                ("ah_phase1_seconds",                     new Property<long>(240,        "Seconds an attacking allegiance must hold the bind stone (2+ attackers within 5m, no enemy interruption) to complete Phase 1 and start Phase 2.")),

                // Allegiance swearing — PK-trophy cost
                ("allegiance_free_swears",                new Property<long>(3,          "Number of times a character may swear allegiance for free before PK-trophy costs apply.")),
                ("allegiance_swear_base_cost",            new Property<long>(100,        "PK-trophy cost of the first paid allegiance swear (the one after allegiance_free_swears).")),
                ("allegiance_swear_max_cost",             new Property<long>(10000,      "Maximum PK-trophy cost of an allegiance swear. The cost ramps from the base to this cap over 12 paid swears (with 3 free, the cap is reached at the 15th swear)."))
                );

        public static readonly ReadOnlyDictionary<string, Property<double>> DefaultDoubleProperties =
            DictOf(

                ("ah_capture_protection_hours", new Property<double>(8.0, "Hours a freshly captured Allegiance Hometown town is protected from re-attack.")),
                ("ah_bindstone_melee_dmg_mod", new Property<double>(0.35, "Multiplier applied to melee damage dealt to the Phase 2 Bind Stone proxy (any weapon skill that is not Bow/Crossbow/Thrown, including unarmed). War magic is unaffected (separate path). Lower = more resistance. 1.0 disables the reduction.")),
                ("ah_bindstone_missile_dmg_mod", new Property<double>(0.35, "Multiplier applied to missile damage dealt to the Phase 2 Bind Stone proxy (Bow, Crossbow, or Thrown Weapon skill). War magic is unaffected (separate path). Lower = more resistance. 1.0 disables the reduction.")),

                ("movement_avg_ceiling_3s", new Property<double>(1.30, "anti-cheat: 3-second average-speed window ceiling, as a multiplier over the time-integrated legitimate movement allowance (4.0 x effective run rate per segment). Violations score when sustained average exceeds this. Lower = stricter. Legit strafe-running is already budgeted separately; keep >= ~1.2 for burst headroom (downhill, lag catch-up)")),
                ("movement_avg_ceiling_15s", new Property<double>(1.15, "anti-cheat: 15-second average-speed window ceiling, as a multiplier over the time-integrated legitimate movement allowance (4.0 x effective run rate per segment). The primary sustained-speed-hack detector: a +20% quickness hack exceeds the default and alerts every window; the violation streak escalator kicks sustained offenders within minutes. Lower = stricter; do not set below ~1.08")),
                ("movement_avg_strafe_ceiling_15s", new Property<double>(1.30, "anti-cheat: max sustained allowance for STRAFE segments on the 15-second window, as a flat multiplier over 4.0 x run rate (NOT stacked on movement_avg_ceiling_15s). Legit strafe-running is 1.248x forward speed; the default 1.30 adds a small margin. This is the cap a quickness hacker gets by faking the sidestep flag, so it is the tightest a determined speed cheat can sustain — lower toward 1.26 to tighten further (a real strafe-runner almost never averages 1.248x over a full 15 s, so this has large real margin), at a small false-positive risk. Do not set below 1.248")),
                ("movement_avg_strafe_ceiling_3s", new Property<double>(1.45, "anti-cheat: max allowance for STRAFE segments on the 3-second window, as a flat multiplier over 4.0 x run rate. Looser than the 15s strafe ceiling to keep burst headroom (downhill/jump/lag), but tighter than the old derived 1.62 so moderate quickness hacks trip the fast window too. Keep >= movement_avg_strafe_ceiling_15s and above ~1.35 for burst safety")),

                ("cantrip_drop_rate", new Property<double>(1.0, "Scales the chance for cantrips to drop in each tier. Defaults to 1.0, as per end of retail")),
                ("cloak_cooldown_seconds", new Property<double>(5.0, "The number of seconds between possible cloak procs.")),
                ("cloak_max_proc_base", new Property<double>(0.25, "The max proc chance of a cloak.")),
                ("cloak_max_proc_damage_percentage", new Property<double>(0.30, "The damage percentage at which cloak proc chance plateaus.")),
                ("cloak_min_proc", new Property<double>(0, "The minimum proc chance of a cloak.")),
                ("minor_cantrip_drop_rate", new Property<double>(1.0, "Scales the chance for minor cantrips to drop, relative to other cantrip levels in the tier. Defaults to 1.0, as per end of retail")),
                ("major_cantrip_drop_rate", new Property<double>(1.0, "Scales the chance for major cantrips to drop, relative to other cantrip levels in the tier. Defaults to 1.0, as per end of retail")),
                ("epic_cantrip_drop_rate", new Property<double>(1.0, "Scales the chance for epic cantrips to drop, relative to other cantrip levels in the tier. Defaults to 1.0, as per end of retail")),
                ("legendary_cantrip_drop_rate", new Property<double>(1.0, "Scales the chance for legendary cantrips to drop, relative to other cantrip levels in the tier. Defaults to 1.0, as per end of retail")),

                ("advocate_fane_auto_bestow_level", new Property<double>(1, "the level that advocates are automatically bestowed by Advocate Fane if advocate_fane_auto_bestow is true")),
                ("aetheria_drop_rate", new Property<double>(1.0, "Modifier for Aetheria drop rate, 1 being normal")),
                ("chess_ai_start_time", new Property<double>(-1.0, "the number of seconds for the chess ai to start. defaults to -1 (disabled)")),
                ("encounter_delay", new Property<double>(1800, "the number of seconds a generator profile for regions is delayed from returning to free slots")),
                ("encounter_regen_interval", new Property<double>(600, "the number of seconds a generator for regions at which spawns its next set of objects")),
                ("equipmentsetid_drop_rate", new Property<double>(1.0, "Modifier for EquipmentSetID drop rate, 1 being normal")),
                ("fast_missile_modifier", new Property<double>(1.2, "The speed multiplier applied to fast missiles. Defaults to retail value of 1.2")),
                ("ignore_magic_armor_pvp_scalar", new Property<double>(1.0, "Scales the effectiveness of IgnoreMagicArmor (ie. hollow weapons) in pvp battles. 1.0 = full effectiveness / ignore all enchantments on armor (default), 0.5 = half effectiveness / use half enchantments from armor, 0.0 = no effectiveness / use full enchantments from armor")),
                ("ignore_magic_resist_pvp_scalar", new Property<double>(1.0, "Scales the effectiveness of IgnoreMagicResist (ie. hollow weapons) in pvp battles. 1.0 = full effectiveness / ignore all resistances from life enchantments (default), 0.5 = half effectiveness / use half resistances from life enchantments, 0.0 = no effectiveness / use full resistances from life enchantments")),
                ("luminance_modifier", new Property<double>(1.0, "Scales the amount of luminance received by players")),
                ("melee_max_angle", new Property<double>(0.0, "for melee players, the maximum angle before a TurnTo is required. retail appeared to have required a TurnTo even for the smallest of angle offsets.")),
                ("missile_aim_center_mass_high", new Property<double>(0.75, "MISSILE FIX 4 tuning: fraction of target Height aimed at for AttackHeight.High when missile_aim_center_mass is enabled. 0.75 = upper collision sphere center. only used when missile_aim_center_mass is true")),
                ("missile_aim_center_mass_medium", new Property<double>(0.62, "MISSILE FIX 4 tuning: fraction of target Height aimed at for AttackHeight.Medium when missile_aim_center_mass is enabled. only used when missile_aim_center_mass is true")),
                ("missile_aim_center_mass_low", new Property<double>(0.27, "MISSILE FIX 4 tuning: fraction of target Height aimed at for AttackHeight.Low when missile_aim_center_mass is enabled. 0.27 = lower collision sphere center. only used when missile_aim_center_mass is true")),
                ("mob_awareness_range", new Property<double>(1.0, "Scales the distance the monsters become alerted and aggro the players")),
                ("player_update_position_threshold", new Property<double>(1.0, "MISSILE FIX 5: seconds between forced position broadcasts for a moving player. between broadcasts, other clients dead-reckon them from MoveToState, so the server position a missile is aimed at can diverge from what the shooter sees. retail-estimated default is 1.0; lower (0.2-0.33) for tighter pvp sync at the cost of bandwidth")),
                ("pk_new_character_grace_period", new Property<double>(300, "the number of seconds, in addition to pk_respite_timer, that a player killer is set to non-player killer status after first exiting training academy")),
                ("pk_respite_timer", new Property<double>(300, "the number of seconds that a player killer is set to non-player killer status after dying to another player killer")),
                ("positive_spell_duration_modifier", new Property<double>(1.0, "Multiplier applied to the duration of beneficial spells when cast. 1.0 = no change, 1.5 = 50% longer. Result is rounded up to the nearest second. Does not affect DoTs, weapon spells, or item enchantments.")),
                ("quest_lum_modifier", new Property<double>(1.0, "Scale multiplier for amount of quest luminance received by players.  Quest lum is also modified by 'luminance_modifier'.")),
                ("quest_mindelta_rate", new Property<double>(1.0, "scales all quest min delta time between solves, 1 being normal")),
                ("quest_xp_modifier", new Property<double>(1.0, "Scale multiplier for amount of quest XP received by players.  Quest XP is also modified by 'xp_modifier'.")),
                ("rare_drop_rate_percent", new Property<double>(0.04, "Adjust the chance of a rare to spawn as a percentage. Default is 0.04, or 1 in 2,500. Max is 100, or every eligible drop.")),
                ("spellcast_max_angle", new Property<double>(20.0, "for advanced player spell casting, the maximum angle to target release a spell projectile. retail seemed to default to value of around 20, although some players seem to prefer a higher 45 degree angle")),
                ("trophy_drop_rate", new Property<double>(1.0, "Modifier for trophies dropped on creature death")),
                ("unlocker_window", new Property<double>(10.0, "The number of seconds a player unlocking a chest has exclusive access to first opening the chest.")),
                ("vendor_unique_rot_time", new Property<double>(300, "the number of seconds before unique items sold to vendors disappear")),
                ("vitae_penalty", new Property<double>(0.05, "the amount of vitae penalty a player gets per death")),
                ("vitae_penalty_max", new Property<double>(0.40, "the maximum vitae penalty a player can have")),
                ("void_pvp_modifier", new Property<double>(0.5, "Scales the amount of damage players take from Void Magic. Defaults to 0.5, as per retail. For earlier content where DRR isn't as readily available, this can be adjusted for balance.")),
                ("rolling_xp_modifier_max", new Property<double>(3.0, "The maximum xp_modifier value applied when rolling_xp_modifier_enabled is true. The quadratic curve is re-anchored to this value, so the season still hits 1.0 at ~36% and this maximum at 80%. Default: 3.0.")),

                // Random Dungeon Bosses
                ("dungeon_boss_spawn_chance", new Property<double>(0.0005, "Per-eligible-monster-spawn probability (0.0-1.0) that a normal monster in an active Hot Dungeon or the Abandoned Mine is replaced by a Dungeon Boss. Default 0.0005 (~1 boss per 2000 monster spawns).")),
                ("dungeon_boss_difficulty_mult", new Property<double>(1.0, "Global multiplier applied on top of the level-cap scaling for Dungeon Bosses. Affects health, damage, skills and armor together. 1.0 = as authored/scaled; raise to make all bosses tougher.")),
                ("dungeon_boss_health_exponent", new Property<double>(1.4, "Exponent for Dungeon Boss health scaling vs the level cap: health = base * (cap / referenceLevel) ^ exponent. Higher = spongier bosses at high level. Default 1.4.")),
                ("dungeon_boss_damage_mult", new Property<double>(1.0, "Additional multiplier applied to Dungeon Boss melee (body-part) damage after level-cap scaling. Default 1.0.")),
                ("dungeon_boss_defense_mult", new Property<double>(1.0, "Multiplier on Dungeon Boss defensive skills (melee/missile/magic defense = how often it evades attacks and resists spells). Lower this if bosses resist/evade too often against near-maxed characters; raise it to make them harder to land on. Default 1.0.")),
                ("dungeon_boss_armor_mult", new Property<double>(1.0, "Multiplier on Dungeon Boss natural armor, which mitigates MELEE and MISSILE damage only (spell damage ignores armor entirely). Lower this if melee/missile hit bosses for too little; raise it to make bosses tankier against weapons without touching their health, damage or magic resistance. Default 1.0.")),
                ("xp_modifier", new Property<double>(1.0, "Globally scales the amount of xp received by players, note that this multiplies the other xp_modifier options.")),
                ("xp_modifier_kill_tier1", new Property<double>(1.0, "Scales the amount of xp received by players for killing tier 1 creatures or unspecified tier creatures below level 28.")),
                ("xp_modifier_kill_tier2", new Property<double>(1.0, "Scales the amount of xp received by players for killing tier 2 creatures or unspecified tier creatures between level 28 and level 65.")),
                ("xp_modifier_kill_tier3", new Property<double>(1.0, "Scales the amount of xp received by players for killing tier 3 creatures or unspecified tier creatures between level 65 and level 95.")),
                ("xp_modifier_kill_tier4", new Property<double>(1.0, "Scales the amount of xp received by players for killing tier 4 creatures or unspecified tier creatures between level 95 and level 110.")),
                ("xp_modifier_kill_tier5", new Property<double>(1.0, "Scales the amount of xp received by players for killing tier 5 creatures or unspecified tier creatures between level 110 and level 135.")),
                ("xp_modifier_kill_tier6", new Property<double>(1.0, "Scales the amount of xp received by players for killing tier 6 creatures or unspecified tier creatures above level 135.")),
                ("xp_modifier_reward_tier1", new Property<double>(1.0, "Scales the amount of xp received by players for completing tier 1 quests or unspecified level quests while being under level 16.")),
                ("xp_modifier_reward_tier2", new Property<double>(1.0, "Scales the amount of xp received by players for completing tier 2 quests or unspecified level quests while being between level 16 and 36.")),
                ("xp_modifier_reward_tier3", new Property<double>(1.0, "Scales the amount of xp received by players for completing tier 3 quests or unspecified level quests while being between level 36 and 56.")),
                ("xp_modifier_reward_tier4", new Property<double>(1.0, "Scales the amount of xp received by players for completing tier 4 quests or unspecified level quests while being between level 56 and 76.")),
                ("xp_modifier_reward_tier5", new Property<double>(1.0, "Scales the amount of xp received by players for completing tier 5 quests or unspecified level quests while being between level 76 and 96.")),
                ("xp_modifier_reward_tier6", new Property<double>(1.0, "Scales the amount of xp received by players for completing tier 6 quests or unspecified level quests while being over level 96.")),
                ("salvage_amount_multiplier", new Property<double>(1.0, "Scales the amount of salvage a player gets from items.")),

                // Hardcore settings
                ("hardcore_npk_death_level_modifier", new Property<double>(1.0, "Percentage of levels lost on death for Hardcore NPK gameplay mode. A value of 1.0 means deaths reset the player to level 1.")),
                ("hardcore_pk_pvp_death_level_modifier", new Property<double>(1.0, "Percentage of levels lost on death for Hardcore PK gameplay mode when dying in PvP. A value of 1.0 means deaths reset the player to level 1.")),
                ("hardcore_pk_pve_death_level_modifier", new Property<double>(1.0, "Percentage of levels lost on death for Hardcore PK gameplay mode when dying in PvE. A value of 1.0 means deaths reset the player to level 1.")),
                ("hardcore_npk_xp_modifier", new Property<double>(1.0, "Scales the amount of xp received by hardcore NPK players.")),
                ("hardcore_pk_xp_modifier", new Property<double>(1.0, "Scales the amount of xp received by hardcore PK players.")),

                ("hot_dungeon_interval", new Property<double>(7800.0, "The minimum possible duration (in seconds) before a new hot dungeon can be automatically rolled after one was previously activated.")),
                ("hot_dungeon_duration", new Property<double>(7200.0, "The total duration (in seconds) which a hot dungeon will be active.")),
                ("hot_dungeon_roll_delay", new Property<double>(1200.0, "The duration (in seconds) between each chance to automatically roll a new hot dungeon (only applies while there are no hot dungeons active).")),
                ("hot_dungeon_chance", new Property<double>(0.33, "The percentage chance (between 0 and 1) when the server will activate a hot dungeon at each roll interval.")),

                ("hot_dungeon_bonus_xp", new Property<double>(1.0, "Extra xp earned for kills when inside hot dungeons. 1.0 means 100% more xp.")),
                ("exploration_bonus_xp", new Property<double>(0.5, "Extra xp earned while completing exploration assignment's objectives. 1.0 means 100% more xp.")),
                ("relive_bonus_xp", new Property<double>(1.0, "Extra xp earned while reliving levels after a death that resulted in lost levels. 1.0 means 100% more xp.")),
                ("bz_whispers_chance", new Property<double>(0.2, "CustomDM: The chance a player will receive a bz whisper every bz_whispers_interval")),
                ("force_logout_materialization_duration", new Property<double>(1.0, "seconds a player materializes for before logging out")),
                ("force_teleport_materialization_duration", new Property<double>(10.0, "seconds after teleporting that a player is held in portal space")),
                ("force_login_materialization_duration", new Property<double>(10.0, "seconds after logging in that a player is held in portal space")),
                ("recent_teleport_threshold", new Property<double>(3.0, "seconds after materializing before a player can teleport again")),
                ("arena_corpse_rot_seconds", new Property<double>(900, "the number of seconds a corpse generated in an arena landblock takes to rot")),
                ("arena_pk_respite_timer", new Property<double>(120, "the number of seconds a player killer is set to NPK status after dying in an arena match")),
                ("arena_1v1_healkit_skill_bonus_cap", new Property<double>(150, "the maximum effective skill bonus applied from a healing kit during arena 1v1 events.")),
                ("arena_1v1_healkit_restoration_bonus_cap", new Property<double>(1.5, "the maximum effective restoration bonus applied from a healing kit during arena 1v1 events.")),
                ("arena_1v1_drain_health_mod", new Property<double>(1.0, "a modifier applied to the effectiveness of drain health spells during arena 1v1 events. 0.5 means drain health spells drain and transfer 50% as much health as they normally would.")),

                // Doctide flat PvP damage modifiers (additive with ClassicPvP's level-interpolated system; all default 1.0)
                // War magic
                ("pvp_dmg_mod_war", new Property<double>(1.0, "Scales the amount of damage for war magic")),
                ("pvp_dmg_mod_war_variance", new Property<double>(1.0, "Scales the low end for war magic bolts and arcs without affecting top end. Values under 1 reduce variance/increase min dmg; values over 1 increase variance/reduce min dmg.")),
                ("pvp_dmg_mod_war_streak", new Property<double>(1.0, "Scales the amount of damage for war streaks")),
                ("pvp_dmg_mod_war_blast", new Property<double>(1.0, "Scales the amount of damage for war blasts")),
                ("pvp_dmg_mod_war_cb_crit", new Property<double>(1.0, "Scales the amount of CB crit damage for war magic")),
                ("pvp_dmg_mod_war_cs_crit", new Property<double>(1.0, "Scales the amount of CS crit damage for war magic")),
                ("pvp_dmg_mod_war_cs_dmg", new Property<double>(1.0, "Scales the amount of CS damage for war magic for all hits (both crits and non-crits)")),
                ("pvp_dmg_mod_war_weeping", new Property<double>(1.0, "Scales war magic damage from Weeping (Human Slayer) casters in PvP")),

                // Void magic
                ("pvp_dmg_mod_void", new Property<double>(1.0, "Scales the amount of damage players take from Void Magic (not including streaks and DOTs which have their own mods)")),
                ("pvp_dmg_mod_void_streak", new Property<double>(1.0, "Scales the amount of damage for void streaks")),
                ("pvp_dmg_mod_void_dot", new Property<double>(1.0, "Scales the amount of damage for void DOTs")),
                ("pvp_dmg_mod_void_crit", new Property<double>(1.0, "Scales the amount of crit damage for void magic")),
                ("pvp_dmg_mod_void_cb_crit", new Property<double>(1.0, "Scales the amount of CB crit damage for void magic")),
                ("pvp_dmg_mod_void_variance", new Property<double>(1.0, "Scales the low end for void magic bolts and arcs without affecting top end. Values under 1 reduce variance/increase min dmg; values over 1 increase variance/reduce min dmg.")),
                ("pvp_dmg_mod_void_dot_rating_reduction", new Property<double>(1.0, "Scales the base DOT damage used when computing the NetherDotDamageRating for a PvP void dot")),
                ("pvp_dmg_mod_void_weeping", new Property<double>(1.0, "Scales void magic damage from Weeping (Human Slayer) casters in PvP")),

                // Global PvP effect mods
                ("pvp_dmg_mod_phantom", new Property<double>(1.0, "Scales the amount of damage for phantom weapons in PvP")),
                ("pvp_dmg_mod_hollow", new Property<double>(1.0, "Scales the amount of damage for hollow weapons in PvP")),
                ("pvp_dmg_mod_cb", new Property<double>(1.0, "Scales the amount of damage for crippling blow in PvP")),
                ("pvp_dmg_mod_ar", new Property<double>(1.0, "Scales the amount of damage for armor rending in PvP")),
                ("pvp_dmg_mod_cs", new Property<double>(1.0, "Scales the amount of damage for critical strike in PvP")),

                // Finesse Weapons
                ("pvp_dmg_mod_fw", new Property<double>(1.0, "Scales the amount of damage for Finesse Weapons in PvP")),
                ("pvp_dmg_mod_fw_cb", new Property<double>(1.0, "Scales the amount of damage for FW CB")),
                ("pvp_dmg_mod_fw_cb_crit", new Property<double>(1.0, "Scales the amount of crit damage for FW CB")),
                ("pvp_dmg_mod_fw_cs", new Property<double>(1.0, "Scales the amount of damage for FW CS")),
                ("pvp_dmg_mod_fw_ar", new Property<double>(1.0, "Scales the amount of damage for FW AR")),
                ("pvp_dmg_mod_fw_hollow", new Property<double>(1.0, "Scales the amount of damage for FW Hollow")),
                ("pvp_dmg_mod_fw_phantom", new Property<double>(1.0, "Scales the amount of damage for FW Phantom")),
                ("pvp_dmg_mod_fw_weeping", new Property<double>(1.0, "Scales the amount of damage for Weeping (Human Slayer) FW in PvP")),

                // Light Weapons
                ("pvp_dmg_mod_lw", new Property<double>(1.0, "Scales the amount of damage for Light Weapons in PvP")),
                ("pvp_dmg_mod_lw_cb", new Property<double>(1.0, "Scales the amount of damage for LW CB")),
                ("pvp_dmg_mod_lw_cb_crit", new Property<double>(1.0, "Scales the amount of crit damage for LW CB")),
                ("pvp_dmg_mod_lw_cs", new Property<double>(1.0, "Scales the amount of damage for LW CS")),
                ("pvp_dmg_mod_lw_ar", new Property<double>(1.0, "Scales the amount of damage for LW AR")),
                ("pvp_dmg_mod_lw_hollow", new Property<double>(1.0, "Scales the amount of damage for LW Hollow")),
                ("pvp_dmg_mod_lw_phantom", new Property<double>(1.0, "Scales the amount of damage for LW Phantom")),
                ("pvp_dmg_mod_lw_weeping", new Property<double>(1.0, "Scales the amount of damage for Weeping (Human Slayer) LW in PvP")),
                ("pvp_dmg_mod_lw_triplestrike", new Property<double>(1.0, "Scales the amount of damage for LW Triple Strike weapons")),
                ("pvp_dmg_mod_lw_cb_crit_triplestrike", new Property<double>(1.0, "Scales the amount of CB crit damage for LW Triple Strike weapons")),

                // Heavy Weapons
                ("pvp_dmg_mod_hw", new Property<double>(1.0, "Scales the amount of damage for Heavy Weapons in PvP")),
                ("pvp_dmg_mod_hw_cb", new Property<double>(1.0, "Scales the amount of damage for HW CB")),
                ("pvp_dmg_mod_hw_cb_crit", new Property<double>(1.0, "Scales the amount of crit damage for HW CB")),
                ("pvp_dmg_mod_hw_cs", new Property<double>(1.0, "Scales the amount of damage for HW CS")),
                ("pvp_dmg_mod_hw_ar", new Property<double>(1.0, "Scales the amount of damage for HW AR")),
                ("pvp_dmg_mod_hw_hollow", new Property<double>(1.0, "Scales the amount of damage for HW Hollow")),
                ("pvp_dmg_mod_hw_phantom", new Property<double>(1.0, "Scales the amount of damage for HW Phantom")),
                ("pvp_dmg_mod_hw_weeping", new Property<double>(1.0, "Scales the amount of damage for Weeping (Human Slayer) HW in PvP")),
                ("pvp_dmg_mod_hw_multistrike", new Property<double>(1.0, "Scales the amount of damage for HW Multi Strike weapons")),
                ("pvp_dmg_mod_hw_cb_crit_multistrike", new Property<double>(1.0, "Scales the amount of CB crit damage for HW Multi Strike weapons")),

                // Two Handed Combat
                ("pvp_dmg_mod_2h", new Property<double>(1.0, "Scales the amount of damage for Two Handed Weapons in PvP")),
                ("pvp_dmg_mod_2h_cb", new Property<double>(1.0, "Scales the amount of damage for 2H CB")),
                ("pvp_dmg_mod_2h_cb_crit", new Property<double>(1.0, "Scales the amount of crit damage for 2H CB")),
                ("pvp_dmg_mod_2h_cs", new Property<double>(1.0, "Scales the amount of damage for 2H CS")),
                ("pvp_dmg_mod_2h_ar", new Property<double>(1.0, "Scales the amount of damage for 2H AR")),
                ("pvp_dmg_mod_2h_hollow", new Property<double>(1.0, "Scales the amount of damage for 2H Hollow")),
                ("pvp_dmg_mod_2h_phantom", new Property<double>(1.0, "Scales the amount of damage for 2H Phantom")),
                ("pvp_dmg_mod_2h_weeping", new Property<double>(1.0, "Scales the amount of damage for Weeping (Human Slayer) 2H in PvP")),

                // Crossbow
                ("pvp_dmg_mod_xbow", new Property<double>(1.0, "Scales the amount of damage for Crossbow in PvP")),
                ("pvp_dmg_mod_xbow_cb", new Property<double>(1.0, "Scales the amount of damage for Xbow CB")),
                ("pvp_dmg_mod_xbow_cb_crit", new Property<double>(1.0, "Scales the amount of crit damage for Xbow CB")),
                ("pvp_dmg_mod_xbow_cs", new Property<double>(1.0, "Scales the amount of damage for Xbow CS")),
                ("pvp_dmg_mod_xbow_ar", new Property<double>(1.0, "Scales the amount of damage for Xbow AR")),
                ("pvp_dmg_mod_xbow_hollow", new Property<double>(1.0, "Scales the amount of damage for Xbow Hollow")),
                ("pvp_dmg_mod_xbow_phantom", new Property<double>(1.0, "Scales the amount of damage for Xbow Phantom")),
                ("pvp_dmg_mod_xbow_weeping", new Property<double>(1.0, "Scales the amount of damage for Weeping (Human Slayer) Xbow in PvP")),

                // Bow
                ("pvp_dmg_mod_bow", new Property<double>(1.0, "Scales the amount of damage for Bow in PvP")),
                ("pvp_dmg_mod_bow_cb", new Property<double>(1.0, "Scales the amount of damage for Bow CB")),
                ("pvp_dmg_mod_bow_cb_crit", new Property<double>(1.0, "Scales the amount of crit damage for Bow CB")),
                ("pvp_dmg_mod_bow_cs", new Property<double>(1.0, "Scales the amount of damage for Bow CS")),
                ("pvp_dmg_mod_bow_ar", new Property<double>(1.0, "Scales the amount of damage for Bow AR")),
                ("pvp_dmg_mod_bow_hollow", new Property<double>(1.0, "Scales the amount of damage for Bow Hollow")),
                ("pvp_dmg_mod_bow_phantom", new Property<double>(1.0, "Scales the amount of damage for Bow Phantom")),
                ("pvp_dmg_mod_bow_weeping", new Property<double>(1.0, "Scales the amount of damage for Weeping (Human Slayer) Bow in PvP")),

                // Thrown Weapons
                ("pvp_dmg_mod_tw", new Property<double>(1.0, "Scales the amount of damage for Thrown Weapons in PvP")),
                ("pvp_dmg_mod_tw_cb", new Property<double>(1.0, "Scales the amount of damage for TW CB")),
                ("pvp_dmg_mod_tw_cb_crit", new Property<double>(1.0, "Scales the amount of crit damage for TW CB")),
                ("pvp_dmg_mod_tw_cs", new Property<double>(1.0, "Scales the amount of damage for TW CS")),
                ("pvp_dmg_mod_tw_ar", new Property<double>(1.0, "Scales the amount of damage for TW AR")),
                ("pvp_dmg_mod_tw_hollow", new Property<double>(1.0, "Scales the amount of damage for TW Hollow")),
                ("pvp_dmg_mod_tw_phantom", new Property<double>(1.0, "Scales the amount of damage for TW Phantom")),
                ("pvp_dmg_mod_tw_weeping", new Property<double>(1.0, "Scales the amount of damage for Weeping (Human Slayer) TW in PvP")),

                // ── Infiltration-era individual weapon skill mods ──────────────────────────
                // These use the pre-ToD skill names that are the actual WeaponSkill values on
                // Infiltration-ruleset weapons. The EoR-era entries above (fw/lw/hw/2h/…) are
                // kept for forward compatibility but will not trigger on a Feb 2005 server.

                // Sword (Skill.Sword)
                ("pvp_dmg_mod_sword", new Property<double>(1.0, "Scales the amount of damage for Sword in PvP")),
                ("pvp_dmg_mod_sword_cb", new Property<double>(1.0, "Scales the amount of damage for Sword CB")),
                ("pvp_dmg_mod_sword_cb_crit", new Property<double>(1.0, "Scales the amount of crit damage for Sword CB")),
                ("pvp_dmg_mod_sword_cs", new Property<double>(1.0, "Scales the amount of damage for Sword CS")),
                ("pvp_dmg_mod_sword_ar", new Property<double>(1.0, "Scales the amount of damage for Sword AR")),
                ("pvp_dmg_mod_sword_hollow", new Property<double>(1.0, "Scales the amount of damage for Sword Hollow")),
                ("pvp_dmg_mod_sword_phantom", new Property<double>(1.0, "Scales the amount of damage for Sword Phantom")),
                ("pvp_dmg_mod_sword_weeping", new Property<double>(1.0, "Scales the amount of damage for Weeping (Human Slayer) Sword in PvP")),

                // Mace (Skill.Mace)
                ("pvp_dmg_mod_mace", new Property<double>(1.0, "Scales the amount of damage for Mace in PvP")),
                ("pvp_dmg_mod_mace_cb", new Property<double>(1.0, "Scales the amount of damage for Mace CB")),
                ("pvp_dmg_mod_mace_cb_crit", new Property<double>(1.0, "Scales the amount of crit damage for Mace CB")),
                ("pvp_dmg_mod_mace_cs", new Property<double>(1.0, "Scales the amount of damage for Mace CS")),
                ("pvp_dmg_mod_mace_ar", new Property<double>(1.0, "Scales the amount of damage for Mace AR")),
                ("pvp_dmg_mod_mace_hollow", new Property<double>(1.0, "Scales the amount of damage for Mace Hollow")),
                ("pvp_dmg_mod_mace_phantom", new Property<double>(1.0, "Scales the amount of damage for Mace Phantom")),
                ("pvp_dmg_mod_mace_weeping", new Property<double>(1.0, "Scales the amount of damage for Weeping (Human Slayer) Mace in PvP")),

                // Axe (Skill.Axe)
                ("pvp_dmg_mod_axe", new Property<double>(1.0, "Scales the amount of damage for Axe in PvP")),
                ("pvp_dmg_mod_axe_cb", new Property<double>(1.0, "Scales the amount of damage for Axe CB")),
                ("pvp_dmg_mod_axe_cb_crit", new Property<double>(1.0, "Scales the amount of crit damage for Axe CB")),
                ("pvp_dmg_mod_axe_cs", new Property<double>(1.0, "Scales the amount of damage for Axe CS")),
                ("pvp_dmg_mod_axe_ar", new Property<double>(1.0, "Scales the amount of damage for Axe AR")),
                ("pvp_dmg_mod_axe_hollow", new Property<double>(1.0, "Scales the amount of damage for Axe Hollow")),
                ("pvp_dmg_mod_axe_phantom", new Property<double>(1.0, "Scales the amount of damage for Axe Phantom")),
                ("pvp_dmg_mod_axe_weeping", new Property<double>(1.0, "Scales the amount of damage for Weeping (Human Slayer) Axe in PvP")),

                // Spear (Skill.Spear)
                ("pvp_dmg_mod_spear", new Property<double>(1.0, "Scales the amount of damage for Spear in PvP")),
                ("pvp_dmg_mod_spear_cb", new Property<double>(1.0, "Scales the amount of damage for Spear CB")),
                ("pvp_dmg_mod_spear_cb_crit", new Property<double>(1.0, "Scales the amount of crit damage for Spear CB")),
                ("pvp_dmg_mod_spear_cs", new Property<double>(1.0, "Scales the amount of damage for Spear CS")),
                ("pvp_dmg_mod_spear_ar", new Property<double>(1.0, "Scales the amount of damage for Spear AR")),
                ("pvp_dmg_mod_spear_hollow", new Property<double>(1.0, "Scales the amount of damage for Spear Hollow")),
                ("pvp_dmg_mod_spear_phantom", new Property<double>(1.0, "Scales the amount of damage for Spear Phantom")),
                ("pvp_dmg_mod_spear_weeping", new Property<double>(1.0, "Scales the amount of damage for Weeping (Human Slayer) Spear in PvP")),

                // Staff (Skill.Staff)
                ("pvp_dmg_mod_staff", new Property<double>(1.0, "Scales the amount of damage for Staff in PvP")),
                ("pvp_dmg_mod_staff_cb", new Property<double>(1.0, "Scales the amount of damage for Staff CB")),
                ("pvp_dmg_mod_staff_cb_crit", new Property<double>(1.0, "Scales the amount of crit damage for Staff CB")),
                ("pvp_dmg_mod_staff_cs", new Property<double>(1.0, "Scales the amount of damage for Staff CS")),
                ("pvp_dmg_mod_staff_ar", new Property<double>(1.0, "Scales the amount of damage for Staff AR")),
                ("pvp_dmg_mod_staff_hollow", new Property<double>(1.0, "Scales the amount of damage for Staff Hollow")),
                ("pvp_dmg_mod_staff_phantom", new Property<double>(1.0, "Scales the amount of damage for Staff Phantom")),
                ("pvp_dmg_mod_staff_weeping", new Property<double>(1.0, "Scales the amount of damage for Weeping (Human Slayer) Staff in PvP")),

                // Dagger (Skill.Dagger)
                ("pvp_dmg_mod_dagger", new Property<double>(1.0, "Scales the amount of damage for Dagger in PvP")),
                ("pvp_dmg_mod_dagger_cb", new Property<double>(1.0, "Scales the amount of damage for Dagger CB")),
                ("pvp_dmg_mod_dagger_cb_crit", new Property<double>(1.0, "Scales the amount of crit damage for Dagger CB")),
                ("pvp_dmg_mod_dagger_cs", new Property<double>(1.0, "Scales the amount of damage for Dagger CS")),
                ("pvp_dmg_mod_dagger_ar", new Property<double>(1.0, "Scales the amount of damage for Dagger AR")),
                ("pvp_dmg_mod_dagger_hollow", new Property<double>(1.0, "Scales the amount of damage for Dagger Hollow")),
                ("pvp_dmg_mod_dagger_phantom", new Property<double>(1.0, "Scales the amount of damage for Dagger Phantom")),
                ("pvp_dmg_mod_dagger_weeping", new Property<double>(1.0, "Scales the amount of damage for Weeping (Human Slayer) Dagger in PvP")),

                // Unarmed Combat (Skill.UnarmedCombat)
                ("pvp_dmg_mod_unarmed", new Property<double>(1.0, "Scales the amount of damage for Unarmed Combat in PvP")),
                ("pvp_dmg_mod_unarmed_cb", new Property<double>(1.0, "Scales the amount of damage for Unarmed CB")),
                ("pvp_dmg_mod_unarmed_cb_crit", new Property<double>(1.0, "Scales the amount of crit damage for Unarmed CB")),
                ("pvp_dmg_mod_unarmed_cs", new Property<double>(1.0, "Scales the amount of damage for Unarmed CS")),
                ("pvp_dmg_mod_unarmed_ar", new Property<double>(1.0, "Scales the amount of damage for Unarmed AR")),
                ("pvp_dmg_mod_unarmed_hollow", new Property<double>(1.0, "Scales the amount of damage for Unarmed Hollow")),
                ("pvp_dmg_mod_unarmed_phantom", new Property<double>(1.0, "Scales the amount of damage for Unarmed Phantom")),
                ("pvp_dmg_mod_unarmed_weeping", new Property<double>(1.0, "Scales the amount of damage for Weeping (Human Slayer) Unarmed in PvP")),

                // Misc PvP modifiers
                ("pvp_void_hybrid_mod", new Property<double>(1.0, "Scales the amount of void DOT damage when the attacker is a hybrid void (has trained/specialized melee or war magic skills)")),
                ("pvp_ratings_mod_dmg", new Property<double>(1.0, "Scales the bonus received from damage and damage-resistance ratings during PvP")),
                ("pvp_ratings_mod_critdmg", new Property<double>(1.0, "Scales the bonus received from crit-damage and crit-damage-resistance ratings during PvP")),

                // ── Arena-only PvP damage modifiers ───────────────────────────
                // Mirror of the pvp_dmg_mod_* set above. When the defender is standing in an arena
                // landblock these REPLACE their global counterparts — the two sets never stack.
                // The check is landblock-only (no arena event or event membership is considered) so
                // that it stays cheap enough to run on every damage calculation.
                // Resolved by PvpDmgMod.Get(key, isArena).

                // War magic
                ("pvp_dmg_mod_arena_war", new Property<double>(1.0, "Arena only: Scales the amount of damage for war magic")),
                ("pvp_dmg_mod_arena_war_variance", new Property<double>(1.0, "Arena only: Scales the low end for war magic bolts and arcs without affecting top end. Values under 1 reduce variance/increase min dmg; values over 1 increase variance/reduce min dmg.")),
                ("pvp_dmg_mod_arena_war_streak", new Property<double>(1.0, "Arena only: Scales the amount of damage for war streaks")),
                ("pvp_dmg_mod_arena_war_blast", new Property<double>(1.0, "Arena only: Scales the amount of damage for war blasts")),
                ("pvp_dmg_mod_arena_war_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of CB crit damage for war magic")),
                ("pvp_dmg_mod_arena_war_cs_crit", new Property<double>(1.0, "Arena only: Scales the amount of CS crit damage for war magic")),
                ("pvp_dmg_mod_arena_war_cs_dmg", new Property<double>(1.0, "Arena only: Scales the amount of CS damage for war magic for all hits (both crits and non-crits)")),
                ("pvp_dmg_mod_arena_war_weeping", new Property<double>(1.0, "Arena only: Scales war magic damage from Weeping (Human Slayer) casters in PvP")),

                // Void magic
                ("pvp_dmg_mod_arena_void", new Property<double>(1.0, "Arena only: Scales the amount of damage players take from Void Magic (not including streaks and DOTs which have their own mods)")),
                ("pvp_dmg_mod_arena_void_streak", new Property<double>(1.0, "Arena only: Scales the amount of damage for void streaks")),
                ("pvp_dmg_mod_arena_void_dot", new Property<double>(1.0, "Arena only: Scales the amount of damage for void DOTs")),
                ("pvp_dmg_mod_arena_void_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for void magic")),
                ("pvp_dmg_mod_arena_void_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of CB crit damage for void magic")),
                ("pvp_dmg_mod_arena_void_variance", new Property<double>(1.0, "Arena only: Scales the low end for void magic bolts and arcs without affecting top end. Values under 1 reduce variance/increase min dmg; values over 1 increase variance/reduce min dmg.")),
                ("pvp_dmg_mod_arena_void_dot_rating_reduction", new Property<double>(1.0, "Arena only: Scales the base DOT damage used when computing the NetherDotDamageRating for a PvP void dot")),
                ("pvp_dmg_mod_arena_void_weeping", new Property<double>(1.0, "Arena only: Scales void magic damage from Weeping (Human Slayer) casters in PvP")),

                // Global PvP effect mods
                ("pvp_dmg_mod_arena_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for phantom weapons in PvP")),
                ("pvp_dmg_mod_arena_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for hollow weapons in PvP")),
                ("pvp_dmg_mod_arena_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for crippling blow in PvP")),
                ("pvp_dmg_mod_arena_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for armor rending in PvP")),
                ("pvp_dmg_mod_arena_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for critical strike in PvP")),

                // Finesse Weapons
                ("pvp_dmg_mod_arena_fw", new Property<double>(1.0, "Arena only: Scales the amount of damage for Finesse Weapons in PvP")),
                ("pvp_dmg_mod_arena_fw_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for FW CB")),
                ("pvp_dmg_mod_arena_fw_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for FW CB")),
                ("pvp_dmg_mod_arena_fw_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for FW CS")),
                ("pvp_dmg_mod_arena_fw_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for FW AR")),
                ("pvp_dmg_mod_arena_fw_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for FW Hollow")),
                ("pvp_dmg_mod_arena_fw_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for FW Phantom")),
                ("pvp_dmg_mod_arena_fw_weeping", new Property<double>(1.0, "Arena only: Scales the amount of damage for Weeping (Human Slayer) FW in PvP")),

                // Light Weapons
                ("pvp_dmg_mod_arena_lw", new Property<double>(1.0, "Arena only: Scales the amount of damage for Light Weapons in PvP")),
                ("pvp_dmg_mod_arena_lw_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for LW CB")),
                ("pvp_dmg_mod_arena_lw_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for LW CB")),
                ("pvp_dmg_mod_arena_lw_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for LW CS")),
                ("pvp_dmg_mod_arena_lw_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for LW AR")),
                ("pvp_dmg_mod_arena_lw_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for LW Hollow")),
                ("pvp_dmg_mod_arena_lw_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for LW Phantom")),
                ("pvp_dmg_mod_arena_lw_weeping", new Property<double>(1.0, "Arena only: Scales the amount of damage for Weeping (Human Slayer) LW in PvP")),
                ("pvp_dmg_mod_arena_lw_triplestrike", new Property<double>(1.0, "Arena only: Scales the amount of damage for LW Triple Strike weapons")),
                ("pvp_dmg_mod_arena_lw_cb_crit_triplestrike", new Property<double>(1.0, "Arena only: Scales the amount of CB crit damage for LW Triple Strike weapons")),

                // Heavy Weapons
                ("pvp_dmg_mod_arena_hw", new Property<double>(1.0, "Arena only: Scales the amount of damage for Heavy Weapons in PvP")),
                ("pvp_dmg_mod_arena_hw_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for HW CB")),
                ("pvp_dmg_mod_arena_hw_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for HW CB")),
                ("pvp_dmg_mod_arena_hw_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for HW CS")),
                ("pvp_dmg_mod_arena_hw_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for HW AR")),
                ("pvp_dmg_mod_arena_hw_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for HW Hollow")),
                ("pvp_dmg_mod_arena_hw_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for HW Phantom")),
                ("pvp_dmg_mod_arena_hw_weeping", new Property<double>(1.0, "Arena only: Scales the amount of damage for Weeping (Human Slayer) HW in PvP")),
                ("pvp_dmg_mod_arena_hw_multistrike", new Property<double>(1.0, "Arena only: Scales the amount of damage for HW Multi Strike weapons")),
                ("pvp_dmg_mod_arena_hw_cb_crit_multistrike", new Property<double>(1.0, "Arena only: Scales the amount of CB crit damage for HW Multi Strike weapons")),

                // Two Handed Combat
                ("pvp_dmg_mod_arena_2h", new Property<double>(1.0, "Arena only: Scales the amount of damage for Two Handed Weapons in PvP")),
                ("pvp_dmg_mod_arena_2h_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for 2H CB")),
                ("pvp_dmg_mod_arena_2h_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for 2H CB")),
                ("pvp_dmg_mod_arena_2h_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for 2H CS")),
                ("pvp_dmg_mod_arena_2h_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for 2H AR")),
                ("pvp_dmg_mod_arena_2h_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for 2H Hollow")),
                ("pvp_dmg_mod_arena_2h_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for 2H Phantom")),
                ("pvp_dmg_mod_arena_2h_weeping", new Property<double>(1.0, "Arena only: Scales the amount of damage for Weeping (Human Slayer) 2H in PvP")),

                // Crossbow
                ("pvp_dmg_mod_arena_xbow", new Property<double>(1.0, "Arena only: Scales the amount of damage for Crossbow in PvP")),
                ("pvp_dmg_mod_arena_xbow_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for Xbow CB")),
                ("pvp_dmg_mod_arena_xbow_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for Xbow CB")),
                ("pvp_dmg_mod_arena_xbow_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for Xbow CS")),
                ("pvp_dmg_mod_arena_xbow_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for Xbow AR")),
                ("pvp_dmg_mod_arena_xbow_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for Xbow Hollow")),
                ("pvp_dmg_mod_arena_xbow_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for Xbow Phantom")),
                ("pvp_dmg_mod_arena_xbow_weeping", new Property<double>(1.0, "Arena only: Scales the amount of damage for Weeping (Human Slayer) Xbow in PvP")),

                // Bow
                ("pvp_dmg_mod_arena_bow", new Property<double>(1.0, "Arena only: Scales the amount of damage for Bow in PvP")),
                ("pvp_dmg_mod_arena_bow_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for Bow CB")),
                ("pvp_dmg_mod_arena_bow_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for Bow CB")),
                ("pvp_dmg_mod_arena_bow_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for Bow CS")),
                ("pvp_dmg_mod_arena_bow_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for Bow AR")),
                ("pvp_dmg_mod_arena_bow_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for Bow Hollow")),
                ("pvp_dmg_mod_arena_bow_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for Bow Phantom")),
                ("pvp_dmg_mod_arena_bow_weeping", new Property<double>(1.0, "Arena only: Scales the amount of damage for Weeping (Human Slayer) Bow in PvP")),

                // Thrown Weapons
                ("pvp_dmg_mod_arena_tw", new Property<double>(1.0, "Arena only: Scales the amount of damage for Thrown Weapons in PvP")),
                ("pvp_dmg_mod_arena_tw_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for TW CB")),
                ("pvp_dmg_mod_arena_tw_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for TW CB")),
                ("pvp_dmg_mod_arena_tw_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for TW CS")),
                ("pvp_dmg_mod_arena_tw_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for TW AR")),
                ("pvp_dmg_mod_arena_tw_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for TW Hollow")),
                ("pvp_dmg_mod_arena_tw_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for TW Phantom")),
                ("pvp_dmg_mod_arena_tw_weeping", new Property<double>(1.0, "Arena only: Scales the amount of damage for Weeping (Human Slayer) TW in PvP")),

                // ── Infiltration-era individual weapon skill mods ──────────────────────────
                // These use the pre-ToD skill names that are the actual WeaponSkill values on
                // Infiltration-ruleset weapons. The EoR-era entries above (fw/lw/hw/2h/…) are
                // kept for forward compatibility but will not trigger on a Feb 2005 server.

                // Sword (Skill.Sword)
                ("pvp_dmg_mod_arena_sword", new Property<double>(1.0, "Arena only: Scales the amount of damage for Sword in PvP")),
                ("pvp_dmg_mod_arena_sword_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for Sword CB")),
                ("pvp_dmg_mod_arena_sword_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for Sword CB")),
                ("pvp_dmg_mod_arena_sword_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for Sword CS")),
                ("pvp_dmg_mod_arena_sword_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for Sword AR")),
                ("pvp_dmg_mod_arena_sword_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for Sword Hollow")),
                ("pvp_dmg_mod_arena_sword_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for Sword Phantom")),
                ("pvp_dmg_mod_arena_sword_weeping", new Property<double>(1.0, "Arena only: Scales the amount of damage for Weeping (Human Slayer) Sword in PvP")),

                // Mace (Skill.Mace)
                ("pvp_dmg_mod_arena_mace", new Property<double>(1.0, "Arena only: Scales the amount of damage for Mace in PvP")),
                ("pvp_dmg_mod_arena_mace_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for Mace CB")),
                ("pvp_dmg_mod_arena_mace_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for Mace CB")),
                ("pvp_dmg_mod_arena_mace_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for Mace CS")),
                ("pvp_dmg_mod_arena_mace_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for Mace AR")),
                ("pvp_dmg_mod_arena_mace_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for Mace Hollow")),
                ("pvp_dmg_mod_arena_mace_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for Mace Phantom")),
                ("pvp_dmg_mod_arena_mace_weeping", new Property<double>(1.0, "Arena only: Scales the amount of damage for Weeping (Human Slayer) Mace in PvP")),

                // Axe (Skill.Axe)
                ("pvp_dmg_mod_arena_axe", new Property<double>(1.0, "Arena only: Scales the amount of damage for Axe in PvP")),
                ("pvp_dmg_mod_arena_axe_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for Axe CB")),
                ("pvp_dmg_mod_arena_axe_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for Axe CB")),
                ("pvp_dmg_mod_arena_axe_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for Axe CS")),
                ("pvp_dmg_mod_arena_axe_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for Axe AR")),
                ("pvp_dmg_mod_arena_axe_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for Axe Hollow")),
                ("pvp_dmg_mod_arena_axe_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for Axe Phantom")),
                ("pvp_dmg_mod_arena_axe_weeping", new Property<double>(1.0, "Arena only: Scales the amount of damage for Weeping (Human Slayer) Axe in PvP")),

                // Spear (Skill.Spear)
                ("pvp_dmg_mod_arena_spear", new Property<double>(1.0, "Arena only: Scales the amount of damage for Spear in PvP")),
                ("pvp_dmg_mod_arena_spear_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for Spear CB")),
                ("pvp_dmg_mod_arena_spear_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for Spear CB")),
                ("pvp_dmg_mod_arena_spear_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for Spear CS")),
                ("pvp_dmg_mod_arena_spear_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for Spear AR")),
                ("pvp_dmg_mod_arena_spear_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for Spear Hollow")),
                ("pvp_dmg_mod_arena_spear_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for Spear Phantom")),
                ("pvp_dmg_mod_arena_spear_weeping", new Property<double>(1.0, "Arena only: Scales the amount of damage for Weeping (Human Slayer) Spear in PvP")),

                // Staff (Skill.Staff)
                ("pvp_dmg_mod_arena_staff", new Property<double>(1.0, "Arena only: Scales the amount of damage for Staff in PvP")),
                ("pvp_dmg_mod_arena_staff_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for Staff CB")),
                ("pvp_dmg_mod_arena_staff_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for Staff CB")),
                ("pvp_dmg_mod_arena_staff_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for Staff CS")),
                ("pvp_dmg_mod_arena_staff_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for Staff AR")),
                ("pvp_dmg_mod_arena_staff_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for Staff Hollow")),
                ("pvp_dmg_mod_arena_staff_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for Staff Phantom")),
                ("pvp_dmg_mod_arena_staff_weeping", new Property<double>(1.0, "Arena only: Scales the amount of damage for Weeping (Human Slayer) Staff in PvP")),

                // Dagger (Skill.Dagger)
                ("pvp_dmg_mod_arena_dagger", new Property<double>(1.0, "Arena only: Scales the amount of damage for Dagger in PvP")),
                ("pvp_dmg_mod_arena_dagger_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for Dagger CB")),
                ("pvp_dmg_mod_arena_dagger_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for Dagger CB")),
                ("pvp_dmg_mod_arena_dagger_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for Dagger CS")),
                ("pvp_dmg_mod_arena_dagger_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for Dagger AR")),
                ("pvp_dmg_mod_arena_dagger_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for Dagger Hollow")),
                ("pvp_dmg_mod_arena_dagger_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for Dagger Phantom")),
                ("pvp_dmg_mod_arena_dagger_weeping", new Property<double>(1.0, "Arena only: Scales the amount of damage for Weeping (Human Slayer) Dagger in PvP")),

                // Unarmed Combat (Skill.UnarmedCombat)
                ("pvp_dmg_mod_arena_unarmed", new Property<double>(1.0, "Arena only: Scales the amount of damage for Unarmed Combat in PvP")),
                ("pvp_dmg_mod_arena_unarmed_cb", new Property<double>(1.0, "Arena only: Scales the amount of damage for Unarmed CB")),
                ("pvp_dmg_mod_arena_unarmed_cb_crit", new Property<double>(1.0, "Arena only: Scales the amount of crit damage for Unarmed CB")),
                ("pvp_dmg_mod_arena_unarmed_cs", new Property<double>(1.0, "Arena only: Scales the amount of damage for Unarmed CS")),
                ("pvp_dmg_mod_arena_unarmed_ar", new Property<double>(1.0, "Arena only: Scales the amount of damage for Unarmed AR")),
                ("pvp_dmg_mod_arena_unarmed_hollow", new Property<double>(1.0, "Arena only: Scales the amount of damage for Unarmed Hollow")),
                ("pvp_dmg_mod_arena_unarmed_phantom", new Property<double>(1.0, "Arena only: Scales the amount of damage for Unarmed Phantom")),
                ("pvp_dmg_mod_arena_unarmed_weeping", new Property<double>(1.0, "Arena only: Scales the amount of damage for Weeping (Human Slayer) Unarmed in PvP")),

                // Rolling Level Cap — per-category XP ratios
                ("daily_xp_category_ratio", new Property<double>(0.70, "[Deprecated] Superseded by daily_quest_xp_category_ratio and daily_monster_xp_category_ratio. Kept so existing DB rows do not error.")),
                ("daily_quest_xp_category_ratio",   new Property<double>(0.60, "Rolling cap: maximum fraction of a player's remaining cap XP that the Quest category (quests, emotes, exploration) can absorb in one cap period.")),
                ("daily_monster_xp_category_ratio",  new Property<double>(0.60, "Rolling cap: maximum fraction of a player's remaining cap XP that the Monster category (kills, fellowship, allegiance, proficiency) can absorb in one cap period.")),
                ("daily_pvp_xp_category_ratio",      new Property<double>(1.00, "Rolling cap: maximum fraction of a player's remaining cap XP that the PvP category (player kills, arenas, PvP custom content) can absorb in one cap period.")),
                // Catch-up XP boost — scales earned XP for characters behind the season cap
                ("catchup_xp_threshold",             new Property<double>(0.70, "Catch-up XP: fraction of the current season XP cap below which a character earns boosted XP. A character whose total XP is at or above this fraction of the cap gets no boost. Default 0.70 (70%).")),
                ("catchup_xp_max_multiplier",        new Property<double>(5.00, "Catch-up XP: multiplier applied to earned XP for a character with 0 total XP (furthest behind the cap). Default 5.0 (500%).")),
                ("catchup_xp_min_multiplier",        new Property<double>(2.00, "Catch-up XP: multiplier applied to earned XP for a character right at catchup_xp_threshold of the cap (least far behind). Default 2.0 (200%). The multiplier ramps linearly between this and catchup_xp_max_multiplier.")),

                ("ancient_bottle_fill_ratio",        new Property<double>(0.25, "Fraction of overflow PvP XP (beyond the daily PvP or global cap) that is stored into Ancient Bottles. The remainder is lost to the cap. e.g. 0.25 means only 25% of post-cap PK XP is captured.")),

                // PK kill XP rewards
                ("pk_xp_level_diff_decay",           new Property<double>(0.85, "Exponential decay factor applied per level the victim is below the killer when awarding PvP XP on a PK kill. e.g. 0.85 means each level gap multiplies the reward by 0.85.")),
                ("pk_xp_repeat_cooldown_minutes",     new Property<double>(60.0, "Legacy: minutes before the same killer can earn PvP XP again from killing the same victim. Superseded by the kill-window system below.")),
                ("pk_kill_window_hours",              new Property<double>(1.0,  "Hours of sliding window used to count kills against the same victim before diminishing returns kick in.")),
                ("pk_kill_diminish_threshold",        new Property<double>(3.0,  "Number of kills against the same victim within the window before rewards are suppressed.")),
                ("pk_kill_diminish_hours",            new Property<double>(3.0,  "Hours the diminishing-returns suppression lasts once the threshold is exceeded.")),

                // Bounty Hunter system
                ("bounty_last_location_duration",    new Property<double>(30.0,  "Seconds a hunter must wait before using the location finder on the same contract again.")),
                ("bounty_weight_exponent",           new Property<double>(0.75,  "Exponent applied when computing the weighted probability for kill-streak and reward-amount factors (0.25–1.0).")),
                ("bounty_weight_multiplier",         new Property<double>(50.0,  "Additive weight multiplier for kill-streak and Writ of Pursuit reward bonuses.")),
                ("bounty_weight_maxstack_scale",     new Property<double>(0.2,   "Fraction of the WoP currency max stack used as the normalisation cap for reward-amount weight (0.01–1.0).")),
                ("bounty_npc_use_cooldown_seconds",  new Property<double>(3.0,   "Minimum seconds between NPC transactions for a single player (anti-spam).")),
                ("bounty_kill_min_damage_percent",   new Property<double>(0.25,  "Fraction (0.0-1.0) of a target's total damage a hunter must deal (while in visible range) to complete a contract when they did not land the killing blow.")),

                // Hot Dungeon box drops
                ("hot_dungeon_box_drop_multiplier",  new Property<double>(1.0,   "Global multiplier applied to every hot dungeon's per-kill Box drop chance (1.0 = tuned defaults, 0.25 = 75% fewer boxes).")),

                // Hot Dungeon salvage bonus
                ("hot_dungeon_salvage_multiplier",   new Property<double>(2.0,   "Multiplier applied to salvage material yield while the player is inside an active hot dungeon (1.0 = no bonus, 2.0 = double material).")),
                ("hot_dungeon_weapon_quality_bias",   new Property<double>(0.02,  "Chance (0-1) for a weapon rolled in a hot dungeon to be upgraded to the best result available at the wield requirement it rolled into. Rolled independently for damage and for damage variance, so a weapon may get one, both, or neither. Melee and thrown weapons roll both; bows, crossbows, atlatls and casters have no variance mutation and roll damage only. Does not change which wield requirement the weapon rolls. 0 disables.")),
                ("hot_dungeon_weapon_drop_bias",      new Property<double>(0.35,  "Chance (0-1) for a gem, art object or scroll rolled in a hot dungeon to be converted into a weapon instead. 0 disables.")),
                ("hot_dungeon_single_slot_armor_bias",new Property<double>(0.50,  "Chance (0-1) for a multi-slot armor piece rolled in a hot dungeon (coat, cuirass, shirt, sleeves, leggings, boots) to be rerolled for a single-slot piece. 0 disables.")),
                ("hot_dungeon_mundane_upgrade_chance",new Property<double>(0.60,  "Chance (0-1) for a mundane item rolled in a hot dungeon to be upgraded to a weapon or armor piece instead. The number of items dropped is unchanged. 0 disables."))
                );
        
        public static readonly ReadOnlyDictionary<string, Property<string>> DefaultStringProperties =
            DictOf(
                ("content_folder", new Property<string>("Content", "for content creators to live edit weenies. defaults to Content folder found in same directory as ACE.Server.dll")),
                ("dat_older_warning_msg", new Property<string>("Your DAT files are incomplete.\nThis server does not support dynamic DAT updating at this time.\nPlease visit https://emulator.ac/how-to-play to download the complete DAT files.", "Warning message displayed (if show_dat_warning is true) to player if client attempts DAT download from server")),
                ("dat_newer_warning_msg", new Property<string>("Your DAT files are newer than expected.\nPlease visit https://emulator.ac/how-to-play to download the correct DAT files.", "Warning message displayed (if show_dat_warning is true) to player if client connects to this server")),
                ("popup_header", new Property<string>("Welcome to Asheron's Call!", "Welcome message displayed when you log in")),
                ("popup_welcome", new Property<string>("To begin your training, speak to the Society Greeter. Walk up to the Society Greeter using the 'W' key, then double-click on her to initiate a conversation.", "Welcome message popup in training halls")),
                ("popup_welcome_olthoi", new Property<string>("Welcome to the Olthoi hive! Be sure to talk to the Olthoi Queen to receive the Olthoi protections granted by the energies of the hive.", "Welcome message displayed on the first login for an Olthoi Player")),
                ("popup_motd", new Property<string>("", "Popup message of the day")),
                ("server_motd", new Property<string>("", "Server message of the day")),
                ("server_motd2", new Property<string>("", "Server message of the day - Second message")),
                ("server_motd3", new Property<string>("", "Server message of the day - Third message")),
                ("server_motd4", new Property<string>("", "Server message of the day - Fourth message")),
                ("turbine_chat_webhook", new Property<string>("", "Webhook to be used for turbine chat. This is for copying ingame general chat channels to a Discord channel.")),
                ("turbine_chat_webhook_audit", new Property<string>("", "Webhook to be used for ingame audit log.")),
                ("season_milestone_webhook", new Property<string>("", "Discord webhook URL for Season weekly milestone announcements. Leave empty to disable Discord posting.")),
                ("proxycheck_api_key", new Property<string>("", "API key for proxycheck.io service for VPN detection")),
                ("vpn_account_whitelist", new Property<string>("", "A comma separated list of account names for which VPN detection is bypassed")),
                ("ip_binding_ip_whitelist", new Property<string>("", "Comma-separated list of IP addresses exempt from the one-account-per-IP binding rule (e.g. a LAN or staff office IP). Accounts logging in from these IPs bypass conflict checks and change-limit enforcement.")),
                ("ip_binding_ip_allowance", new Property<string>("", "Comma-separated per-IP overrides for the number of distinct accounts allowed to bind to a single IP, formatted 'ip:count'. Example: '203.0.113.42:2, 198.51.100.7:2'. IPs not listed use the default allowance of 1 (one account per IP). Unlike ip_binding_ip_whitelist, this enforces a hard cap instead of unlimited.")),
                ("discord_login_token", new Property<string>("", "Login Token used for Discord chat integration")),
                ("arena_globals_webhook", new Property<string>("", "Webhook for sending arena global messages to Discord")),
                ("arenas_blacklist", new Property<string>("", "Comma-separated list of character/monarch IDs blocked from arena queues")),
                ("whitelisted_allegiances", new Property<string>("", "Comma-separated list of MonarchID values whose allegiances are whitelisted for PK quest kill credit, arena XP, and other whitelist-gated features")),

                // Discord webhooks — per-channel
                ("pk_kill_webhook",    new Property<string>("", "Discord webhook URL for PK and PKL kill broadcast messages")),
                ("hot_dungeon_webhook", new Property<string>("", "Discord webhook URL for Hot Dungeon announcements")),
                ("dungeon_boss_webhook", new Property<string>("", "Discord webhook URL for Dungeon Boss spawn/slain announcements")),
                ("hometown_webhook", new Property<string>("", "Discord webhook URL for Allegiance Hometown global broadcasts (captures, defenses, phase changes)")),

                ("movement_violation_webhook", new Property<string>("", "Discord webhook URL for movement anti-cheat violation alerts (all violation types: speed, geometry, jump, door ghost, script detection, etc.)"))
                );
    }
}
