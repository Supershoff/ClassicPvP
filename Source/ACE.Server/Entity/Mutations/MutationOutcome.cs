using System.Collections.Generic;

using ACE.Common;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity.Mutations
{
    public class MutationOutcome
    {
        public List<EffectList> EffectLists = new List<EffectList>();

        public bool TryMutate(WorldObject wo, double rng, float qualityBias = 0.0f)
        {
            for (var i = 0; i < EffectLists.Count; i++)
            {
                if (rng < EffectLists[i].Chance)
                {
                    var idx = qualityBias > 0.0f ? BiasRoll(i, qualityBias) : i;

                    return EffectLists[idx].TryMutate(wo);
                }
            }
            return false;
        }

        /// <summary>
        /// Upgrades an outcome that has already been rolled, with qualityBias as the probability
        /// of the upgrade landing. Never downgrades, and never moves the roll out of the
        /// WieldDifficulty band it landed in.
        /// </summary>
        private int BiasRoll(int rolled, float qualityBias)
        {
            if (ThreadSafeRandom.Next(0.0f, 1.0f) >= qualityBias)
                return rolled;

            // damage variance: lower is better, and it is not wield-banded
            if (EffectLists[rolled].DamageVariance.HasValue)
                return GetBestDamageVariance(rolled);

            // everything else in these scripts (Damage, DamageMod, ElementalDamageBonus) is
            // ordered ascending by power within a contiguous WieldDifficulty band
            return GetTopOfWieldBand(rolled);
        }

        private int GetBestDamageVariance(int rolled)
        {
            var best = rolled;

            for (var i = 0; i < EffectLists.Count; i++)
            {
                var variance = EffectLists[i].DamageVariance;

                if (variance.HasValue && variance.Value < EffectLists[best].DamageVariance.Value)
                    best = i;
            }
            return best;
        }

        private int GetTopOfWieldBand(int rolled)
        {
            var band = EffectLists[rolled].WieldDifficulty;
            var top = rolled;

            for (var i = rolled + 1; i < EffectLists.Count; i++)
            {
                if (EffectLists[i].WieldDifficulty != band)
                    break;

                top = i;
            }
            return top;
        }
    }
}
