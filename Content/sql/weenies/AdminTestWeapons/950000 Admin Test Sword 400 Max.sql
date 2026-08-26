/* Admin Test Sword 400 Max -- clone of wcid 350 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950000;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950000, 'ace950000-admintestsword400max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950000,   1, 1) /* ItemType */
     , (950000,   3, 20) /* PaletteTemplate */
     , (950000,   5, 550) /* EncumbranceVal */
     , (950000,   8, 220) /* Mass */
     , (950000,   9, 1048576) /* ValidLocations */
     , (950000,  16, 1) /* ItemUseable */
     , (950000,  19, 340) /* Value */
     , (950000,  44, 55) /* Damage */
     , (950000,  45, 3) /* DamageType */
     , (950000,  46, 2) /* DefaultCombatStyle */
     , (950000,  47, 6) /* AttackType */
     , (950000,  48, 11) /* WeaponSkill */
     , (950000,  49, 37) /* WeaponTime */
     , (950000,  51, 1) /* CombatUse */
     , (950000,  93, 1044) /* PhysicsState */
     , (950000, 105, 1) /* ItemWorkmanship */
     , (950000, 131, 64) /* MaterialType */
     , (950000, 150, 103) /* HookPlacement */
     , (950000, 151, 2) /* HookType */
     , (950000, 158, 2) /* WieldRequirements */
     , (950000, 159, 11) /* WieldSkillType */
     , (950000, 160, 400) /* WieldDifficulty */
     , (950000, 169, 101255170) /* TsysMutationData */
     , (950000, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950000,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950000,  21, 0.95) /* WeaponLength */
     , (950000,  22, 0.4) /* DamageVariance */
     , (950000,  29, 1.15) /* WeaponDefense */
     , (950000,  39, 1.1) /* DefaultScale */
     , (950000,  62, 1.15) /* WeaponOffense */
     , (950000, 149, 1.025) /* WeaponMissileDefense */
     , (950000, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950000,   1, 'Admin Test Sword 400 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950000,   1, 0x02000146) /* Setup */
     , (950000,   3, 0x20000014) /* SoundTable */
     , (950000,   6, 0x04000BEF) /* PaletteBase */
     , (950000,   7, 0x1000013A) /* ClothingBase */
     , (950000,   8, 0x06001657) /* Icon */
     , (950000,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950000,  36, 0x0E00001D) /* MutateFilter */
     , (950000,  46, 0x38000005) /* TsysMutationFilter */;
