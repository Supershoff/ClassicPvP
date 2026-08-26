/* Admin Test Axe 400 Max -- clone of wcid 301 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950002;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950002, 'ace950002-admintestaxe400max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950002,   1, 1) /* ItemType */
     , (950002,   3, 20) /* PaletteTemplate */
     , (950002,   5, 800) /* EncumbranceVal */
     , (950002,   8, 320) /* Mass */
     , (950002,   9, 1048576) /* ValidLocations */
     , (950002,  16, 1) /* ItemUseable */
     , (950002,  19, 360) /* Value */
     , (950002,  44, 46) /* Damage */
     , (950002,  45, 1) /* DamageType */
     , (950002,  46, 2) /* DefaultCombatStyle */
     , (950002,  47, 4) /* AttackType */
     , (950002,  48, 1) /* WeaponSkill */
     , (950002,  49, 45) /* WeaponTime */
     , (950002,  51, 1) /* CombatUse */
     , (950002,  93, 1044) /* PhysicsState */
     , (950002, 105, 1) /* ItemWorkmanship */
     , (950002, 131, 64) /* MaterialType */
     , (950002, 150, 103) /* HookPlacement */
     , (950002, 151, 2) /* HookType */
     , (950002, 158, 2) /* WieldRequirements */
     , (950002, 159, 1) /* WieldSkillType */
     , (950002, 160, 400) /* WieldDifficulty */
     , (950002, 169, 101189386) /* TsysMutationData */
     , (950002, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950002,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950002,  21, 0.75) /* WeaponLength */
     , (950002,  22, 0.4) /* DamageVariance */
     , (950002,  29, 1.15) /* WeaponDefense */
     , (950002,  62, 1.15) /* WeaponOffense */
     , (950002, 149, 1.025) /* WeaponMissileDefense */
     , (950002, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950002,   1, 'Admin Test Axe 400 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950002,   1, 0x02000125) /* Setup */
     , (950002,   3, 0x20000014) /* SoundTable */
     , (950002,   6, 0x04000BEF) /* PaletteBase */
     , (950002,   7, 0x10000143) /* ClothingBase */
     , (950002,   8, 0x06001639) /* Icon */
     , (950002,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950002,  30, 0x00000058) /* PhysicsScript */
     , (950002,  36, 0x0E00001D) /* MutateFilter */
     , (950002,  46, 0x38000002) /* TsysMutationFilter */;
