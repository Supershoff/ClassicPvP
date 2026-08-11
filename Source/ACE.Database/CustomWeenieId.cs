namespace ACE.Database
{
    /// <summary>
    /// Weenie IDs for custom ClassicPvP reward items.
    /// All features that spawn or check these items should reference this class
    /// rather than embedding raw integer literals.
    /// </summary>
    public static class CustomWeenieId
    {
        /// <summary>Darkbeat's Lost Storage Key (arena and PK quest reward)</summary>
        public const uint DarkbeatKey         = 480608;

        /// <summary>A Box (Hot Dungeon drop, Town Control and PK quest reward)</summary>
        public const uint ABox                = 510000;

        /// <summary>PK Trophy (arena, Town Control, PK quest reward and rename currency)</summary>
        public const uint PkTrophy            = 1000002;

        /// <summary>Phial of Bloody Tears (arena, Hot Dungeon and PK quest reward)</summary>
        public const uint PhialOfBloodyTears  = 1000003;

        /// <summary>Bind Stone creature proxy spawned during Phase 2 of Allegiance Hometown Capture</summary>
        public const uint BindstoneCreatureProxy = 1000010;

        public const uint XpBottle = 490071;

        public const uint TinkeringTool = 490298;

        public const uint SkillAttrResetGem = 49090101;

        // ── My Mule ──────────────────────────────────────────────────────────
        // Reusable summon gem for the personal "mule" storage vendor.
        public const uint Mule = 490400;

        /// <summary>
        /// Shared off-world Container weenie backing each player's persistent mule storage. A player
        /// may own a chain of several of these (see PropertyInstanceId.MuleNextContainerId) once
        /// their storage exceeds one container's 255-item capacity.
        /// </summary>
        public const uint MuleStorageContainer = 490408;

        /// <summary>Shared Vendor weenie used as the ephemeral "My Mule" NPC shell.</summary>
        public const uint MuleVendor = 490409;

        // ── Random Dungeon Bosses ─────────────────────────────────────────────
        // Universal boss roster that can replace a normal monster spawn in an
        // active Hot Dungeon or the Abandoned Mine (see DungeonBossManager).
        // Combat stats are scaled to the current level cap at spawn time; the
        // weenies below are authored at the reference difficulty.
        public const uint BossGravewalker  = 940001;
        public const uint BossEmberlord    = 940002;
        public const uint BossRendmaw      = 940003;
        public const uint BossAggregate    = 940004;
        public const uint BossWhisperer    = 940005;
    }
}
