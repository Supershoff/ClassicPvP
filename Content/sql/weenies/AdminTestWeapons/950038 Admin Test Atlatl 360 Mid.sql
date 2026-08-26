/* Admin Test Atlatl 360 Mid -- clone of wcid 29258 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950038;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950038, 'ace950038-admintestatlatl360mid', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950038,   1, 256) /* ItemType */
     , (950038,   3, 20) /* PaletteTemplate */
     , (950038,   5, 400) /* EncumbranceVal */
     , (950038,   8, 16) /* Mass */
     , (950038,   9, 4194304) /* ValidLocations */
     , (950038,  16, 1) /* ItemUseable */
     , (950038,  18, 1024) /* UiEffects */
     , (950038,  19, 200) /* Value */
     , (950038,  44, 0) /* Damage */
     , (950038,  45, 1) /* DamageType */
     , (950038,  46, 1024) /* DefaultCombatStyle */
     , (950038,  48, 12) /* WeaponSkill */
     , (950038,  49, 21) /* WeaponTime */
     , (950038,  50, 4) /* AmmoType */
     , (950038,  51, 2) /* CombatUse */
     , (950038,  60, 120) /* WeaponRange */
     , (950038,  93, 1044) /* PhysicsState */
     , (950038, 105, 1) /* ItemWorkmanship */
     , (950038, 131, 75) /* MaterialType */
     , (950038, 150, 103) /* HookPlacement */
     , (950038, 151, 2) /* HookType */
     , (950038, 158, 2) /* WieldRequirements */
     , (950038, 159, 12) /* WieldSkillType */
     , (950038, 160, 360) /* WieldDifficulty */
     , (950038, 169, 101189386) /* TsysMutationData */
     , (950038, 204, 8) /* ElementalDamageBonus */
     , (950038, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950038,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950038,  26, 24.9) /* MaximumVelocity */
     , (950038,  29, 1.11) /* WeaponDefense */
     , (950038,  39, 1.1) /* DefaultScale */
     , (950038,  62, 1.0) /* WeaponOffense */
     , (950038,  63, 2.45) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950038,   1, 'Admin Test Atlatl 360 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950038,   1, 0x020012C9) /* Setup */
     , (950038,   3, 0x20000014) /* SoundTable */
     , (950038,   6, 0x0400196D) /* PaletteBase */
     , (950038,   7, 0x100005A8) /* ClothingBase */
     , (950038,   8, 0x060026E2) /* Icon */
     , (950038,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950038,  36, 0x0E00001D) /* MutateFilter */
     , (950038,  46, 0x38000049) /* TsysMutationFilter */;
