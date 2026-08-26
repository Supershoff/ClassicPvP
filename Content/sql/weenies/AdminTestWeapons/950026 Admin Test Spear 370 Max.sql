/* Admin Test Spear 370 Max -- clone of wcid 348 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950026;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950026, 'ace950026-admintestspear370max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950026,   1, 1) /* ItemType */
     , (950026,   3, 20) /* PaletteTemplate */
     , (950026,   5, 700) /* EncumbranceVal */
     , (950026,   8, 140) /* Mass */
     , (950026,   9, 1048576) /* ValidLocations */
     , (950026,  16, 1) /* ItemUseable */
     , (950026,  19, 170) /* Value */
     , (950026,  44, 36) /* Damage */
     , (950026,  45, 2) /* DamageType */
     , (950026,  46, 2) /* DefaultCombatStyle */
     , (950026,  47, 2) /* AttackType */
     , (950026,  48, 9) /* WeaponSkill */
     , (950026,  49, 22) /* WeaponTime */
     , (950026,  51, 1) /* CombatUse */
     , (950026,  93, 1044) /* PhysicsState */
     , (950026, 105, 1) /* ItemWorkmanship */
     , (950026, 131, 64) /* MaterialType */
     , (950026, 150, 103) /* HookPlacement */
     , (950026, 151, 2) /* HookType */
     , (950026, 158, 2) /* WieldRequirements */
     , (950026, 159, 9) /* WieldSkillType */
     , (950026, 160, 370) /* WieldDifficulty */
     , (950026, 169, 101188618) /* TsysMutationData */
     , (950026, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950026,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950026,  21, 1.5) /* WeaponLength */
     , (950026,  22, 0.45) /* DamageVariance */
     , (950026,  29, 1.15) /* WeaponDefense */
     , (950026,  62, 1.15) /* WeaponOffense */
     , (950026, 149, 1.025) /* WeaponMissileDefense */
     , (950026, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950026,   1, 'Admin Test Spear 370 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950026,   1, 0x02000144) /* Setup */
     , (950026,   3, 0x20000014) /* SoundTable */
     , (950026,   6, 0x04000BEF) /* PaletteBase */
     , (950026,   7, 0x10000138) /* ClothingBase */
     , (950026,   8, 0x0600164D) /* Icon */
     , (950026,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950026,  36, 0x0E00001D) /* MutateFilter */
     , (950026,  46, 0x38000004) /* TsysMutationFilter */;
