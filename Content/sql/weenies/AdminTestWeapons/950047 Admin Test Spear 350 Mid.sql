/* Admin Test Spear 350 Mid -- clone of wcid 348 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950047;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950047, 'ace950047-admintestspear350mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950047,   1, 1) /* ItemType */
     , (950047,   3, 20) /* PaletteTemplate */
     , (950047,   5, 700) /* EncumbranceVal */
     , (950047,   8, 140) /* Mass */
     , (950047,   9, 1048576) /* ValidLocations */
     , (950047,  16, 1) /* ItemUseable */
     , (950047,  19, 170) /* Value */
     , (950047,  44, 30) /* Damage */
     , (950047,  45, 2) /* DamageType */
     , (950047,  46, 2) /* DefaultCombatStyle */
     , (950047,  47, 2) /* AttackType */
     , (950047,  48, 9) /* WeaponSkill */
     , (950047,  49, 25) /* WeaponTime */
     , (950047,  51, 1) /* CombatUse */
     , (950047,  93, 1044) /* PhysicsState */
     , (950047, 105, 1) /* ItemWorkmanship */
     , (950047, 131, 64) /* MaterialType */
     , (950047, 150, 103) /* HookPlacement */
     , (950047, 151, 2) /* HookType */
     , (950047, 158, 2) /* WieldRequirements */
     , (950047, 159, 9) /* WieldSkillType */
     , (950047, 160, 350) /* WieldDifficulty */
     , (950047, 169, 101188618) /* TsysMutationData */
     , (950047, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950047,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950047,  21, 1.5) /* WeaponLength */
     , (950047,  22, 0.6) /* DamageVariance */
     , (950047,  29, 1.11) /* WeaponDefense */
     , (950047,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950047,   1, 'Admin Test Spear 350 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950047,   1, 0x02000144) /* Setup */
     , (950047,   3, 0x20000014) /* SoundTable */
     , (950047,   6, 0x04000BEF) /* PaletteBase */
     , (950047,   7, 0x10000138) /* ClothingBase */
     , (950047,   8, 0x0600164D) /* Icon */
     , (950047,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950047,  36, 0x0E00001D) /* MutateFilter */
     , (950047,  46, 0x38000004) /* TsysMutationFilter */;
