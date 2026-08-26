/* Admin Test Bow 360 Mid -- clone of wcid 29244 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950034;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950034, 'ace950034-admintestbow360mid', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950034,   1, 256) /* ItemType */
     , (950034,   3, 20) /* PaletteTemplate */
     , (950034,   5, 980) /* EncumbranceVal */
     , (950034,   8, 140) /* Mass */
     , (950034,   9, 4194304) /* ValidLocations */
     , (950034,  16, 1) /* ItemUseable */
     , (950034,  18, 1024) /* UiEffects */
     , (950034,  19, 400) /* Value */
     , (950034,  44, 0) /* Damage */
     , (950034,  45, 1) /* DamageType */
     , (950034,  46, 16) /* DefaultCombatStyle */
     , (950034,  48, 2) /* WeaponSkill */
     , (950034,  49, 38) /* WeaponTime */
     , (950034,  50, 1) /* AmmoType */
     , (950034,  51, 2) /* CombatUse */
     , (950034,  52, 2) /* ParentLocation */
     , (950034,  53, 3) /* PlacementPosition */
     , (950034,  60, 192) /* WeaponRange */
     , (950034,  93, 1044) /* PhysicsState */
     , (950034, 105, 1) /* ItemWorkmanship */
     , (950034, 131, 75) /* MaterialType */
     , (950034, 150, 103) /* HookPlacement */
     , (950034, 151, 2) /* HookType */
     , (950034, 158, 2) /* WieldRequirements */
     , (950034, 159, 2) /* WieldSkillType */
     , (950034, 160, 360) /* WieldDifficulty */
     , (950034, 169, 101187850) /* TsysMutationData */
     , (950034, 204, 10) /* ElementalDamageBonus */
     , (950034, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950034,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950034,  26, 27.3) /* MaximumVelocity */
     , (950034,  29, 1.11) /* WeaponDefense */
     , (950034,  39, 1.1) /* DefaultScale */
     , (950034,  62, 1.0) /* WeaponOffense */
     , (950034,  63, 2.25) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950034,   1, 'Admin Test Bow 360 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950034,   1, 0x020011F4) /* Setup */
     , (950034,   3, 0x20000014) /* SoundTable */
     , (950034,   6, 0x0400196D) /* PaletteBase */
     , (950034,   7, 0x10000589) /* ClothingBase */
     , (950034,   8, 0x0600158F) /* Icon */
     , (950034,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950034,  36, 0x0E00001D) /* MutateFilter */
     , (950034,  46, 0x38000047) /* TsysMutationFilter */;
