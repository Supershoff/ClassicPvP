/* Admin Test Dagger 400 Mid -- clone of wcid 22440 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950009;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950009, 'ace950009-admintestdagger400mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950009,   1, 1) /* ItemType */
     , (950009,   3, 20) /* PaletteTemplate */
     , (950009,   5, 200) /* EncumbranceVal */
     , (950009,   9, 1048576) /* ValidLocations */
     , (950009,  16, 1) /* ItemUseable */
     , (950009,  19, 100) /* Value */
     , (950009,  44, 25) /* Damage */
     , (950009,  45, 3) /* DamageType */
     , (950009,  46, 2) /* DefaultCombatStyle */
     , (950009,  47, 6) /* AttackType */
     , (950009,  48, 4) /* WeaponSkill */
     , (950009,  49, 34) /* WeaponTime */
     , (950009,  51, 1) /* CombatUse */
     , (950009,  93, 1044) /* PhysicsState */
     , (950009, 105, 1) /* ItemWorkmanship */
     , (950009, 131, 64) /* MaterialType */
     , (950009, 150, 103) /* HookPlacement */
     , (950009, 151, 2) /* HookType */
     , (950009, 158, 2) /* WieldRequirements */
     , (950009, 159, 4) /* WieldSkillType */
     , (950009, 160, 400) /* WieldDifficulty */
     , (950009, 169, 101254146) /* TsysMutationData */
     , (950009, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950009,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950009,  21, 0.4) /* WeaponLength */
     , (950009,  22, 0.6) /* DamageVariance */
     , (950009,  29, 1.11) /* WeaponDefense */
     , (950009,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950009,   1, 'Admin Test Dagger 400 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950009,   1, 0x02000E49) /* Setup */
     , (950009,   3, 0x20000014) /* SoundTable */
     , (950009,   6, 0x04000BEF) /* PaletteBase */
     , (950009,   7, 0x10000415) /* ClothingBase */
     , (950009,   8, 0x06002900) /* Icon */
     , (950009,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950009,  36, 0x0E00001D) /* MutateFilter */
     , (950009,  46, 0x38000031) /* TsysMutationFilter */;
