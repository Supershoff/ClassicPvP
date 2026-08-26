/* Admin Test Sword 370 Mid -- clone of wcid 350 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950021;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950021, 'ace950021-admintestsword370mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950021,   1, 1) /* ItemType */
     , (950021,   3, 20) /* PaletteTemplate */
     , (950021,   5, 550) /* EncumbranceVal */
     , (950021,   8, 220) /* Mass */
     , (950021,   9, 1048576) /* ValidLocations */
     , (950021,  16, 1) /* ItemUseable */
     , (950021,  19, 340) /* Value */
     , (950021,  44, 48) /* Damage */
     , (950021,  45, 3) /* DamageType */
     , (950021,  46, 2) /* DefaultCombatStyle */
     , (950021,  47, 6) /* AttackType */
     , (950021,  48, 11) /* WeaponSkill */
     , (950021,  49, 42) /* WeaponTime */
     , (950021,  51, 1) /* CombatUse */
     , (950021,  93, 1044) /* PhysicsState */
     , (950021, 105, 1) /* ItemWorkmanship */
     , (950021, 131, 64) /* MaterialType */
     , (950021, 150, 103) /* HookPlacement */
     , (950021, 151, 2) /* HookType */
     , (950021, 158, 2) /* WieldRequirements */
     , (950021, 159, 11) /* WieldSkillType */
     , (950021, 160, 370) /* WieldDifficulty */
     , (950021, 169, 101255170) /* TsysMutationData */
     , (950021, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950021,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950021,  21, 0.95) /* WeaponLength */
     , (950021,  22, 0.45) /* DamageVariance */
     , (950021,  29, 1.11) /* WeaponDefense */
     , (950021,  39, 1.1) /* DefaultScale */
     , (950021,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950021,   1, 'Admin Test Sword 370 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950021,   1, 0x02000146) /* Setup */
     , (950021,   3, 0x20000014) /* SoundTable */
     , (950021,   6, 0x04000BEF) /* PaletteBase */
     , (950021,   7, 0x1000013A) /* ClothingBase */
     , (950021,   8, 0x06001657) /* Icon */
     , (950021,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950021,  36, 0x0E00001D) /* MutateFilter */
     , (950021,  46, 0x38000005) /* TsysMutationFilter */;
