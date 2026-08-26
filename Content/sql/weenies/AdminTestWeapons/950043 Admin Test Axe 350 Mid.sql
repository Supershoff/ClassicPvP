/* Admin Test Axe 350 Mid -- clone of wcid 301 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950043;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950043, 'ace950043-admintestaxe350mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950043,   1, 1) /* ItemType */
     , (950043,   3, 20) /* PaletteTemplate */
     , (950043,   5, 800) /* EncumbranceVal */
     , (950043,   8, 320) /* Mass */
     , (950043,   9, 1048576) /* ValidLocations */
     , (950043,  16, 1) /* ItemUseable */
     , (950043,  19, 360) /* Value */
     , (950043,  44, 37) /* Damage */
     , (950043,  45, 1) /* DamageType */
     , (950043,  46, 2) /* DefaultCombatStyle */
     , (950043,  47, 4) /* AttackType */
     , (950043,  48, 1) /* WeaponSkill */
     , (950043,  49, 51) /* WeaponTime */
     , (950043,  51, 1) /* CombatUse */
     , (950043,  93, 1044) /* PhysicsState */
     , (950043, 105, 1) /* ItemWorkmanship */
     , (950043, 131, 64) /* MaterialType */
     , (950043, 150, 103) /* HookPlacement */
     , (950043, 151, 2) /* HookType */
     , (950043, 158, 2) /* WieldRequirements */
     , (950043, 159, 1) /* WieldSkillType */
     , (950043, 160, 350) /* WieldDifficulty */
     , (950043, 169, 101189386) /* TsysMutationData */
     , (950043, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950043,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950043,  21, 0.75) /* WeaponLength */
     , (950043,  22, 0.45) /* DamageVariance */
     , (950043,  29, 1.11) /* WeaponDefense */
     , (950043,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950043,   1, 'Admin Test Axe 350 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950043,   1, 0x02000125) /* Setup */
     , (950043,   3, 0x20000014) /* SoundTable */
     , (950043,   6, 0x04000BEF) /* PaletteBase */
     , (950043,   7, 0x10000143) /* ClothingBase */
     , (950043,   8, 0x06001639) /* Icon */
     , (950043,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950043,  30, 0x00000058) /* PhysicsScript */
     , (950043,  36, 0x0E00001D) /* MutateFilter */
     , (950043,  46, 0x38000002) /* TsysMutationFilter */;
