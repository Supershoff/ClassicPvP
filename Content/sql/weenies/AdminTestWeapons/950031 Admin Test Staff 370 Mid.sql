/* Admin Test Staff 370 Mid -- clone of wcid 338 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950031;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950031, 'ace950031-adminteststaff370mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950031,   1, 1) /* ItemType */
     , (950031,   3, 4) /* PaletteTemplate */
     , (950031,   5, 450) /* EncumbranceVal */
     , (950031,   8, 90) /* Mass */
     , (950031,   9, 1048576) /* ValidLocations */
     , (950031,  16, 1) /* ItemUseable */
     , (950031,  19, 130) /* Value */
     , (950031,  44, 22) /* Damage */
     , (950031,  45, 4) /* DamageType */
     , (950031,  46, 2) /* DefaultCombatStyle */
     , (950031,  47, 6) /* AttackType */
     , (950031,  48, 10) /* WeaponSkill */
     , (950031,  49, 25) /* WeaponTime */
     , (950031,  51, 1) /* CombatUse */
     , (950031,  93, 1044) /* PhysicsState */
     , (950031, 105, 1) /* ItemWorkmanship */
     , (950031, 131, 75) /* MaterialType */
     , (950031, 150, 103) /* HookPlacement */
     , (950031, 151, 2) /* HookType */
     , (950031, 158, 2) /* WieldRequirements */
     , (950031, 159, 10) /* WieldSkillType */
     , (950031, 160, 370) /* WieldDifficulty */
     , (950031, 169, 101189388) /* TsysMutationData */
     , (950031, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950031,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950031,  21, 1.33) /* WeaponLength */
     , (950031,  22, 0.4) /* DamageVariance */
     , (950031,  29, 1.11) /* WeaponDefense */
     , (950031,  39, 0.67) /* DefaultScale */
     , (950031,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950031,   1, 'Admin Test Staff 370 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950031,   1, 0x0200013D) /* Setup */
     , (950031,   3, 0x20000014) /* SoundTable */
     , (950031,   6, 0x04000BEF) /* PaletteBase */
     , (950031,   7, 0x10000153) /* ClothingBase */
     , (950031,   8, 0x060016B1) /* Icon */
     , (950031,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950031,  36, 0x0E00001D) /* MutateFilter */
     , (950031,  46, 0x3800000E) /* TsysMutationFilter */;
