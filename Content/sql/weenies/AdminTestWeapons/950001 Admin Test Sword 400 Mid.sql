/* Admin Test Sword 400 Mid -- clone of wcid 350 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950001;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950001, 'ace950001-admintestsword400mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950001,   1, 1) /* ItemType */
     , (950001,   3, 20) /* PaletteTemplate */
     , (950001,   5, 550) /* EncumbranceVal */
     , (950001,   8, 220) /* Mass */
     , (950001,   9, 1048576) /* ValidLocations */
     , (950001,  16, 1) /* ItemUseable */
     , (950001,  19, 340) /* Value */
     , (950001,  44, 53) /* Damage */
     , (950001,  45, 3) /* DamageType */
     , (950001,  46, 2) /* DefaultCombatStyle */
     , (950001,  47, 6) /* AttackType */
     , (950001,  48, 11) /* WeaponSkill */
     , (950001,  49, 42) /* WeaponTime */
     , (950001,  51, 1) /* CombatUse */
     , (950001,  93, 1044) /* PhysicsState */
     , (950001, 105, 1) /* ItemWorkmanship */
     , (950001, 131, 64) /* MaterialType */
     , (950001, 150, 103) /* HookPlacement */
     , (950001, 151, 2) /* HookType */
     , (950001, 158, 2) /* WieldRequirements */
     , (950001, 159, 11) /* WieldSkillType */
     , (950001, 160, 400) /* WieldDifficulty */
     , (950001, 169, 101255170) /* TsysMutationData */
     , (950001, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950001,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950001,  21, 0.95) /* WeaponLength */
     , (950001,  22, 0.45) /* DamageVariance */
     , (950001,  29, 1.11) /* WeaponDefense */
     , (950001,  39, 1.1) /* DefaultScale */
     , (950001,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950001,   1, 'Admin Test Sword 400 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950001,   1, 0x02000146) /* Setup */
     , (950001,   3, 0x20000014) /* SoundTable */
     , (950001,   6, 0x04000BEF) /* PaletteBase */
     , (950001,   7, 0x1000013A) /* ClothingBase */
     , (950001,   8, 0x06001657) /* Icon */
     , (950001,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950001,  36, 0x0E00001D) /* MutateFilter */
     , (950001,  46, 0x38000005) /* TsysMutationFilter */;
