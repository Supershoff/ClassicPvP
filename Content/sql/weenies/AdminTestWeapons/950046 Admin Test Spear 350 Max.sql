/* Admin Test Spear 350 Max -- clone of wcid 348 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950046;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950046, 'ace950046-admintestspear350max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950046,   1, 1) /* ItemType */
     , (950046,   3, 20) /* PaletteTemplate */
     , (950046,   5, 700) /* EncumbranceVal */
     , (950046,   8, 140) /* Mass */
     , (950046,   9, 1048576) /* ValidLocations */
     , (950046,  16, 1) /* ItemUseable */
     , (950046,  19, 170) /* Value */
     , (950046,  44, 32) /* Damage */
     , (950046,  45, 2) /* DamageType */
     , (950046,  46, 2) /* DefaultCombatStyle */
     , (950046,  47, 2) /* AttackType */
     , (950046,  48, 9) /* WeaponSkill */
     , (950046,  49, 22) /* WeaponTime */
     , (950046,  51, 1) /* CombatUse */
     , (950046,  93, 1044) /* PhysicsState */
     , (950046, 105, 1) /* ItemWorkmanship */
     , (950046, 131, 64) /* MaterialType */
     , (950046, 150, 103) /* HookPlacement */
     , (950046, 151, 2) /* HookType */
     , (950046, 158, 2) /* WieldRequirements */
     , (950046, 159, 9) /* WieldSkillType */
     , (950046, 160, 350) /* WieldDifficulty */
     , (950046, 169, 101188618) /* TsysMutationData */
     , (950046, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950046,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950046,  21, 1.5) /* WeaponLength */
     , (950046,  22, 0.45) /* DamageVariance */
     , (950046,  29, 1.15) /* WeaponDefense */
     , (950046,  62, 1.15) /* WeaponOffense */
     , (950046, 149, 1.025) /* WeaponMissileDefense */
     , (950046, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950046,   1, 'Admin Test Spear 350 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950046,   1, 0x02000144) /* Setup */
     , (950046,   3, 0x20000014) /* SoundTable */
     , (950046,   6, 0x04000BEF) /* PaletteBase */
     , (950046,   7, 0x10000138) /* ClothingBase */
     , (950046,   8, 0x0600164D) /* Icon */
     , (950046,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950046,  36, 0x0E00001D) /* MutateFilter */
     , (950046,  46, 0x38000004) /* TsysMutationFilter */;
