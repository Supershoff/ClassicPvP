/* Admin Test Mace 400 Max -- clone of wcid 331 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950004;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950004, 'ace950004-admintestmace400max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950004,   1, 1) /* ItemType */
     , (950004,   3, 20) /* PaletteTemplate */
     , (950004,   5, 675) /* EncumbranceVal */
     , (950004,   8, 450) /* Mass */
     , (950004,   9, 1048576) /* ValidLocations */
     , (950004,  16, 1) /* ItemUseable */
     , (950004,  19, 260) /* Value */
     , (950004,  44, 42) /* Damage */
     , (950004,  45, 4) /* DamageType */
     , (950004,  46, 2) /* DefaultCombatStyle */
     , (950004,  47, 4) /* AttackType */
     , (950004,  48, 5) /* WeaponSkill */
     , (950004,  49, 30) /* WeaponTime */
     , (950004,  51, 1) /* CombatUse */
     , (950004,  93, 1044) /* PhysicsState */
     , (950004, 105, 1) /* ItemWorkmanship */
     , (950004, 131, 64) /* MaterialType */
     , (950004, 150, 103) /* HookPlacement */
     , (950004, 151, 2) /* HookType */
     , (950004, 158, 2) /* WieldRequirements */
     , (950004, 159, 5) /* WieldSkillType */
     , (950004, 160, 400) /* WieldDifficulty */
     , (950004, 169, 101189386) /* TsysMutationData */
     , (950004, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950004,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950004,  21, 0.62) /* WeaponLength */
     , (950004,  22, 0.25) /* DamageVariance */
     , (950004,  29, 1.15) /* WeaponDefense */
     , (950004,  62, 1.15) /* WeaponOffense */
     , (950004, 149, 1.025) /* WeaponMissileDefense */
     , (950004, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950004,   1, 'Admin Test Mace 400 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950004,   1, 0x0200013A) /* Setup */
     , (950004,   3, 0x20000014) /* SoundTable */
     , (950004,   6, 0x04000BEF) /* PaletteBase */
     , (950004,   7, 0x10000150) /* ClothingBase */
     , (950004,   8, 0x0600161B) /* Icon */
     , (950004,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950004,  36, 0x0E00001D) /* MutateFilter */
     , (950004,  46, 0x38000003) /* TsysMutationFilter */;
