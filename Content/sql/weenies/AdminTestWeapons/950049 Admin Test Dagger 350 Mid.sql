/* Admin Test Dagger 350 Mid -- clone of wcid 22440 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950049;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950049, 'ace950049-admintestdagger350mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950049,   1, 1) /* ItemType */
     , (950049,   3, 20) /* PaletteTemplate */
     , (950049,   5, 200) /* EncumbranceVal */
     , (950049,   9, 1048576) /* ValidLocations */
     , (950049,  16, 1) /* ItemUseable */
     , (950049,  19, 100) /* Value */
     , (950049,  44, 19) /* Damage */
     , (950049,  45, 3) /* DamageType */
     , (950049,  46, 2) /* DefaultCombatStyle */
     , (950049,  47, 6) /* AttackType */
     , (950049,  48, 4) /* WeaponSkill */
     , (950049,  49, 34) /* WeaponTime */
     , (950049,  51, 1) /* CombatUse */
     , (950049,  93, 1044) /* PhysicsState */
     , (950049, 105, 1) /* ItemWorkmanship */
     , (950049, 131, 64) /* MaterialType */
     , (950049, 150, 103) /* HookPlacement */
     , (950049, 151, 2) /* HookType */
     , (950049, 158, 2) /* WieldRequirements */
     , (950049, 159, 4) /* WieldSkillType */
     , (950049, 160, 350) /* WieldDifficulty */
     , (950049, 169, 101254146) /* TsysMutationData */
     , (950049, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950049,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950049,  21, 0.4) /* WeaponLength */
     , (950049,  22, 0.6) /* DamageVariance */
     , (950049,  29, 1.11) /* WeaponDefense */
     , (950049,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950049,   1, 'Admin Test Dagger 350 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950049,   1, 0x02000E49) /* Setup */
     , (950049,   3, 0x20000014) /* SoundTable */
     , (950049,   6, 0x04000BEF) /* PaletteBase */
     , (950049,   7, 0x10000415) /* ClothingBase */
     , (950049,   8, 0x06002900) /* Icon */
     , (950049,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950049,  36, 0x0E00001D) /* MutateFilter */
     , (950049,  46, 0x38000031) /* TsysMutationFilter */;
