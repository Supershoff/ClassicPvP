/* Admin Test Atlatl 360 Max -- clone of wcid 29258 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950018;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950018, 'ace950018-admintestatlatl360max', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950018,   1, 256) /* ItemType */
     , (950018,   3, 20) /* PaletteTemplate */
     , (950018,   5, 400) /* EncumbranceVal */
     , (950018,   8, 16) /* Mass */
     , (950018,   9, 4194304) /* ValidLocations */
     , (950018,  16, 1) /* ItemUseable */
     , (950018,  18, 1024) /* UiEffects */
     , (950018,  19, 200) /* Value */
     , (950018,  44, 0) /* Damage */
     , (950018,  45, 1) /* DamageType */
     , (950018,  46, 1024) /* DefaultCombatStyle */
     , (950018,  48, 12) /* WeaponSkill */
     , (950018,  49, 18) /* WeaponTime */
     , (950018,  50, 4) /* AmmoType */
     , (950018,  51, 2) /* CombatUse */
     , (950018,  60, 120) /* WeaponRange */
     , (950018,  93, 1044) /* PhysicsState */
     , (950018, 105, 1) /* ItemWorkmanship */
     , (950018, 131, 75) /* MaterialType */
     , (950018, 150, 103) /* HookPlacement */
     , (950018, 151, 2) /* HookType */
     , (950018, 158, 2) /* WieldRequirements */
     , (950018, 159, 12) /* WieldSkillType */
     , (950018, 160, 360) /* WieldDifficulty */
     , (950018, 169, 101189386) /* TsysMutationData */
     , (950018, 204, 10) /* ElementalDamageBonus */
     , (950018, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950018,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950018,  26, 24.9) /* MaximumVelocity */
     , (950018,  29, 1.15) /* WeaponDefense */
     , (950018,  39, 1.1) /* DefaultScale */
     , (950018,  62, 1.0) /* WeaponOffense */
     , (950018,  63, 2.5) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950018,   1, 'Admin Test Atlatl 360 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950018,   1, 0x020012C9) /* Setup */
     , (950018,   3, 0x20000014) /* SoundTable */
     , (950018,   6, 0x0400196D) /* PaletteBase */
     , (950018,   7, 0x100005A8) /* ClothingBase */
     , (950018,   8, 0x060026E2) /* Icon */
     , (950018,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950018,  36, 0x0E00001D) /* MutateFilter */
     , (950018,  46, 0x38000049) /* TsysMutationFilter */;
