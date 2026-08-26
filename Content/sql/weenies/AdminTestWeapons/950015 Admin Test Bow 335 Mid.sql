/* Admin Test Bow 335 Mid -- clone of wcid 29244 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950015;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950015, 'ace950015-admintestbow335mid', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950015,   1, 256) /* ItemType */
     , (950015,   3, 20) /* PaletteTemplate */
     , (950015,   5, 980) /* EncumbranceVal */
     , (950015,   8, 140) /* Mass */
     , (950015,   9, 4194304) /* ValidLocations */
     , (950015,  16, 1) /* ItemUseable */
     , (950015,  18, 1024) /* UiEffects */
     , (950015,  19, 400) /* Value */
     , (950015,  44, 0) /* Damage */
     , (950015,  45, 1) /* DamageType */
     , (950015,  46, 16) /* DefaultCombatStyle */
     , (950015,  48, 2) /* WeaponSkill */
     , (950015,  49, 38) /* WeaponTime */
     , (950015,  50, 1) /* AmmoType */
     , (950015,  51, 2) /* CombatUse */
     , (950015,  52, 2) /* ParentLocation */
     , (950015,  53, 3) /* PlacementPosition */
     , (950015,  60, 192) /* WeaponRange */
     , (950015,  93, 1044) /* PhysicsState */
     , (950015, 105, 1) /* ItemWorkmanship */
     , (950015, 131, 75) /* MaterialType */
     , (950015, 150, 103) /* HookPlacement */
     , (950015, 151, 2) /* HookType */
     , (950015, 158, 2) /* WieldRequirements */
     , (950015, 159, 2) /* WieldSkillType */
     , (950015, 160, 335) /* WieldDifficulty */
     , (950015, 169, 101187850) /* TsysMutationData */
     , (950015, 204, 6) /* ElementalDamageBonus */
     , (950015, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950015,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950015,  26, 27.3) /* MaximumVelocity */
     , (950015,  29, 1.11) /* WeaponDefense */
     , (950015,  39, 1.1) /* DefaultScale */
     , (950015,  62, 1.0) /* WeaponOffense */
     , (950015,  63, 2.25) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950015,   1, 'Admin Test Bow 335 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950015,   1, 0x020011F4) /* Setup */
     , (950015,   3, 0x20000014) /* SoundTable */
     , (950015,   6, 0x0400196D) /* PaletteBase */
     , (950015,   7, 0x10000589) /* ClothingBase */
     , (950015,   8, 0x0600158F) /* Icon */
     , (950015,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950015,  36, 0x0E00001D) /* MutateFilter */
     , (950015,  46, 0x38000047) /* TsysMutationFilter */;
