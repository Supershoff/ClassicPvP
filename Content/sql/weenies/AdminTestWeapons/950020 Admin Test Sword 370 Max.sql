/* Admin Test Sword 370 Max -- clone of wcid 350 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950020;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950020, 'ace950020-admintestsword370max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950020,   1, 1) /* ItemType */
     , (950020,   3, 20) /* PaletteTemplate */
     , (950020,   5, 550) /* EncumbranceVal */
     , (950020,   8, 220) /* Mass */
     , (950020,   9, 1048576) /* ValidLocations */
     , (950020,  16, 1) /* ItemUseable */
     , (950020,  19, 340) /* Value */
     , (950020,  44, 50) /* Damage */
     , (950020,  45, 3) /* DamageType */
     , (950020,  46, 2) /* DefaultCombatStyle */
     , (950020,  47, 6) /* AttackType */
     , (950020,  48, 11) /* WeaponSkill */
     , (950020,  49, 37) /* WeaponTime */
     , (950020,  51, 1) /* CombatUse */
     , (950020,  93, 1044) /* PhysicsState */
     , (950020, 105, 1) /* ItemWorkmanship */
     , (950020, 131, 64) /* MaterialType */
     , (950020, 150, 103) /* HookPlacement */
     , (950020, 151, 2) /* HookType */
     , (950020, 158, 2) /* WieldRequirements */
     , (950020, 159, 11) /* WieldSkillType */
     , (950020, 160, 370) /* WieldDifficulty */
     , (950020, 169, 101255170) /* TsysMutationData */
     , (950020, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950020,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950020,  21, 0.95) /* WeaponLength */
     , (950020,  22, 0.4) /* DamageVariance */
     , (950020,  29, 1.15) /* WeaponDefense */
     , (950020,  39, 1.1) /* DefaultScale */
     , (950020,  62, 1.15) /* WeaponOffense */
     , (950020, 149, 1.025) /* WeaponMissileDefense */
     , (950020, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950020,   1, 'Admin Test Sword 370 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950020,   1, 0x02000146) /* Setup */
     , (950020,   3, 0x20000014) /* SoundTable */
     , (950020,   6, 0x04000BEF) /* PaletteBase */
     , (950020,   7, 0x1000013A) /* ClothingBase */
     , (950020,   8, 0x06001657) /* Icon */
     , (950020,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950020,  36, 0x0E00001D) /* MutateFilter */
     , (950020,  46, 0x38000005) /* TsysMutationFilter */;
