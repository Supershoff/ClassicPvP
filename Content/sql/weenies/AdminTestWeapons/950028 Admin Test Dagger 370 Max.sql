/* Admin Test Dagger 370 Max -- clone of wcid 22440 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950028;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950028, 'ace950028-admintestdagger370max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950028,   1, 1) /* ItemType */
     , (950028,   3, 20) /* PaletteTemplate */
     , (950028,   5, 200) /* EncumbranceVal */
     , (950028,   9, 1048576) /* ValidLocations */
     , (950028,  16, 1) /* ItemUseable */
     , (950028,  19, 100) /* Value */
     , (950028,  44, 24) /* Damage */
     , (950028,  45, 3) /* DamageType */
     , (950028,  46, 2) /* DefaultCombatStyle */
     , (950028,  47, 6) /* AttackType */
     , (950028,  48, 4) /* WeaponSkill */
     , (950028,  49, 30) /* WeaponTime */
     , (950028,  51, 1) /* CombatUse */
     , (950028,  93, 1044) /* PhysicsState */
     , (950028, 105, 1) /* ItemWorkmanship */
     , (950028, 131, 64) /* MaterialType */
     , (950028, 150, 103) /* HookPlacement */
     , (950028, 151, 2) /* HookType */
     , (950028, 158, 2) /* WieldRequirements */
     , (950028, 159, 4) /* WieldSkillType */
     , (950028, 160, 370) /* WieldDifficulty */
     , (950028, 169, 101254146) /* TsysMutationData */
     , (950028, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950028,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950028,  21, 0.4) /* WeaponLength */
     , (950028,  22, 0.3) /* DamageVariance */
     , (950028,  29, 1.15) /* WeaponDefense */
     , (950028,  62, 1.15) /* WeaponOffense */
     , (950028, 149, 1.025) /* WeaponMissileDefense */
     , (950028, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950028,   1, 'Admin Test Dagger 370 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950028,   1, 0x02000E49) /* Setup */
     , (950028,   3, 0x20000014) /* SoundTable */
     , (950028,   6, 0x04000BEF) /* PaletteBase */
     , (950028,   7, 0x10000415) /* ClothingBase */
     , (950028,   8, 0x06002900) /* Icon */
     , (950028,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950028,  36, 0x0E00001D) /* MutateFilter */
     , (950028,  46, 0x38000031) /* TsysMutationFilter */;
