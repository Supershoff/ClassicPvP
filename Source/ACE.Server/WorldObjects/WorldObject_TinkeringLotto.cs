using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Managers;

using System;

namespace ACE.Server.WorldObjects
{
    partial class WorldObject
    {
        public string TinkeringLotto_Mutate(string salvageType, int salvageWorkmanship, float chanceMod = 1.0f)
        {
            var resultMessage = "";

            if (!PropertyManager.GetBool("tinker_lotto_enabled").Item)
                return "";

            switch (salvageType)
            {
                case "Steel":
                    resultMessage = TinkeringLotto_PlaySteelLottery(salvageWorkmanship, chanceMod);
                    break;

                case "Iron":
                    resultMessage = TinkeringLotto_PlayIronLottery(salvageWorkmanship, chanceMod);
                    break;

                case "Granite":
                    resultMessage = TinkeringLotto_PlayGraniteLottery(salvageWorkmanship, chanceMod);
                    break;

                case "Green Garnet":
                    resultMessage = TinkeringLotto_PlayGreenGarnetLottery(salvageWorkmanship, chanceMod);
                    break;

                case "Opal":
                    resultMessage = TinkeringLotto_PlayOpalLottery(salvageWorkmanship, chanceMod);
                    break;

                case "Mahogany":
                    resultMessage = TinkeringLotto_PlayMahoganyLottery(salvageWorkmanship, chanceMod);
                    break;

                case "Velvet":
                    resultMessage = TinkeringLotto_PlayVelvetLottery(salvageWorkmanship, chanceMod);
                    break;

                case "Brass":
                    resultMessage = TinkeringLotto_PlayBrassLottery(salvageWorkmanship, chanceMod);
                    break;

                case "Aquamarine":
                case "Black Garnet":
                case "Emerald":
                case "Imperial Topaz":
                case "Jet":
                case "Red Garnet":
                case "White Sapphire":
                    resultMessage = TinkeringLotto_PlayRendLottery(salvageWorkmanship, chanceMod);
                    break;

                case "Sunstone":
                case "Fire Opal":
                case "Black Opal":
                    resultMessage = TinkeringLotto_PlayARCSCBLottery(salvageWorkmanship, chanceMod);
                    break;

                case "Zircon":
                case "Peridot":
                case "Yellow Topaz":
                    resultMessage = TinkeringLotto_PlayDefenseImbueLottery(salvageWorkmanship, chanceMod);
                    break;
                case "Agate":
                case "Bloodstone":
                case "Carnelian":
                case "Lapis Lazuli":
                case "Smokey Quartz":
                case "Rose Quartz":
                    //Agate - Focus
                    //Bloodstone - Endurance
                    //Carnelian - Strength
                    //Lapis Lazuli - Willpower
                    //Smokey Quartz - Coord
                    //Rose Quartz - Quickness
                    resultMessage = TinkeringLotto_PlayMinorImbueLottery(salvageType, salvageWorkmanship, chanceMod);
                    break;
                case "Ebony":
                case "Porcelain":
                case "Teak":
                case "Silk":
                    resultMessage = TinkeringLotto_PlayActivationRemovalLottery(salvageType, salvageWorkmanship, chanceMod);
                    break;
            /*

Red Jade - Health Gain

Ebony - Heritage to Gharu
Porcelain - Sho
Teak - Aluvian

Hematite
Moonstone - Increases items mana
Pine - 


Silk - Removes rank
             */

                default:
                    return "";
            }

            return resultMessage;
        }

        private string TinkeringLotto_PlaySteelLottery(int salvageWorkmanship, float chanceMod = 1.0f)
        {
            string resultMsg = "";

            Random rand = new Random();
            var roll = rand.NextDouble();

            var currentLottoAlBonus = GetCumulativeArmorLevelLottoBonus();

            if (currentLottoAlBonus < 20)
            {
                if (roll < 0.025)
                {
                    //Add 10 AL
                    var alBonus = currentLottoAlBonus <= 10 ? 10 : 20 - currentLottoAlBonus;
                    this.ArmorLevel += alBonus;
                    resultMsg = $"Jackpot! Improved Armor Level by {alBonus}";
                    HandleTinkerLottoLog($"AL+{alBonus}");
                }
                else if (roll < 0.075)
                {
                    //Add between 1 - 5 AL
                    var alBonus = rand.Next(1, 6);
                    if(currentLottoAlBonus + alBonus > 20) alBonus = 20 - currentLottoAlBonus;
                    this.ArmorLevel += alBonus;                    
                    resultMsg = $"Improved Armor Level by {alBonus}";
                    HandleTinkerLottoLog($"AL+{alBonus}");
                }
            }

            //Roll for Creature Resist and Creature Slayer ratings            
            if (this.GearCreatureResistType == 0)
            {
                roll = rand.NextDouble();
                if (roll < 0.05)
                {
                    string gearCreatureResult = TinkeringLotto_ApplyGearCreatureResistMutation();
                    if (!string.IsNullOrEmpty(gearCreatureResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? gearCreatureResult : $"{resultMsg}\n{gearCreatureResult}";
                }
            }

            if (this.GearCreatureSlayerType == 0)
            {
                roll = rand.NextDouble();
                if (roll < 0.05)
                {
                    string gearCreatureResult = TinkeringLotto_ApplyGearCreatureSlayerMutation();
                    if (!string.IsNullOrEmpty(gearCreatureResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? gearCreatureResult : $"{resultMsg}\n{gearCreatureResult}";
                }
            }

            return resultMsg;
        }

        private string TinkeringLotto_PlayIronLottery(int salvageWorkmanship, float chanceMod = 1.0f)
        {
            string resultMsg = "";

            Random rand = new Random();
            var dmgBonusRoll = rand.NextDouble();

            //Capped at 1 dmg bonus for any given item
            var lottoDmgBonusCount = GetCountDmgLottoBonus();
            if (dmgBonusRoll < 0.05 && lottoDmgBonusCount < 1)
            {
                //Add +1 dmg
                this.Damage += 1;
                resultMsg = "Improved Damage by 1";
                HandleTinkerLottoLog("Dmg1");
            }

            var slayerRoll = rand.NextDouble();
            if (slayerRoll < 0.025)
            {
                if (!this.SlayerCreatureType.HasValue)
                {
                    var slayerResult = TinkeringLotto_ApplySlayerMutation();
                    if (!string.IsNullOrEmpty(slayerResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? slayerResult : $"{resultMsg}\n{slayerResult}";
                }
            }

            //var rendRoll = rand.NextDouble();
            //if (rendRoll < 0.025)
            //{
            //    var rendResult = TinkeringLotto_ApplyResistanceCleaveMutation();
            //    if (!string.IsNullOrEmpty(rendResult))
            //        resultMsg = string.IsNullOrEmpty(resultMsg) ? rendResult : $"{resultMsg}\n{rendResult}";
            //}

            return resultMsg;
        }

        private string TinkeringLotto_PlayGraniteLottery(int salvageWorkmanship, float chanceMod = 1.0f)
        {
            string resultMsg = "";

            Random rand = new Random();
            var varianceBonusroll = rand.NextDouble();

            //Capped at 1 dmg bonuses for any given item
            var lottoDmgBonusCount = GetCountDmgLottoBonus();
            if (varianceBonusroll < 0.05 && lottoDmgBonusCount < 1)
            {
                //Add +1 variance
                this.DamageVariance *= 0.8f;
                resultMsg = "Improved Damage Variance by 20%";
                HandleTinkerLottoLog("DmgVar20%");
            }

            var slayerRoll = rand.NextDouble();
            if (slayerRoll < 0.025)
            {
                if (!this.SlayerCreatureType.HasValue)
                {
                    var slayerResult = TinkeringLotto_ApplySlayerMutation();
                    if (!string.IsNullOrEmpty(slayerResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? slayerResult : $"{resultMsg}\n{slayerResult}";
                }
            }

            //var rendRoll = rand.NextDouble();
            //if (rendRoll < 0.025)
            //{
            //    var rendResult = TinkeringLotto_ApplyResistanceCleaveMutation();
            //    if (!string.IsNullOrEmpty(rendResult))
            //        resultMsg = string.IsNullOrEmpty(resultMsg) ? rendResult : $"{resultMsg}\n{rendResult}";
            //}

            return resultMsg;
        }

        private string TinkeringLotto_PlayGreenGarnetLottery(int salvageWorkmanship, float chanceMod = 1.0f)
        {
            string resultMsg = "";

            Random rand = new Random();
            var dmgBonusroll = rand.NextDouble();

            //Capped at 1 dmg bonuses for any given item
            var lottoDmgBonusCount = GetCountDmgLottoBonus();
            if (dmgBonusroll < 0.05 && lottoDmgBonusCount < 1)
            {
                //Add +1% dmg bonus
                this.ElementalDamageMod = (this.ElementalDamageMod ?? 0.0f) + 0.01f;
                resultMsg = "Improved Elemental Damage Bonus by 1% vs Monsters and 0.5% against Players";
                HandleTinkerLottoLog("CasterDmgBonus1%");
            }

            var slayerRoll = rand.NextDouble();
            if (slayerRoll < 0.025)
            {
                if (!this.SlayerCreatureType.HasValue)
                {
                    var slayerResult = TinkeringLotto_ApplySlayerMutation();
                    if (!string.IsNullOrEmpty(slayerResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? slayerResult : $"{resultMsg}\n{slayerResult}";
                }
            }

            //var rendRoll = rand.NextDouble();
            //if (rendRoll < 0.025)
            //{
            //    var rendResult = TinkeringLotto_ApplyResistanceCleaveMutation();
            //    if (!string.IsNullOrEmpty(rendResult))
            //        resultMsg = string.IsNullOrEmpty(resultMsg) ? rendResult : $"{resultMsg}\n{rendResult}";
            //}

            return resultMsg;
        }

        private string TinkeringLotto_PlayOpalLottery(int salvageWorkmanship, float chanceMod = 1.0f)
        {
            string resultMsg = "";

            Random rand = new Random();
            var manaConvRoll = rand.NextDouble();

            //Capped at 5 bonuses for any given item
            var manacCount = GetCountManaConvLottoBonus();
            if (manaConvRoll < 0.1 && manacCount < 5)
            {
                //Add +1% Mana C bonus
                this.ManaConversionMod = (this.ManaConversionMod ?? 0.0f) + 0.01f;
                resultMsg = "Improved Mana Conversion Mod by 1%";
                HandleTinkerLottoLog("CasterManaConvMod1%");
            }

            var dmgRoll = rand.NextDouble();

            //Capped at 1 bonus for any given item
            var dmgCount = GetCountDmgLottoBonus();
            if (dmgRoll < 0.05 && dmgCount < 1)
            {
                //Add +1% dmg bonus
                this.ElementalDamageMod = (this.ElementalDamageMod ?? 0.0f) + 0.01f;
                resultMsg = "Improved Elemental Damage Bonus by 1% vs Monsters and 0.5% against Players";
                HandleTinkerLottoLog("CasterDmgBonus1%");
            }

            var slayerRoll = rand.NextDouble();
            if (slayerRoll < 0.025)
            {
                if (!this.SlayerCreatureType.HasValue)
                {
                    var slayerResult = TinkeringLotto_ApplySlayerMutation();
                    if (!string.IsNullOrEmpty(slayerResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? slayerResult : $"{resultMsg}\n{slayerResult}";
                }
            }

            //var rendRoll = rand.NextDouble();
            //if (rendRoll < 0.025)
            //{
            //    var rendResult = TinkeringLotto_ApplyResistanceCleaveMutation();
            //    if (!string.IsNullOrEmpty(rendResult))
            //        resultMsg = string.IsNullOrEmpty(resultMsg) ? rendResult : $"{resultMsg}\n{rendResult}";
            //}

            return resultMsg;
        }

        private string TinkeringLotto_PlayMahoganyLottery(int salvageWorkmanship, float chanceMod = 1.0f)
        {
            string resultMsg = "";

            Random rand = new Random();
            var dmgBonusroll = rand.NextDouble();

            //Capped at 1 dmg bonuses for any given item
            var lottoDmgBonusCount = GetCountDmgLottoBonus();
            if (dmgBonusroll < 0.05 && lottoDmgBonusCount < 1)
            {
                //Add +4% dmg mod
                this.DamageMod += 0.04f;
                resultMsg = "Improved Missile Damage Mod by 4%";
                HandleTinkerLottoLog("MissileDmgMod4%");
            }

            var slayerRoll = rand.NextDouble();
            if (slayerRoll < 0.025)
            {
                if (!this.SlayerCreatureType.HasValue)
                {
                    var slayerResult = TinkeringLotto_ApplySlayerMutation();
                    if (!string.IsNullOrEmpty(slayerResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? slayerResult : $"{resultMsg}\n{slayerResult}";
                }
            }

            //var rendRoll = rand.NextDouble();
            //if (rendRoll < 0.025)
            //{
            //    var rendResult = TinkeringLotto_ApplyResistanceCleaveMutation();
            //    if (!string.IsNullOrEmpty(rendResult))
            //        resultMsg = string.IsNullOrEmpty(resultMsg) ? rendResult : $"{resultMsg}\n{rendResult}";
            //}

            return resultMsg;
        }

        private string TinkeringLotto_PlayVelvetLottery(int salvageWorkmanship, float chanceMod = 1.0f)
        {
            string resultMsg = "";

            Random rand = new Random();
            var dmgBonusroll = rand.NextDouble();

            //Capped at 1 dmg bonuses for any given item
            var lottoDmgBonusCount = GetCountDmgLottoBonus();
            if (dmgBonusroll < 0.05 && lottoDmgBonusCount < 1)
            {
                this.WeaponOffense += 0.01f;
                resultMsg = "Improved Attack Mod by 1%";
                HandleTinkerLottoLog("AttackMod1");
            }

            var slayerRoll = rand.NextDouble();
            if (slayerRoll < 0.025)
            {
                if (!this.SlayerCreatureType.HasValue)
                {
                    var slayerResult = TinkeringLotto_ApplySlayerMutation();
                    if (!string.IsNullOrEmpty(slayerResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? slayerResult : $"{resultMsg}\n{slayerResult}";
                }
            }

            //var rendRoll = rand.NextDouble();
            //if (rendRoll < 0.025)
            //{
            //    var rendResult = TinkeringLotto_ApplyResistanceCleaveMutation();
            //    if (!string.IsNullOrEmpty(rendResult))
            //        resultMsg = string.IsNullOrEmpty(resultMsg) ? rendResult : $"{resultMsg}\n{rendResult}";
            //}

            return resultMsg;
        }

        private string TinkeringLotto_PlayBrassLottery(int salvageWorkmanship, float chanceMod = 1.0f)
        {
            string resultMsg = "";

            Random rand = new Random();
            var dmgBonusroll = rand.NextDouble();

            //Capped at 1 dmg bonuses for any given item
            var lottoDmgBonusCount = GetCountDmgLottoBonus();
            if (dmgBonusroll < 0.05 && lottoDmgBonusCount < 1)
            {
                this.WeaponDefense += 0.01f;
                resultMsg = "Improved Melee Defence Mod by 1%";
                HandleTinkerLottoLog("MeleeDMod1");
            }

            var slayerRoll = rand.NextDouble();
            if (slayerRoll < 0.025)
            {
                if (!this.SlayerCreatureType.HasValue)
                {
                    var slayerResult = TinkeringLotto_ApplySlayerMutation();
                    if (!string.IsNullOrEmpty(slayerResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? slayerResult : $"{resultMsg}\n{slayerResult}";
                }
            }

            //var rendRoll = rand.NextDouble();
            //if (rendRoll < 0.025)
            //{
            //    var rendResult = TinkeringLotto_ApplyResistanceCleaveMutation();
            //    if (!string.IsNullOrEmpty(rendResult))
            //        resultMsg = string.IsNullOrEmpty(resultMsg) ? rendResult : $"{resultMsg}\n{rendResult}";
            //}

            return resultMsg;
        }

        private string TinkeringLotto_PlayRendLottery(int salvageWorkmanship, float chanceMod = 1.0f)
        {
            string resultMsg = "";

            Random rand = new Random();
            var roll = rand.NextDouble();

            var lottoDmgBonusCount = GetCountDmgLottoBonus();
            if (roll < 0.3 && lottoDmgBonusCount < 1)
            {
                if (this.ItemType == ItemType.MissileWeapon)
                {
                    this.DamageMod += 0.04f;
                    resultMsg = "Improved Missile Damage Mod by 4%";
                    HandleTinkerLottoLog("MissileDmgMod4%");
                }
                else if (this.ItemType == ItemType.MeleeWeapon)
                {
                    this.Damage += 1;
                    resultMsg = "Improved Damage by 1";
                    HandleTinkerLottoLog("Dmg1");
                }
                else if (this.ItemType == ItemType.Caster)
                {
                    this.ElementalDamageMod = (this.ElementalDamageMod ?? 0.0f) + 0.01f;
                    resultMsg = "Improved Elemental Damage Bonus by 1% vs Monsters and 0.5% against Players";
                    HandleTinkerLottoLog("CasterDmgBonus1%");
                }
            }

            // If you're using WS 10 salvage and your target item is <= WS 6
            if (salvageWorkmanship == 10 && this.Workmanship <= 6)
            {
                //// 15% chance for bonus effect
                //roll = rand.NextDouble();
                //if (roll < 0.15)
                //{
                //    var currRollResultMsg = "";

                //    if (this.ItemType == ItemType.MissileWeapon)
                //    {
                //        this.IgnoreShield = 1;
                //        currRollResultMsg = "Added Shield Hollow";
                //        HandleTinkerLottoLog("ShieldHollow");
                //    }
                //    else if (this.ItemType == ItemType.MeleeWeapon)
                //    {
                //        if (!this.GetProperty(PropertyInt.Cleaving).HasValue || this.GetProperty(PropertyInt.Cleaving) < 2)
                //            this.SetProperty(PropertyInt.Cleaving, 2);
                //        else
                //            this.SetProperty(PropertyInt.Cleaving, 3);

                //        currRollResultMsg = "Added +1 Cleaving Target";
                //        HandleTinkerLottoLog("Cleave1");
                //    }
                //    else if (this.ItemType == ItemType.Caster)
                //    {
                //        roll = rand.NextDouble();
                //        if (roll < 0.5)
                //        {
                //            this.SetProperty(PropertyFloat.CriticalFrequency, 0.25);
                //            currRollResultMsg = "Added Biting Strike";
                //            HandleTinkerLottoLog("BS");
                //        }
                //        else
                //        {
                //            this.SetProperty(PropertyFloat.CriticalMultiplier, 2.5);
                //            currRollResultMsg = "Added Crushing Blow";
                //            HandleTinkerLottoLog("CB");
                //        }
                //    }

                //    resultMsg = string.IsNullOrEmpty(resultMsg) ? currRollResultMsg : $"{resultMsg}\n{currRollResultMsg}";
                //}

                //// 15% chance for cast on strike
                //roll = rand.NextDouble();
                //if (roll < 0.15)
                //{
                //    var currRollResultMsg = "";

                //    this.ProcSpellRate = 0.15f;
                //    this.ProcSpellSelfTargeted = false;
                //    this.ItemSpellcraft = 525;
                //    if (this.ItemType == ItemType.Caster)
                //    {
                //        this.ProcSpell = (uint)SpellId.MagicYieldOther8;
                //        currRollResultMsg = "Added Cast on Strike Magic Yield";
                //        HandleTinkerLottoLog("COSYIELD");
                //    }
                //    else
                //    {
                //        this.ProcSpell = (uint)SpellId.ImperilOther8;
                //        currRollResultMsg = "Added Cast on Strike Imperil";
                //        HandleTinkerLottoLog("COSIMP");
                //    }

                //    resultMsg = string.IsNullOrEmpty(resultMsg) ? currRollResultMsg : $"{resultMsg}\n{currRollResultMsg}";
                //}
            }

            return resultMsg;
        }

        private string TinkeringLotto_PlayARCSCBLottery(int salvageWorkmanship, float chanceMod = 1.0f)
        {
            string resultMsg = "";

            Random rand = new Random();
            var roll = rand.NextDouble();

            var lottoDmgBonusCount = GetCountDmgLottoBonus();
            if (roll < 0.3 && lottoDmgBonusCount < 1)
            {
                if (this.ItemType == ItemType.MissileWeapon)
                {
                    this.DamageMod += 0.04f;
                    resultMsg = "Improved Missile Damage Mod by 4%";
                    HandleTinkerLottoLog("MissileDmgMod4%");
                }
                else if (this.ItemType == ItemType.MeleeWeapon)
                {
                    this.Damage += 1;
                    resultMsg = "Improved Damage by 1";
                    HandleTinkerLottoLog("Dmg1");
                }
                else if (this.ItemType == ItemType.Caster)
                {
                    this.ElementalDamageMod = (this.ElementalDamageMod ?? 0.0f) + 0.01f;
                    resultMsg = "Improved Elemental Damage Bonus by 1% vs Monsters and 0.5% against Players";
                    HandleTinkerLottoLog("CasterDmgBonus1%");
                }
            }

            if (salvageWorkmanship == 10 && this.Workmanship <= 6)
            {
                //roll = rand.NextDouble();
                //if (roll < 0.15)
                //{
                //    var currRollResultMsg = "";

                //    if (this.ItemType == ItemType.MissileWeapon)
                //    {
                //        this.IgnoreShield = 1;
                //        currRollResultMsg = "Added Shield Hollow";
                //        HandleTinkerLottoLog("ShieldHollow");
                //    }
                //    else if (this.ItemType == ItemType.MeleeWeapon)
                //    {
                //        if (!this.GetProperty(PropertyInt.Cleaving).HasValue || this.GetProperty(PropertyInt.Cleaving) < 2)
                //            this.SetProperty(PropertyInt.Cleaving, 2);
                //        else
                //            this.SetProperty(PropertyInt.Cleaving, 3);

                //        currRollResultMsg = "Added +1 Cleaving Target";
                //        HandleTinkerLottoLog("Cleave1");
                //    }
                //    else if (this.ItemType == ItemType.Caster)
                //    {
                //        if (this.WeaponMagicDefense.HasValue)
                //            this.SetProperty(PropertyFloat.WeaponMagicDefense, this.WeaponMagicDefense.Value + 0.01);
                //        else
                //            this.SetProperty(PropertyFloat.WeaponMagicDefense, 1.01);

                //        currRollResultMsg = "Added +1 Magic Defense";
                //        HandleTinkerLottoLog("MagicD1");
                //    }

                //    resultMsg = string.IsNullOrEmpty(resultMsg) ? currRollResultMsg : $"{resultMsg}\n{currRollResultMsg}";
                //}
            }

            return resultMsg;
        }

        private string TinkeringLotto_PlayDefenseImbueLottery(int salvageWorkmanship, float chanceMod = 1.0f)
        {
            string resultMsg = "";

            Random rand = new Random();
            var roll = rand.NextDouble();
            var alBonus = 0;

            if (roll < 0.3)
            {
                alBonus = rand.Next(10, 21);
                resultMsg = $"Improved Armor Level by {alBonus}";

                if (salvageWorkmanship == 10 && this.Workmanship <= 6)
                {
                    roll = rand.NextDouble();
                    if (roll < 0.15)
                    {
                        alBonus += rand.Next(3, 6);
                        resultMsg = $"Jackpot! Improved Armor Level by {alBonus}";
                    }
                }

                this.ArmorLevel += alBonus;
                HandleTinkerLottoLog($"AL+{alBonus}");
            }

            //Roll for Creature Resist and Creature Slayer ratings            
            if (this.GearCreatureResistType == 0)
            {
                roll = rand.NextDouble();
                if (roll < 0.1)
                {
                    string gearCreatureResult = TinkeringLotto_ApplyGearCreatureResistMutation();
                    if (!string.IsNullOrEmpty(gearCreatureResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? gearCreatureResult : $"{resultMsg}\n{gearCreatureResult}";
                }
            }

            if (this.GearCreatureSlayerType == 0)
            {
                roll = rand.NextDouble();
                if (roll < 0.1)
                {
                    string gearCreatureResult = TinkeringLotto_ApplyGearCreatureSlayerMutation();
                    if (!string.IsNullOrEmpty(gearCreatureResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? gearCreatureResult : $"{resultMsg}\n{gearCreatureResult}";
                }
            }

            return resultMsg;
        }

        private string TinkeringLotto_PlayMinorImbueLottery(string salvageType, int salvageWorkmanship, float chanceMod = 1.0f)
        {
            //Chance to increase mana pool
            //Chance to decrease mana burn rate
            //Chance to upgrade minor to a moderate

            string resultMsg = "";

            Random rand = new Random();
            var roll = rand.NextDouble();

            //Mana pool
            if(roll > 0.6 && this.ItemMaxMana.HasValue)
            {
                var manaBonus = Convert.ToInt32(Math.Round(roll * 1000));
                this.ItemMaxMana = this.ItemMaxMana.Value + manaBonus;
                resultMsg = $"Increased maximum mana by {manaBonus}";
                HandleTinkerLottoLog($"MaxMana+{manaBonus}");
            }

            //Mana burn rate
            if(roll < 0.4 && this.ManaRate.HasValue && this.ManaRate.Value < 0)
            {
                var bonusSeconds = Convert.ToInt32(Math.Round(roll * 100));
                var currSeconds = -1.0 / this.ManaRate.Value;
                var newSeconds = currSeconds + bonusSeconds;
                this.ManaRate = -1.0 / newSeconds;
                resultMsg = $"Decreased mana burn rate by {bonusSeconds} seconds";
                HandleTinkerLottoLog($"ManaRate+{bonusSeconds}");
            }

            //25% chance to upgrade
            //5% chance to major
            //95% chance to moderate
            roll = rand.NextDouble();
            if (roll < 0.25)
            {
                var isMajor = rand.NextDouble() < 0.05;
                string successMsg = "";
                var minorCantrips = this.MinorCantrips?.Keys;
                switch(salvageType)
                {
                    //Agate - Focus
                    //Bloodstone - Endurance
                    //Carnelian - Strength
                    //Lapis Lazuli - Willpower
                    //Smokey Quartz - Coord
                    //Rose Quartz - Quickness
                    case "Agate":
                        if(minorCantrips.Contains((int)SpellId.CANTRIPFOCUS1))
                        {
                            this.Biota.TryRemoveKnownSpell((int)SpellId.CANTRIPFOCUS1, this.BiotaDatabaseLock);
                            this.Biota.GetOrAddKnownSpell(isMajor ? (int)SpellId.CANTRIPFOCUS2 : 2661, this.BiotaDatabaseLock, out _);
                            successMsg = $"Upgraded spell Minor Focus to {(isMajor ? "Major" : "Moderate")} Focus";
                            resultMsg = string.IsNullOrEmpty(resultMsg) ? successMsg : $"{resultMsg}\n{successMsg}";
                        }
                        break;
                    case "Bloodstone":
                        if (minorCantrips.Contains((int)SpellId.CANTRIPENDURANCE1))
                        {
                            this.Biota.TryRemoveKnownSpell((int)SpellId.CANTRIPENDURANCE1, this.BiotaDatabaseLock);
                            this.Biota.GetOrAddKnownSpell(isMajor ? (int)SpellId.CANTRIPENDURANCE2 : 2660, this.BiotaDatabaseLock, out _);
                            successMsg = $"Upgraded spell Minor Endurance to {(isMajor ? "Major" : "Moderate")} Endurance";
                            resultMsg = string.IsNullOrEmpty(resultMsg) ? successMsg : $"{resultMsg}\n{successMsg}";
                        }
                        break;
                    case "Carnelian":
                        if (minorCantrips.Contains((int)SpellId.CANTRIPSTRENGTH1))
                        {
                            this.Biota.TryRemoveKnownSpell((int)SpellId.CANTRIPSTRENGTH1, this.BiotaDatabaseLock);
                            this.Biota.GetOrAddKnownSpell(isMajor ? (int)SpellId.CANTRIPSTRENGTH2 : 2663, this.BiotaDatabaseLock, out _);
                            successMsg = $"Upgraded spell Minor Strength to {(isMajor ? "Major" : "Moderate")} Strength";
                            resultMsg = string.IsNullOrEmpty(resultMsg) ? successMsg : $"{resultMsg}\n{successMsg}";
                        }
                        break;
                    case "Lapis Lazuli":
                        if (minorCantrips.Contains((int)SpellId.CANTRIPWILLPOWER1))
                        {
                            this.Biota.TryRemoveKnownSpell((int)SpellId.CANTRIPWILLPOWER1, this.BiotaDatabaseLock);
                            this.Biota.GetOrAddKnownSpell(isMajor ? (int)SpellId.CANTRIPWILLPOWER2 : 2664, this.BiotaDatabaseLock, out _);
                            successMsg = $"Upgraded spell Minor Willpower to {(isMajor ? "Major" : "Moderate")} Willpower";
                            resultMsg = string.IsNullOrEmpty(resultMsg) ? successMsg : $"{resultMsg}\n{successMsg}";
                        }
                        break;
                    case "Smokey Quartz":
                        if (minorCantrips.Contains((int)SpellId.CANTRIPCOORDINATION1))
                        {
                            this.Biota.TryRemoveKnownSpell((int)SpellId.CANTRIPCOORDINATION1, this.BiotaDatabaseLock);
                            this.Biota.GetOrAddKnownSpell(isMajor ? (int)SpellId.CANTRIPCOORDINATION2 : 2659, this.BiotaDatabaseLock, out _);
                            successMsg = $"Upgraded spell Minor Coordination to {(isMajor ? "Major" : "Moderate")} Coordination";
                            resultMsg = string.IsNullOrEmpty(resultMsg) ? successMsg : $"{resultMsg}\n{successMsg}";
                        }
                        break;
                    case "Rose Quartz":
                        if (minorCantrips.Contains((int)SpellId.CANTRIPQUICKNESS1))
                        {
                            this.Biota.TryRemoveKnownSpell((int)SpellId.CANTRIPQUICKNESS1, this.BiotaDatabaseLock);
                            this.Biota.GetOrAddKnownSpell(isMajor ? (int)SpellId.CANTRIPQUICKNESS2 : 2662, this.BiotaDatabaseLock, out _);
                            successMsg = $"Upgraded spell Minor Quickness to {(isMajor ? "Major" : "Moderate")} Quickness";
                            resultMsg = string.IsNullOrEmpty(resultMsg) ? successMsg : $"{resultMsg}\n{successMsg}";
                        }
                        break;
                    default:
                        break;
                }
            }

            //Roll for Creature Resist and Creature Slayer ratings            
            if (this.GearCreatureResistType == 0)
            {
                roll = rand.NextDouble();
                if (roll < 0.1)
                {
                    string gearCreatureResult = TinkeringLotto_ApplyGearCreatureResistMutation();
                    if (!string.IsNullOrEmpty(gearCreatureResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? gearCreatureResult : $"{resultMsg}\n{gearCreatureResult}";
                }
            }

            if (this.GearCreatureSlayerType == 0)
            {
                roll = rand.NextDouble();
                if (roll < 0.1)
                {
                    string gearCreatureResult = TinkeringLotto_ApplyGearCreatureSlayerMutation();
                    if (!string.IsNullOrEmpty(gearCreatureResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? gearCreatureResult : $"{resultMsg}\n{gearCreatureResult}";
                }
            }

            return resultMsg;
        }
        
        private string TinkeringLotto_PlayActivationRemovalLottery(string salvageType, int salvageWorkmanship, float chanceMod = 1.0f)
        {
            string resultMsg = "";

            Random rand = new Random();
            var roll = rand.NextDouble();
            var alBonus = 0;

            if (roll < 0.5 && this.ArmorLevel > 0)
            {
                alBonus = rand.Next(10, 21);
                resultMsg = $"Improved Armor Level by {alBonus}";

                if (salvageWorkmanship == 10 && this.Workmanship <= 6)
                {
                    roll = rand.NextDouble();
                    if (roll < 0.15)
                    {
                        alBonus += rand.Next(10, 21);
                        resultMsg = $"Jackpot! Improved Armor Level by {alBonus}";
                    }
                }

                this.ArmorLevel += alBonus;
                HandleTinkerLottoLog($"AL+{alBonus}");
            }

            //Roll for Creature Resist and Creature Slayer ratings            
            if (this.GearCreatureResistType == 0)
            {
                roll = rand.NextDouble();
                if (roll < 0.1)
                {
                    string gearCreatureResult = TinkeringLotto_ApplyGearCreatureResistMutation();
                    if (!string.IsNullOrEmpty(gearCreatureResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? gearCreatureResult : $"{resultMsg}\n{gearCreatureResult}";
                }
            }

            if (this.GearCreatureSlayerType == 0)
            {
                roll = rand.NextDouble();
                if (roll < 0.1)
                {
                    string gearCreatureResult = TinkeringLotto_ApplyGearCreatureSlayerMutation();
                    if (!string.IsNullOrEmpty(gearCreatureResult))
                        resultMsg = string.IsNullOrEmpty(resultMsg) ? gearCreatureResult : $"{resultMsg}\n{gearCreatureResult}";
                }
            }

            return resultMsg;
        }

        private string TinkeringLotto_ApplySlayerMutation()
        {
            ApplyRandomSlayer(1.5);
            HandleTinkerLottoLog($"{this.SlayerCreatureType}Slayer");
            return $"Added {this.SlayerCreatureType} slayer";
        }

        private string TinkeringLotto_ApplyResistanceCleaveMutation()
        {
            string resultMsg = "";

            if (this.ResistanceModifier >= 1.5)
                return "";

            switch (this.W_DamageType)
            {
                case DamageType.Acid:
                    this.ResistanceModifierType = this.W_DamageType;
                    this.ResistanceModifier = 1.5;
                    resultMsg = "Added Acid Cleaving";
                    HandleTinkerLottoLog("AcidCleaving");
                    break;

                case DamageType.Cold:
                    this.ResistanceModifierType = this.W_DamageType;
                    this.ResistanceModifier = 1.5;
                    resultMsg = "Added Cold Cleaving";
                    HandleTinkerLottoLog("ColdCleaving");
                    break;

                case DamageType.Electric:
                    this.ResistanceModifierType = this.W_DamageType;
                    this.ResistanceModifier = 1.5;
                    resultMsg = "Added Electric Cleaving";
                    HandleTinkerLottoLog("ElectricCleaving");
                    break;

                case DamageType.Fire:
                    this.ResistanceModifierType = this.W_DamageType;
                    this.ResistanceModifier = 1.5;
                    resultMsg = "Added Fire Cleaving";
                    HandleTinkerLottoLog("FireCleaving");
                    break;

                case DamageType.Pierce:
                    this.ResistanceModifierType = this.W_DamageType;
                    this.ResistanceModifier = 1.5;
                    resultMsg = "Added Pierce Cleaving";
                    HandleTinkerLottoLog("PierceCleaving");
                    break;

                case DamageType.Slash:
                    this.ResistanceModifierType = this.W_DamageType;
                    this.ResistanceModifier = 1.5;
                    resultMsg = "Added Slash Cleaving";
                    HandleTinkerLottoLog("SlashCleaving");
                    break;

                case DamageType.Bludgeon:
                    this.ResistanceModifierType = this.W_DamageType;
                    this.ResistanceModifier = 1.5;
                    resultMsg = "Added Bludgeon Cleaving";
                    HandleTinkerLottoLog("BludgeonCleaving");
                    break;

                case DamageType.Pierce | DamageType.Slash:
                    this.ResistanceModifierType = new Random().Next(2) == 0 ? DamageType.Pierce : DamageType.Slash;
                    this.ResistanceModifier = 1.5;
                    resultMsg = $"Added {ResistanceModifierType} Cleaving";
                    HandleTinkerLottoLog("PierceSlashCleaving");
                    break;
            }

            return resultMsg;
        }

        private string TinkeringLotto_ApplyGearCreatureSlayerMutation()
        {
            ApplyRandomGearCreatureSlayerRating();
            HandleTinkerLottoLog($"{this.GearCreatureSlayerType}GearSlayer_{this.GearCreatureSlayerRating}");
            return $"Added {this.GearCreatureSlayerType} Creature Slayer Rating {this.GearCreatureSlayerRating}";
        }

        private string TinkeringLotto_ApplyGearCreatureResistMutation()
        {
            ApplyRandomGearCreatureResistRating();
            HandleTinkerLottoLog($"{this.GearCreatureResistType}GearResist_{this.GearCreatureResistRating}");
            return $"Added {this.GearCreatureResistType} Creature Resist Rating {this.GearCreatureResistRating}";
        }

        public void HandleTinkerLottoLog(string lottoResult)
        {
            if (!string.IsNullOrEmpty(this.TinkerLottoLog))
                this.TinkerLottoLog += ",";

            this.TinkerLottoLog += lottoResult;
        }

        private int GetCumulativeArmorLevelLottoBonus()
        {
            if (string.IsNullOrEmpty(this.TinkerLottoLog))
                return 0;

            var alBonus = 0;

            var lottoEvents = this.TinkerLottoLog.Split(',');

            foreach (var lottoEvent in lottoEvents)
            {
                if (lottoEvent.StartsWith("AL+"))
                {
                    var eventAlString = lottoEvent.Substring(3);
                    if (int.TryParse(eventAlString, out int eventAlAmount))
                        alBonus += eventAlAmount;
                }
            }

            return alBonus;
        }

        private int GetCountDmgLottoBonus()
        {
            if (string.IsNullOrEmpty(this.TinkerLottoLog))
                return 0;

            var dmgBonusCount = 0;

            var lottoEvents = this.TinkerLottoLog.Split(',');

            foreach (var lottoEvent in lottoEvents)
            {
                if (lottoEvent.Contains("Dmg") || lottoEvent.Contains("AttackMod1") || lottoEvent.Contains("MeleeDMod1"))
                    dmgBonusCount++;
            }

            return dmgBonusCount;
        }

        private int GetCountManaConvLottoBonus()
        {
            if (string.IsNullOrEmpty(this.TinkerLottoLog))
                return 0;

            var bonusCount = 0;

            var lottoEvents = this.TinkerLottoLog.Split(',');

            foreach (var lottoEvent in lottoEvents)
            {
                if (lottoEvent.Contains("ManaConvMod"))
                    bonusCount++;
            }

            return bonusCount;
        }
    }
}
