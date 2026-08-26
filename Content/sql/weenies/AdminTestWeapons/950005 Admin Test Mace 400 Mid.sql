/* Admin Test Mace 400 Mid -- clone of wcid 331 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950005;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950005, 'ace950005-admintestmace400mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950005,   1, 1) /* ItemType */
     , (950005,   3, 20) /* PaletteTemplate */
     , (950005,   5, 675) /* EncumbranceVal */
     , (950005,   8, 450) /* Mass */
     , (950005,   9, 1048576) /* ValidLocations */
     , (950005,  16, 1) /* ItemUseable */
     , (950005,  19, 260) /* Value */
     , (950005,  44, 40) /* Damage */
     , (950005,  45, 4) /* DamageType */
     , (950005,  46, 2) /* DefaultCombatStyle */
     , (950005,  47, 4) /* AttackType */
     , (950005,  48, 5) /* WeaponSkill */
     , (950005,  49, 34) /* WeaponTime */
     , (950005,  51, 1) /* CombatUse */
     , (950005,  93, 1044) /* PhysicsState */
     , (950005, 105, 1) /* ItemWorkmanship */
     , (950005, 131, 64) /* MaterialType */
     , (950005, 150, 103) /* HookPlacement */
     , (950005, 151, 2) /* HookType */
     , (950005, 158, 2) /* WieldRequirements */
     , (950005, 159, 5) /* WieldSkillType */
     , (950005, 160, 400) /* WieldDifficulty */
     , (950005, 169, 101189386) /* TsysMutationData */
     , (950005, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950005,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950005,  21, 0.62) /* WeaponLength */
     , (950005,  22, 0.4) /* DamageVariance */
     , (950005,  29, 1.11) /* WeaponDefense */
     , (950005,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950005,   1, 'Admin Test Mace 400 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950005,   1, 0x0200013A) /* Setup */
     , (950005,   3, 0x20000014) /* SoundTable */
     , (950005,   6, 0x04000BEF) /* PaletteBase */
     , (950005,   7, 0x10000150) /* ClothingBase */
     , (950005,   8, 0x0600161B) /* Icon */
     , (950005,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950005,  36, 0x0E00001D) /* MutateFilter */
     , (950005,  46, 0x38000003) /* TsysMutationFilter */;
