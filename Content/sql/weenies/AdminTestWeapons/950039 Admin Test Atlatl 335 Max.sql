/* Admin Test Atlatl 335 Max -- clone of wcid 29258 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950039;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950039, 'ace950039-admintestatlatl335max', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950039,   1, 256) /* ItemType */
     , (950039,   3, 20) /* PaletteTemplate */
     , (950039,   5, 400) /* EncumbranceVal */
     , (950039,   8, 16) /* Mass */
     , (950039,   9, 4194304) /* ValidLocations */
     , (950039,  16, 1) /* ItemUseable */
     , (950039,  18, 1024) /* UiEffects */
     , (950039,  19, 200) /* Value */
     , (950039,  44, 0) /* Damage */
     , (950039,  45, 1) /* DamageType */
     , (950039,  46, 1024) /* DefaultCombatStyle */
     , (950039,  48, 12) /* WeaponSkill */
     , (950039,  49, 18) /* WeaponTime */
     , (950039,  50, 4) /* AmmoType */
     , (950039,  51, 2) /* CombatUse */
     , (950039,  60, 120) /* WeaponRange */
     , (950039,  93, 1044) /* PhysicsState */
     , (950039, 105, 1) /* ItemWorkmanship */
     , (950039, 131, 75) /* MaterialType */
     , (950039, 150, 103) /* HookPlacement */
     , (950039, 151, 2) /* HookType */
     , (950039, 158, 2) /* WieldRequirements */
     , (950039, 159, 12) /* WieldSkillType */
     , (950039, 160, 335) /* WieldDifficulty */
     , (950039, 169, 101189386) /* TsysMutationData */
     , (950039, 204, 6) /* ElementalDamageBonus */
     , (950039, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950039,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950039,  26, 24.9) /* MaximumVelocity */
     , (950039,  29, 1.15) /* WeaponDefense */
     , (950039,  39, 1.1) /* DefaultScale */
     , (950039,  62, 1.0) /* WeaponOffense */
     , (950039,  63, 2.5) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950039,   1, 'Admin Test Atlatl 335 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950039,   1, 0x020012C9) /* Setup */
     , (950039,   3, 0x20000014) /* SoundTable */
     , (950039,   6, 0x0400196D) /* PaletteBase */
     , (950039,   7, 0x100005A8) /* ClothingBase */
     , (950039,   8, 0x060026E2) /* Icon */
     , (950039,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950039,  36, 0x0E00001D) /* MutateFilter */
     , (950039,  46, 0x38000049) /* TsysMutationFilter */;
