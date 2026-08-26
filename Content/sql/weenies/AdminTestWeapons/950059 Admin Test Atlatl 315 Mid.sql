/* Admin Test Atlatl 315 Mid -- clone of wcid 29258 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950059;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950059, 'ace950059-admintestatlatl315mid', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950059,   1, 256) /* ItemType */
     , (950059,   3, 20) /* PaletteTemplate */
     , (950059,   5, 400) /* EncumbranceVal */
     , (950059,   8, 16) /* Mass */
     , (950059,   9, 4194304) /* ValidLocations */
     , (950059,  16, 1) /* ItemUseable */
     , (950059,  18, 1024) /* UiEffects */
     , (950059,  19, 200) /* Value */
     , (950059,  44, 0) /* Damage */
     , (950059,  45, 1) /* DamageType */
     , (950059,  46, 1024) /* DefaultCombatStyle */
     , (950059,  48, 12) /* WeaponSkill */
     , (950059,  49, 21) /* WeaponTime */
     , (950059,  50, 4) /* AmmoType */
     , (950059,  51, 2) /* CombatUse */
     , (950059,  60, 120) /* WeaponRange */
     , (950059,  93, 1044) /* PhysicsState */
     , (950059, 105, 1) /* ItemWorkmanship */
     , (950059, 131, 75) /* MaterialType */
     , (950059, 150, 103) /* HookPlacement */
     , (950059, 151, 2) /* HookType */
     , (950059, 158, 2) /* WieldRequirements */
     , (950059, 159, 12) /* WieldSkillType */
     , (950059, 160, 315) /* WieldDifficulty */
     , (950059, 169, 101189386) /* TsysMutationData */
     , (950059, 204, 1) /* ElementalDamageBonus */
     , (950059, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950059,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950059,  26, 24.9) /* MaximumVelocity */
     , (950059,  29, 1.11) /* WeaponDefense */
     , (950059,  39, 1.1) /* DefaultScale */
     , (950059,  62, 1.0) /* WeaponOffense */
     , (950059,  63, 2.45) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950059,   1, 'Admin Test Atlatl 315 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950059,   1, 0x020012C9) /* Setup */
     , (950059,   3, 0x20000014) /* SoundTable */
     , (950059,   6, 0x0400196D) /* PaletteBase */
     , (950059,   7, 0x100005A8) /* ClothingBase */
     , (950059,   8, 0x060026E2) /* Icon */
     , (950059,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950059,  36, 0x0E00001D) /* MutateFilter */
     , (950059,  46, 0x38000049) /* TsysMutationFilter */;
