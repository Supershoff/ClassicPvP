/* Admin Test Sword 350 Max -- clone of wcid 350 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950040;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950040, 'ace950040-admintestsword350max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950040,   1, 1) /* ItemType */
     , (950040,   3, 20) /* PaletteTemplate */
     , (950040,   5, 550) /* EncumbranceVal */
     , (950040,   8, 220) /* Mass */
     , (950040,   9, 1048576) /* ValidLocations */
     , (950040,  16, 1) /* ItemUseable */
     , (950040,  19, 340) /* Value */
     , (950040,  44, 45) /* Damage */
     , (950040,  45, 3) /* DamageType */
     , (950040,  46, 2) /* DefaultCombatStyle */
     , (950040,  47, 6) /* AttackType */
     , (950040,  48, 11) /* WeaponSkill */
     , (950040,  49, 37) /* WeaponTime */
     , (950040,  51, 1) /* CombatUse */
     , (950040,  93, 1044) /* PhysicsState */
     , (950040, 105, 1) /* ItemWorkmanship */
     , (950040, 131, 64) /* MaterialType */
     , (950040, 150, 103) /* HookPlacement */
     , (950040, 151, 2) /* HookType */
     , (950040, 158, 2) /* WieldRequirements */
     , (950040, 159, 11) /* WieldSkillType */
     , (950040, 160, 350) /* WieldDifficulty */
     , (950040, 169, 101255170) /* TsysMutationData */
     , (950040, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950040,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950040,  21, 0.95) /* WeaponLength */
     , (950040,  22, 0.4) /* DamageVariance */
     , (950040,  29, 1.15) /* WeaponDefense */
     , (950040,  39, 1.1) /* DefaultScale */
     , (950040,  62, 1.15) /* WeaponOffense */
     , (950040, 149, 1.025) /* WeaponMissileDefense */
     , (950040, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950040,   1, 'Admin Test Sword 350 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950040,   1, 0x02000146) /* Setup */
     , (950040,   3, 0x20000014) /* SoundTable */
     , (950040,   6, 0x04000BEF) /* PaletteBase */
     , (950040,   7, 0x1000013A) /* ClothingBase */
     , (950040,   8, 0x06001657) /* Icon */
     , (950040,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950040,  36, 0x0E00001D) /* MutateFilter */
     , (950040,  46, 0x38000005) /* TsysMutationFilter */;
