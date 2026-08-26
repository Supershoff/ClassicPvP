/* Admin Test Bow 335 Max -- clone of wcid 29244 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950035;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950035, 'ace950035-admintestbow335max', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950035,   1, 256) /* ItemType */
     , (950035,   3, 20) /* PaletteTemplate */
     , (950035,   5, 980) /* EncumbranceVal */
     , (950035,   8, 140) /* Mass */
     , (950035,   9, 4194304) /* ValidLocations */
     , (950035,  16, 1) /* ItemUseable */
     , (950035,  18, 1024) /* UiEffects */
     , (950035,  19, 400) /* Value */
     , (950035,  44, 0) /* Damage */
     , (950035,  45, 1) /* DamageType */
     , (950035,  46, 16) /* DefaultCombatStyle */
     , (950035,  48, 2) /* WeaponSkill */
     , (950035,  49, 33) /* WeaponTime */
     , (950035,  50, 1) /* AmmoType */
     , (950035,  51, 2) /* CombatUse */
     , (950035,  52, 2) /* ParentLocation */
     , (950035,  53, 3) /* PlacementPosition */
     , (950035,  60, 192) /* WeaponRange */
     , (950035,  93, 1044) /* PhysicsState */
     , (950035, 105, 1) /* ItemWorkmanship */
     , (950035, 131, 75) /* MaterialType */
     , (950035, 150, 103) /* HookPlacement */
     , (950035, 151, 2) /* HookType */
     , (950035, 158, 2) /* WieldRequirements */
     , (950035, 159, 2) /* WieldSkillType */
     , (950035, 160, 335) /* WieldDifficulty */
     , (950035, 169, 101187850) /* TsysMutationData */
     , (950035, 204, 8) /* ElementalDamageBonus */
     , (950035, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950035,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950035,  26, 27.3) /* MaximumVelocity */
     , (950035,  29, 1.15) /* WeaponDefense */
     , (950035,  39, 1.1) /* DefaultScale */
     , (950035,  62, 1.0) /* WeaponOffense */
     , (950035,  63, 2.3) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950035,   1, 'Admin Test Bow 335 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950035,   1, 0x020011F4) /* Setup */
     , (950035,   3, 0x20000014) /* SoundTable */
     , (950035,   6, 0x0400196D) /* PaletteBase */
     , (950035,   7, 0x10000589) /* ClothingBase */
     , (950035,   8, 0x0600158F) /* Icon */
     , (950035,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950035,  36, 0x0E00001D) /* MutateFilter */
     , (950035,  46, 0x38000047) /* TsysMutationFilter */;
