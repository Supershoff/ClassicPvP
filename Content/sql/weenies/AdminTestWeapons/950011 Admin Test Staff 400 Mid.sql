/* Admin Test Staff 400 Mid -- clone of wcid 338 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950011;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950011, 'ace950011-adminteststaff400mid', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950011,   1, 1) /* ItemType */
     , (950011,   3, 4) /* PaletteTemplate */
     , (950011,   5, 450) /* EncumbranceVal */
     , (950011,   8, 90) /* Mass */
     , (950011,   9, 1048576) /* ValidLocations */
     , (950011,  16, 1) /* ItemUseable */
     , (950011,  19, 130) /* Value */
     , (950011,  44, 25) /* Damage */
     , (950011,  45, 4) /* DamageType */
     , (950011,  46, 2) /* DefaultCombatStyle */
     , (950011,  47, 6) /* AttackType */
     , (950011,  48, 10) /* WeaponSkill */
     , (950011,  49, 25) /* WeaponTime */
     , (950011,  51, 1) /* CombatUse */
     , (950011,  93, 1044) /* PhysicsState */
     , (950011, 105, 1) /* ItemWorkmanship */
     , (950011, 131, 75) /* MaterialType */
     , (950011, 150, 103) /* HookPlacement */
     , (950011, 151, 2) /* HookType */
     , (950011, 158, 2) /* WieldRequirements */
     , (950011, 159, 10) /* WieldSkillType */
     , (950011, 160, 400) /* WieldDifficulty */
     , (950011, 169, 101189388) /* TsysMutationData */
     , (950011, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950011,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950011,  21, 1.33) /* WeaponLength */
     , (950011,  22, 0.4) /* DamageVariance */
     , (950011,  29, 1.11) /* WeaponDefense */
     , (950011,  39, 0.67) /* DefaultScale */
     , (950011,  62, 1.11) /* WeaponOffense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950011,   1, 'Admin Test Staff 400 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950011,   1, 0x0200013D) /* Setup */
     , (950011,   3, 0x20000014) /* SoundTable */
     , (950011,   6, 0x04000BEF) /* PaletteBase */
     , (950011,   7, 0x10000153) /* ClothingBase */
     , (950011,   8, 0x060016B1) /* Icon */
     , (950011,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950011,  36, 0x0E00001D) /* MutateFilter */
     , (950011,  46, 0x3800000E) /* TsysMutationFilter */;
