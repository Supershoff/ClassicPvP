/* Admin Test Bow 315 Mid -- clone of wcid 29244 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950055;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950055, 'ace950055-admintestbow315mid', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950055,   1, 256) /* ItemType */
     , (950055,   3, 20) /* PaletteTemplate */
     , (950055,   5, 980) /* EncumbranceVal */
     , (950055,   8, 140) /* Mass */
     , (950055,   9, 4194304) /* ValidLocations */
     , (950055,  16, 1) /* ItemUseable */
     , (950055,  18, 1024) /* UiEffects */
     , (950055,  19, 400) /* Value */
     , (950055,  44, 0) /* Damage */
     , (950055,  45, 1) /* DamageType */
     , (950055,  46, 16) /* DefaultCombatStyle */
     , (950055,  48, 2) /* WeaponSkill */
     , (950055,  49, 38) /* WeaponTime */
     , (950055,  50, 1) /* AmmoType */
     , (950055,  51, 2) /* CombatUse */
     , (950055,  52, 2) /* ParentLocation */
     , (950055,  53, 3) /* PlacementPosition */
     , (950055,  60, 192) /* WeaponRange */
     , (950055,  93, 1044) /* PhysicsState */
     , (950055, 105, 1) /* ItemWorkmanship */
     , (950055, 131, 75) /* MaterialType */
     , (950055, 150, 103) /* HookPlacement */
     , (950055, 151, 2) /* HookType */
     , (950055, 158, 2) /* WieldRequirements */
     , (950055, 159, 2) /* WieldSkillType */
     , (950055, 160, 315) /* WieldDifficulty */
     , (950055, 169, 101187850) /* TsysMutationData */
     , (950055, 204, 2) /* ElementalDamageBonus */
     , (950055, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950055,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950055,  26, 27.3) /* MaximumVelocity */
     , (950055,  29, 1.11) /* WeaponDefense */
     , (950055,  39, 1.1) /* DefaultScale */
     , (950055,  62, 1.0) /* WeaponOffense */
     , (950055,  63, 2.25) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950055,   1, 'Admin Test Bow 315 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950055,   1, 0x020011F4) /* Setup */
     , (950055,   3, 0x20000014) /* SoundTable */
     , (950055,   6, 0x0400196D) /* PaletteBase */
     , (950055,   7, 0x10000589) /* ClothingBase */
     , (950055,   8, 0x0600158F) /* Icon */
     , (950055,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950055,  36, 0x0E00001D) /* MutateFilter */
     , (950055,  46, 0x38000047) /* TsysMutationFilter */;
