/* Admin Test Atlatl 315 Max -- clone of wcid 29258 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950058;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950058, 'ace950058-admintestatlatl315max', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950058,   1, 256) /* ItemType */
     , (950058,   3, 20) /* PaletteTemplate */
     , (950058,   5, 400) /* EncumbranceVal */
     , (950058,   8, 16) /* Mass */
     , (950058,   9, 4194304) /* ValidLocations */
     , (950058,  16, 1) /* ItemUseable */
     , (950058,  18, 1024) /* UiEffects */
     , (950058,  19, 200) /* Value */
     , (950058,  44, 0) /* Damage */
     , (950058,  45, 1) /* DamageType */
     , (950058,  46, 1024) /* DefaultCombatStyle */
     , (950058,  48, 12) /* WeaponSkill */
     , (950058,  49, 18) /* WeaponTime */
     , (950058,  50, 4) /* AmmoType */
     , (950058,  51, 2) /* CombatUse */
     , (950058,  60, 120) /* WeaponRange */
     , (950058,  93, 1044) /* PhysicsState */
     , (950058, 105, 1) /* ItemWorkmanship */
     , (950058, 131, 75) /* MaterialType */
     , (950058, 150, 103) /* HookPlacement */
     , (950058, 151, 2) /* HookType */
     , (950058, 158, 2) /* WieldRequirements */
     , (950058, 159, 12) /* WieldSkillType */
     , (950058, 160, 315) /* WieldDifficulty */
     , (950058, 169, 101189386) /* TsysMutationData */
     , (950058, 204, 3) /* ElementalDamageBonus */
     , (950058, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950058,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950058,  26, 24.9) /* MaximumVelocity */
     , (950058,  29, 1.15) /* WeaponDefense */
     , (950058,  39, 1.1) /* DefaultScale */
     , (950058,  62, 1.0) /* WeaponOffense */
     , (950058,  63, 2.5) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950058,   1, 'Admin Test Atlatl 315 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950058,   1, 0x020012C9) /* Setup */
     , (950058,   3, 0x20000014) /* SoundTable */
     , (950058,   6, 0x0400196D) /* PaletteBase */
     , (950058,   7, 0x100005A8) /* ClothingBase */
     , (950058,   8, 0x060026E2) /* Icon */
     , (950058,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950058,  36, 0x0E00001D) /* MutateFilter */
     , (950058,  46, 0x38000049) /* TsysMutationFilter */;
