/* Admin Test Bow 360 Max -- clone of wcid 29244 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950014;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950014, 'ace950014-admintestbow360max', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950014,   1, 256) /* ItemType */
     , (950014,   3, 20) /* PaletteTemplate */
     , (950014,   5, 980) /* EncumbranceVal */
     , (950014,   8, 140) /* Mass */
     , (950014,   9, 4194304) /* ValidLocations */
     , (950014,  16, 1) /* ItemUseable */
     , (950014,  18, 1024) /* UiEffects */
     , (950014,  19, 400) /* Value */
     , (950014,  44, 0) /* Damage */
     , (950014,  45, 1) /* DamageType */
     , (950014,  46, 16) /* DefaultCombatStyle */
     , (950014,  48, 2) /* WeaponSkill */
     , (950014,  49, 33) /* WeaponTime */
     , (950014,  50, 1) /* AmmoType */
     , (950014,  51, 2) /* CombatUse */
     , (950014,  52, 2) /* ParentLocation */
     , (950014,  53, 3) /* PlacementPosition */
     , (950014,  60, 192) /* WeaponRange */
     , (950014,  93, 1044) /* PhysicsState */
     , (950014, 105, 1) /* ItemWorkmanship */
     , (950014, 131, 75) /* MaterialType */
     , (950014, 150, 103) /* HookPlacement */
     , (950014, 151, 2) /* HookType */
     , (950014, 158, 2) /* WieldRequirements */
     , (950014, 159, 2) /* WieldSkillType */
     , (950014, 160, 360) /* WieldDifficulty */
     , (950014, 169, 101187850) /* TsysMutationData */
     , (950014, 204, 12) /* ElementalDamageBonus */
     , (950014, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950014,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950014,  26, 27.3) /* MaximumVelocity */
     , (950014,  29, 1.15) /* WeaponDefense */
     , (950014,  39, 1.1) /* DefaultScale */
     , (950014,  62, 1.0) /* WeaponOffense */
     , (950014,  63, 2.3) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950014,   1, 'Admin Test Bow 360 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950014,   1, 0x020011F4) /* Setup */
     , (950014,   3, 0x20000014) /* SoundTable */
     , (950014,   6, 0x0400196D) /* PaletteBase */
     , (950014,   7, 0x10000589) /* ClothingBase */
     , (950014,   8, 0x0600158F) /* Icon */
     , (950014,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950014,  36, 0x0E00001D) /* MutateFilter */
     , (950014,  46, 0x38000047) /* TsysMutationFilter */;
