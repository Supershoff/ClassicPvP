/* Admin Test Bow 315 Max -- clone of wcid 29244 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950054;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950054, 'ace950054-admintestbow315max', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950054,   1, 256) /* ItemType */
     , (950054,   3, 20) /* PaletteTemplate */
     , (950054,   5, 980) /* EncumbranceVal */
     , (950054,   8, 140) /* Mass */
     , (950054,   9, 4194304) /* ValidLocations */
     , (950054,  16, 1) /* ItemUseable */
     , (950054,  18, 1024) /* UiEffects */
     , (950054,  19, 400) /* Value */
     , (950054,  44, 0) /* Damage */
     , (950054,  45, 1) /* DamageType */
     , (950054,  46, 16) /* DefaultCombatStyle */
     , (950054,  48, 2) /* WeaponSkill */
     , (950054,  49, 33) /* WeaponTime */
     , (950054,  50, 1) /* AmmoType */
     , (950054,  51, 2) /* CombatUse */
     , (950054,  52, 2) /* ParentLocation */
     , (950054,  53, 3) /* PlacementPosition */
     , (950054,  60, 192) /* WeaponRange */
     , (950054,  93, 1044) /* PhysicsState */
     , (950054, 105, 1) /* ItemWorkmanship */
     , (950054, 131, 75) /* MaterialType */
     , (950054, 150, 103) /* HookPlacement */
     , (950054, 151, 2) /* HookType */
     , (950054, 158, 2) /* WieldRequirements */
     , (950054, 159, 2) /* WieldSkillType */
     , (950054, 160, 315) /* WieldDifficulty */
     , (950054, 169, 101187850) /* TsysMutationData */
     , (950054, 204, 4) /* ElementalDamageBonus */
     , (950054, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950054,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950054,  26, 27.3) /* MaximumVelocity */
     , (950054,  29, 1.15) /* WeaponDefense */
     , (950054,  39, 1.1) /* DefaultScale */
     , (950054,  62, 1.0) /* WeaponOffense */
     , (950054,  63, 2.3) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950054,   1, 'Admin Test Bow 315 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950054,   1, 0x020011F4) /* Setup */
     , (950054,   3, 0x20000014) /* SoundTable */
     , (950054,   6, 0x0400196D) /* PaletteBase */
     , (950054,   7, 0x10000589) /* ClothingBase */
     , (950054,   8, 0x0600158F) /* Icon */
     , (950054,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950054,  36, 0x0E00001D) /* MutateFilter */
     , (950054,  46, 0x38000047) /* TsysMutationFilter */;
