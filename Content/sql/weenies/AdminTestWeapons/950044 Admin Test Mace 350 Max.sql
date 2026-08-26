/* Admin Test Mace 350 Max -- clone of wcid 331 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950044;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950044, 'ace950044-admintestmace350max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950044,   1, 1) /* ItemType */
     , (950044,   3, 20) /* PaletteTemplate */
     , (950044,   5, 675) /* EncumbranceVal */
     , (950044,   8, 450) /* Mass */
     , (950044,   9, 1048576) /* ValidLocations */
     , (950044,  16, 1) /* ItemUseable */
     , (950044,  19, 260) /* Value */
     , (950044,  44, 36) /* Damage */
     , (950044,  45, 4) /* DamageType */
     , (950044,  46, 2) /* DefaultCombatStyle */
     , (950044,  47, 4) /* AttackType */
     , (950044,  48, 5) /* WeaponSkill */
     , (950044,  49, 30) /* WeaponTime */
     , (950044,  51, 1) /* CombatUse */
     , (950044,  93, 1044) /* PhysicsState */
     , (950044, 105, 1) /* ItemWorkmanship */
     , (950044, 131, 64) /* MaterialType */
     , (950044, 150, 103) /* HookPlacement */
     , (950044, 151, 2) /* HookType */
     , (950044, 158, 2) /* WieldRequirements */
     , (950044, 159, 5) /* WieldSkillType */
     , (950044, 160, 350) /* WieldDifficulty */
     , (950044, 169, 101189386) /* TsysMutationData */
     , (950044, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950044,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950044,  21, 0.62) /* WeaponLength */
     , (950044,  22, 0.25) /* DamageVariance */
     , (950044,  29, 1.15) /* WeaponDefense */
     , (950044,  62, 1.15) /* WeaponOffense */
     , (950044, 149, 1.025) /* WeaponMissileDefense */
     , (950044, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950044,   1, 'Admin Test Mace 350 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950044,   1, 0x0200013A) /* Setup */
     , (950044,   3, 0x20000014) /* SoundTable */
     , (950044,   6, 0x04000BEF) /* PaletteBase */
     , (950044,   7, 0x10000150) /* ClothingBase */
     , (950044,   8, 0x0600161B) /* Icon */
     , (950044,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950044,  36, 0x0E00001D) /* MutateFilter */
     , (950044,  46, 0x38000003) /* TsysMutationFilter */;
