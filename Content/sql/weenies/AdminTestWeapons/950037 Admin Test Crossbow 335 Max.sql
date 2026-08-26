/* Admin Test Crossbow 335 Max -- clone of wcid 29251 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950037;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950037, 'ace950037-admintestcrossbow335max', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950037,   1, 256) /* ItemType */
     , (950037,   3, 20) /* PaletteTemplate */
     , (950037,   5, 1920) /* EncumbranceVal */
     , (950037,   8, 640) /* Mass */
     , (950037,   9, 4194304) /* ValidLocations */
     , (950037,  16, 1) /* ItemUseable */
     , (950037,  18, 1024) /* UiEffects */
     , (950037,  19, 375) /* Value */
     , (950037,  44, 0) /* Damage */
     , (950037,  45, 1) /* DamageType */
     , (950037,  46, 32) /* DefaultCombatStyle */
     , (950037,  48, 3) /* WeaponSkill */
     , (950037,  49, 90) /* WeaponTime */
     , (950037,  50, 2) /* AmmoType */
     , (950037,  51, 2) /* CombatUse */
     , (950037,  52, 2) /* ParentLocation */
     , (950037,  53, 3) /* PlacementPosition */
     , (950037,  60, 192) /* WeaponRange */
     , (950037,  93, 1044) /* PhysicsState */
     , (950037, 105, 1) /* ItemWorkmanship */
     , (950037, 131, 75) /* MaterialType */
     , (950037, 150, 103) /* HookPlacement */
     , (950037, 151, 2) /* HookType */
     , (950037, 158, 2) /* WieldRequirements */
     , (950037, 159, 3) /* WieldSkillType */
     , (950037, 160, 335) /* WieldDifficulty */
     , (950037, 169, 101189386) /* TsysMutationData */
     , (950037, 204, 8) /* ElementalDamageBonus */
     , (950037, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950037,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950037,  26, 27.3) /* MaximumVelocity */
     , (950037,  29, 1.15) /* WeaponDefense */
     , (950037,  39, 1.25) /* DefaultScale */
     , (950037,  62, 1.0) /* WeaponOffense */
     , (950037,  63, 2.55) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950037,   1, 'Admin Test Crossbow 335 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950037,   1, 0x020012C2) /* Setup */
     , (950037,   3, 0x20000014) /* SoundTable */
     , (950037,   6, 0x0400196D) /* PaletteBase */
     , (950037,   7, 0x100005A7) /* ClothingBase */
     , (950037,   8, 0x060015A3) /* Icon */
     , (950037,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950037,  36, 0x0E00001D) /* MutateFilter */
     , (950037,  46, 0x38000048) /* TsysMutationFilter */;
