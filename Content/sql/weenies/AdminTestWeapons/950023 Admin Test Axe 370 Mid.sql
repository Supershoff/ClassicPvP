/* Admin Test Axe 370 Mid -- clone of wcid 301 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950023;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950023, 'ace950023-admintestaxe370mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950023,   1, 1) /* ItemType */
     , (950023,   3, 20) /* PaletteTemplate */
     , (950023,   5, 800) /* EncumbranceVal */
     , (950023,   8, 320) /* Mass */
     , (950023,   9, 1048576) /* ValidLocations */
     , (950023,  16, 1) /* ItemUseable */
     , (950023,  19, 360) /* Value */
     , (950023,  44, 40) /* Damage */
     , (950023,  45, 1) /* DamageType */
     , (950023,  46, 2) /* DefaultCombatStyle */
     , (950023,  47, 4) /* AttackType */
     , (950023,  48, 1) /* WeaponSkill */
     , (950023,  49, 51) /* WeaponTime */
     , (950023,  51, 1) /* CombatUse */
     , (950023,  93, 1044) /* PhysicsState */
     , (950023, 105, 1) /* ItemWorkmanship */
     , (950023, 131, 64) /* MaterialType */
     , (950023, 150, 103) /* HookPlacement */
     , (950023, 151, 2) /* HookType */
     , (950023, 158, 2) /* WieldRequirements */
     , (950023, 159, 1) /* WieldSkillType */
     , (950023, 160, 370) /* WieldDifficulty */
     , (950023, 169, 101189386) /* TsysMutationData */
     , (950023, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950023,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950023,  21, 0.75) /* WeaponLength */
     , (950023,  22, 0.45) /* DamageVariance */
     , (950023,  29, 1.11) /* WeaponDefense */
     , (950023,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950023,   1, 'Admin Test Axe 370 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950023,   1, 0x02000125) /* Setup */
     , (950023,   3, 0x20000014) /* SoundTable */
     , (950023,   6, 0x04000BEF) /* PaletteBase */
     , (950023,   7, 0x10000143) /* ClothingBase */
     , (950023,   8, 0x06001639) /* Icon */
     , (950023,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950023,  30, 0x00000058) /* PhysicsScript */
     , (950023,  36, 0x0E00001D) /* MutateFilter */
     , (950023,  46, 0x38000002) /* TsysMutationFilter */;
