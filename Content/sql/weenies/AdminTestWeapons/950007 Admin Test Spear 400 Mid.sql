/* Admin Test Spear 400 Mid -- clone of wcid 348 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950007;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950007, 'ace950007-admintestspear400mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950007,   1, 1) /* ItemType */
     , (950007,   3, 20) /* PaletteTemplate */
     , (950007,   5, 700) /* EncumbranceVal */
     , (950007,   8, 140) /* Mass */
     , (950007,   9, 1048576) /* ValidLocations */
     , (950007,  16, 1) /* ItemUseable */
     , (950007,  19, 170) /* Value */
     , (950007,  44, 38) /* Damage */
     , (950007,  45, 2) /* DamageType */
     , (950007,  46, 2) /* DefaultCombatStyle */
     , (950007,  47, 2) /* AttackType */
     , (950007,  48, 9) /* WeaponSkill */
     , (950007,  49, 25) /* WeaponTime */
     , (950007,  51, 1) /* CombatUse */
     , (950007,  93, 1044) /* PhysicsState */
     , (950007, 105, 1) /* ItemWorkmanship */
     , (950007, 131, 64) /* MaterialType */
     , (950007, 150, 103) /* HookPlacement */
     , (950007, 151, 2) /* HookType */
     , (950007, 158, 2) /* WieldRequirements */
     , (950007, 159, 9) /* WieldSkillType */
     , (950007, 160, 400) /* WieldDifficulty */
     , (950007, 169, 101188618) /* TsysMutationData */
     , (950007, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950007,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950007,  21, 1.5) /* WeaponLength */
     , (950007,  22, 0.6) /* DamageVariance */
     , (950007,  29, 1.11) /* WeaponDefense */
     , (950007,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950007,   1, 'Admin Test Spear 400 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950007,   1, 0x02000144) /* Setup */
     , (950007,   3, 0x20000014) /* SoundTable */
     , (950007,   6, 0x04000BEF) /* PaletteBase */
     , (950007,   7, 0x10000138) /* ClothingBase */
     , (950007,   8, 0x0600164D) /* Icon */
     , (950007,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950007,  36, 0x0E00001D) /* MutateFilter */
     , (950007,  46, 0x38000004) /* TsysMutationFilter */;
