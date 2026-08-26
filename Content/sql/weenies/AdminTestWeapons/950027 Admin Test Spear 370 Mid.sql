/* Admin Test Spear 370 Mid -- clone of wcid 348 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950027;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950027, 'ace950027-admintestspear370mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950027,   1, 1) /* ItemType */
     , (950027,   3, 20) /* PaletteTemplate */
     , (950027,   5, 700) /* EncumbranceVal */
     , (950027,   8, 140) /* Mass */
     , (950027,   9, 1048576) /* ValidLocations */
     , (950027,  16, 1) /* ItemUseable */
     , (950027,  19, 170) /* Value */
     , (950027,  44, 34) /* Damage */
     , (950027,  45, 2) /* DamageType */
     , (950027,  46, 2) /* DefaultCombatStyle */
     , (950027,  47, 2) /* AttackType */
     , (950027,  48, 9) /* WeaponSkill */
     , (950027,  49, 25) /* WeaponTime */
     , (950027,  51, 1) /* CombatUse */
     , (950027,  93, 1044) /* PhysicsState */
     , (950027, 105, 1) /* ItemWorkmanship */
     , (950027, 131, 64) /* MaterialType */
     , (950027, 150, 103) /* HookPlacement */
     , (950027, 151, 2) /* HookType */
     , (950027, 158, 2) /* WieldRequirements */
     , (950027, 159, 9) /* WieldSkillType */
     , (950027, 160, 370) /* WieldDifficulty */
     , (950027, 169, 101188618) /* TsysMutationData */
     , (950027, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950027,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950027,  21, 1.5) /* WeaponLength */
     , (950027,  22, 0.6) /* DamageVariance */
     , (950027,  29, 1.11) /* WeaponDefense */
     , (950027,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950027,   1, 'Admin Test Spear 370 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950027,   1, 0x02000144) /* Setup */
     , (950027,   3, 0x20000014) /* SoundTable */
     , (950027,   6, 0x04000BEF) /* PaletteBase */
     , (950027,   7, 0x10000138) /* ClothingBase */
     , (950027,   8, 0x0600164D) /* Icon */
     , (950027,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950027,  36, 0x0E00001D) /* MutateFilter */
     , (950027,  46, 0x38000004) /* TsysMutationFilter */;
