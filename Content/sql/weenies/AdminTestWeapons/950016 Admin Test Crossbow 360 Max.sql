/* Admin Test Crossbow 360 Max -- clone of wcid 29251 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950016;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950016, 'ace950016-admintestcrossbow360max', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950016,   1, 256) /* ItemType */
     , (950016,   3, 20) /* PaletteTemplate */
     , (950016,   5, 1920) /* EncumbranceVal */
     , (950016,   8, 640) /* Mass */
     , (950016,   9, 4194304) /* ValidLocations */
     , (950016,  16, 1) /* ItemUseable */
     , (950016,  18, 1024) /* UiEffects */
     , (950016,  19, 375) /* Value */
     , (950016,  44, 0) /* Damage */
     , (950016,  45, 1) /* DamageType */
     , (950016,  46, 32) /* DefaultCombatStyle */
     , (950016,  48, 3) /* WeaponSkill */
     , (950016,  49, 90) /* WeaponTime */
     , (950016,  50, 2) /* AmmoType */
     , (950016,  51, 2) /* CombatUse */
     , (950016,  52, 2) /* ParentLocation */
     , (950016,  53, 3) /* PlacementPosition */
     , (950016,  60, 192) /* WeaponRange */
     , (950016,  93, 1044) /* PhysicsState */
     , (950016, 105, 1) /* ItemWorkmanship */
     , (950016, 131, 75) /* MaterialType */
     , (950016, 150, 103) /* HookPlacement */
     , (950016, 151, 2) /* HookType */
     , (950016, 158, 2) /* WieldRequirements */
     , (950016, 159, 3) /* WieldSkillType */
     , (950016, 160, 360) /* WieldDifficulty */
     , (950016, 169, 101189386) /* TsysMutationData */
     , (950016, 204, 12) /* ElementalDamageBonus */
     , (950016, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950016,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950016,  26, 27.3) /* MaximumVelocity */
     , (950016,  29, 1.15) /* WeaponDefense */
     , (950016,  39, 1.25) /* DefaultScale */
     , (950016,  62, 1.0) /* WeaponOffense */
     , (950016,  63, 2.55) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950016,   1, 'Admin Test Crossbow 360 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950016,   1, 0x020012C2) /* Setup */
     , (950016,   3, 0x20000014) /* SoundTable */
     , (950016,   6, 0x0400196D) /* PaletteBase */
     , (950016,   7, 0x100005A7) /* ClothingBase */
     , (950016,   8, 0x060015A3) /* Icon */
     , (950016,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950016,  36, 0x0E00001D) /* MutateFilter */
     , (950016,  46, 0x38000048) /* TsysMutationFilter */;
