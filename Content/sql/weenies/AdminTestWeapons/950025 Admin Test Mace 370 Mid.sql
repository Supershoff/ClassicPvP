/* Admin Test Mace 370 Mid -- clone of wcid 331 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950025;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950025, 'ace950025-admintestmace370mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950025,   1, 1) /* ItemType */
     , (950025,   3, 20) /* PaletteTemplate */
     , (950025,   5, 675) /* EncumbranceVal */
     , (950025,   8, 450) /* Mass */
     , (950025,   9, 1048576) /* ValidLocations */
     , (950025,  16, 1) /* ItemUseable */
     , (950025,  19, 260) /* Value */
     , (950025,  44, 37) /* Damage */
     , (950025,  45, 4) /* DamageType */
     , (950025,  46, 2) /* DefaultCombatStyle */
     , (950025,  47, 4) /* AttackType */
     , (950025,  48, 5) /* WeaponSkill */
     , (950025,  49, 34) /* WeaponTime */
     , (950025,  51, 1) /* CombatUse */
     , (950025,  93, 1044) /* PhysicsState */
     , (950025, 105, 1) /* ItemWorkmanship */
     , (950025, 131, 64) /* MaterialType */
     , (950025, 150, 103) /* HookPlacement */
     , (950025, 151, 2) /* HookType */
     , (950025, 158, 2) /* WieldRequirements */
     , (950025, 159, 5) /* WieldSkillType */
     , (950025, 160, 370) /* WieldDifficulty */
     , (950025, 169, 101189386) /* TsysMutationData */
     , (950025, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950025,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950025,  21, 0.62) /* WeaponLength */
     , (950025,  22, 0.4) /* DamageVariance */
     , (950025,  29, 1.11) /* WeaponDefense */
     , (950025,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950025,   1, 'Admin Test Mace 370 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950025,   1, 0x0200013A) /* Setup */
     , (950025,   3, 0x20000014) /* SoundTable */
     , (950025,   6, 0x04000BEF) /* PaletteBase */
     , (950025,   7, 0x10000150) /* ClothingBase */
     , (950025,   8, 0x0600161B) /* Icon */
     , (950025,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950025,  36, 0x0E00001D) /* MutateFilter */
     , (950025,  46, 0x38000003) /* TsysMutationFilter */;
