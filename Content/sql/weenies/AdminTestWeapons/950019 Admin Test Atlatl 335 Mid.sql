/* Admin Test Atlatl 335 Mid -- clone of wcid 29258 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950019;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950019, 'ace950019-admintestatlatl335mid', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950019,   1, 256) /* ItemType */
     , (950019,   3, 20) /* PaletteTemplate */
     , (950019,   5, 400) /* EncumbranceVal */
     , (950019,   8, 16) /* Mass */
     , (950019,   9, 4194304) /* ValidLocations */
     , (950019,  16, 1) /* ItemUseable */
     , (950019,  18, 1024) /* UiEffects */
     , (950019,  19, 200) /* Value */
     , (950019,  44, 0) /* Damage */
     , (950019,  45, 1) /* DamageType */
     , (950019,  46, 1024) /* DefaultCombatStyle */
     , (950019,  48, 12) /* WeaponSkill */
     , (950019,  49, 21) /* WeaponTime */
     , (950019,  50, 4) /* AmmoType */
     , (950019,  51, 2) /* CombatUse */
     , (950019,  60, 120) /* WeaponRange */
     , (950019,  93, 1044) /* PhysicsState */
     , (950019, 105, 1) /* ItemWorkmanship */
     , (950019, 131, 75) /* MaterialType */
     , (950019, 150, 103) /* HookPlacement */
     , (950019, 151, 2) /* HookType */
     , (950019, 158, 2) /* WieldRequirements */
     , (950019, 159, 12) /* WieldSkillType */
     , (950019, 160, 335) /* WieldDifficulty */
     , (950019, 169, 101189386) /* TsysMutationData */
     , (950019, 204, 4) /* ElementalDamageBonus */
     , (950019, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950019,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950019,  26, 24.9) /* MaximumVelocity */
     , (950019,  29, 1.11) /* WeaponDefense */
     , (950019,  39, 1.1) /* DefaultScale */
     , (950019,  62, 1.0) /* WeaponOffense */
     , (950019,  63, 2.45) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950019,   1, 'Admin Test Atlatl 335 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950019,   1, 0x020012C9) /* Setup */
     , (950019,   3, 0x20000014) /* SoundTable */
     , (950019,   6, 0x0400196D) /* PaletteBase */
     , (950019,   7, 0x100005A8) /* ClothingBase */
     , (950019,   8, 0x060026E2) /* Icon */
     , (950019,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950019,  36, 0x0E00001D) /* MutateFilter */
     , (950019,  46, 0x38000049) /* TsysMutationFilter */;
