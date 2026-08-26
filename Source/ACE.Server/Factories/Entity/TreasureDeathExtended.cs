using ACE.Database.Models.World;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Entity
{
    public class TreasureDeathExtended : TreasureDeath
    {
        public double ExtendedTier { get; set; }
        public TreasureItemType_Orig ForceTreasureItemType { get; set; }
        public TreasureArmorType ForceArmorType { get; set; }
        public TreasureWeaponType ForceWeaponType { get; set; }
        public TreasureHeritageGroup ForceHeritage { get; set; }
        public bool ForContainer { get; set; }

        /// <summary>
        /// Set when the loot is rolling for a creature that died inside an active Hot Dungeon.
        /// Biases weapon damage/variance rolls and the item type mix (see LootGenerationFactory).
        /// </summary>
        public bool IsHotDungeon { get; set; }

        public bool AllowSpecialProperties = true;

        public TreasureDeathExtended()
        {
        }

        public TreasureDeathExtended(TreasureDeathExtended other) : base(other)
        {
            ForceTreasureItemType = other.ForceTreasureItemType;
            ForceArmorType = other.ForceArmorType;
            ForceWeaponType = other.ForceWeaponType;
            ForceHeritage = other.ForceHeritage;
            ForContainer = other.ForContainer;
            IsHotDungeon = other.IsHotDungeon;
            AllowSpecialProperties = other.AllowSpecialProperties;
        }

        public TreasureDeathExtended(TreasureDeath other) : base(other)
        {
        }
    }
}
