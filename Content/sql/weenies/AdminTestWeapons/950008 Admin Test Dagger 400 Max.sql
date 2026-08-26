/* Admin Test Dagger 400 Max -- clone of wcid 22440 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950008;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950008, 'ace950008-admintestdagger400max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950008,   1, 1) /* ItemType */
     , (950008,   3, 20) /* PaletteTemplate */
     , (950008,   5, 200) /* EncumbranceVal */
     , (950008,   9, 1048576) /* ValidLocations */
     , (950008,  16, 1) /* ItemUseable */
     , (950008,  19, 100) /* Value */
     , (950008,  44, 26) /* Damage */
     , (950008,  45, 3) /* DamageType */
     , (950008,  46, 2) /* DefaultCombatStyle */
     , (950008,  47, 6) /* AttackType */
     , (950008,  48, 4) /* WeaponSkill */
     , (950008,  49, 30) /* WeaponTime */
     , (950008,  51, 1) /* CombatUse */
     , (950008,  93, 1044) /* PhysicsState */
     , (950008, 105, 1) /* ItemWorkmanship */
     , (950008, 131, 64) /* MaterialType */
     , (950008, 150, 103) /* HookPlacement */
     , (950008, 151, 2) /* HookType */
     , (950008, 158, 2) /* WieldRequirements */
     , (950008, 159, 4) /* WieldSkillType */
     , (950008, 160, 400) /* WieldDifficulty */
     , (950008, 169, 101254146) /* TsysMutationData */
     , (950008, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950008,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950008,  21, 0.4) /* WeaponLength */
     , (950008,  22, 0.3) /* DamageVariance */
     , (950008,  29, 1.15) /* WeaponDefense */
     , (950008,  62, 1.15) /* WeaponOffense */
     , (950008, 149, 1.025) /* WeaponMissileDefense */
     , (950008, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950008,   1, 'Admin Test Dagger 400 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950008,   1, 0x02000E49) /* Setup */
     , (950008,   3, 0x20000014) /* SoundTable */
     , (950008,   6, 0x04000BEF) /* PaletteBase */
     , (950008,   7, 0x10000415) /* ClothingBase */
     , (950008,   8, 0x06002900) /* Icon */
     , (950008,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950008,  36, 0x0E00001D) /* MutateFilter */
     , (950008,  46, 0x38000031) /* TsysMutationFilter */;
