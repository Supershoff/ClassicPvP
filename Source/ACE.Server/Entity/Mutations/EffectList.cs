using System.Collections.Generic;

using ACE.Server.WorldObjects;

namespace ACE.Server.Entity.Mutations
{
    public class EffectList
    {
        public float Chance;
        public List<Effect> Effects = new List<Effect>();

        /// <summary>
        /// Parse-time metadata: the WieldDifficulty this outcome assigns, if any.
        /// Outcomes in the weapon damage scripts are ordered ascending by power and grouped
        /// into contiguous WieldDifficulty bands; this lets a caller bias toward the top of
        /// the band it rolled into without pushing the item into a higher wield requirement.
        /// </summary>
        public int? WieldDifficulty;

        /// <summary>
        /// Parse-time metadata: the DamageVariance this outcome assigns, if any.
        /// Lower is better (a tighter damage range), so biasing toward the minimum improves the roll.
        /// </summary>
        public float? DamageVariance;

        public bool TryMutate(WorldObject wo)
        {
            var mutated = false;

            foreach (var effect in Effects)
                mutated |= effect.TryMutate(wo);      // stop completely on failure?

            return mutated;
        }
    }
}
