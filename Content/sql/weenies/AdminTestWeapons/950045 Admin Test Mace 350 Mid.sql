/* Admin Test Mace 350 Mid -- clone of wcid 331 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950045;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950045, 'ace950045-admintestmace350mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950045,   1, 1) /* ItemType */
     , (950045,   3, 20) /* PaletteTemplate */
     , (950045,   5, 675) /* EncumbranceVal */
     , (950045,   8, 450) /* Mass */
     , (950045,   9, 1048576) /* ValidLocations */
     , (950045,  16, 1) /* ItemUseable */
     , (950045,  19, 260) /* Value */
     , (950045,  44, 35) /* Damage */
     , (950045,  45, 4) /* DamageType */
     , (950045,  46, 2) /* DefaultCombatStyle */
     , (950045,  47, 4) /* AttackType */
     , (950045,  48, 5) /* WeaponSkill */
     , (950045,  49, 34) /* WeaponTime */
     , (950045,  51, 1) /* CombatUse */
     , (950045,  93, 1044) /* PhysicsState */
     , (950045, 105, 1) /* ItemWorkmanship */
     , (950045, 131, 64) /* MaterialType */
     , (950045, 150, 103) /* HookPlacement */
     , (950045, 151, 2) /* HookType */
     , (950045, 158, 2) /* WieldRequirements */
     , (950045, 159, 5) /* WieldSkillType */
     , (950045, 160, 350) /* WieldDifficulty */
     , (950045, 169, 101189386) /* TsysMutationData */
     , (950045, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950045,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950045,  21, 0.62) /* WeaponLength */
     , (950045,  22, 0.4) /* DamageVariance */
     , (950045,  29, 1.11) /* WeaponDefense */
     , (950045,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950045,   1, 'Admin Test Mace 350 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950045,   1, 0x0200013A) /* Setup */
     , (950045,   3, 0x20000014) /* SoundTable */
     , (950045,   6, 0x04000BEF) /* PaletteBase */
     , (950045,   7, 0x10000150) /* ClothingBase */
     , (950045,   8, 0x0600161B) /* Icon */
     , (950045,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950045,  36, 0x0E00001D) /* MutateFilter */
     , (950045,  46, 0x38000003) /* TsysMutationFilter */;
