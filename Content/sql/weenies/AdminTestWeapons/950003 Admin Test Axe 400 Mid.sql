/* Admin Test Axe 400 Mid -- clone of wcid 301 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950003;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950003, 'ace950003-admintestaxe400mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950003,   1, 1) /* ItemType */
     , (950003,   3, 20) /* PaletteTemplate */
     , (950003,   5, 800) /* EncumbranceVal */
     , (950003,   8, 320) /* Mass */
     , (950003,   9, 1048576) /* ValidLocations */
     , (950003,  16, 1) /* ItemUseable */
     , (950003,  19, 360) /* Value */
     , (950003,  44, 44) /* Damage */
     , (950003,  45, 1) /* DamageType */
     , (950003,  46, 2) /* DefaultCombatStyle */
     , (950003,  47, 4) /* AttackType */
     , (950003,  48, 1) /* WeaponSkill */
     , (950003,  49, 51) /* WeaponTime */
     , (950003,  51, 1) /* CombatUse */
     , (950003,  93, 1044) /* PhysicsState */
     , (950003, 105, 1) /* ItemWorkmanship */
     , (950003, 131, 64) /* MaterialType */
     , (950003, 150, 103) /* HookPlacement */
     , (950003, 151, 2) /* HookType */
     , (950003, 158, 2) /* WieldRequirements */
     , (950003, 159, 1) /* WieldSkillType */
     , (950003, 160, 400) /* WieldDifficulty */
     , (950003, 169, 101189386) /* TsysMutationData */
     , (950003, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950003,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950003,  21, 0.75) /* WeaponLength */
     , (950003,  22, 0.45) /* DamageVariance */
     , (950003,  29, 1.11) /* WeaponDefense */
     , (950003,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950003,   1, 'Admin Test Axe 400 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950003,   1, 0x02000125) /* Setup */
     , (950003,   3, 0x20000014) /* SoundTable */
     , (950003,   6, 0x04000BEF) /* PaletteBase */
     , (950003,   7, 0x10000143) /* ClothingBase */
     , (950003,   8, 0x06001639) /* Icon */
     , (950003,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950003,  30, 0x00000058) /* PhysicsScript */
     , (950003,  36, 0x0E00001D) /* MutateFilter */
     , (950003,  46, 0x38000002) /* TsysMutationFilter */;
