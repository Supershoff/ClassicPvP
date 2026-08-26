/* Admin Test Crossbow 360 Mid -- clone of wcid 29251 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950036;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950036, 'ace950036-admintestcrossbow360mid', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950036,   1, 256) /* ItemType */
     , (950036,   3, 20) /* PaletteTemplate */
     , (950036,   5, 1920) /* EncumbranceVal */
     , (950036,   8, 640) /* Mass */
     , (950036,   9, 4194304) /* ValidLocations */
     , (950036,  16, 1) /* ItemUseable */
     , (950036,  18, 1024) /* UiEffects */
     , (950036,  19, 375) /* Value */
     , (950036,  44, 0) /* Damage */
     , (950036,  45, 1) /* DamageType */
     , (950036,  46, 32) /* DefaultCombatStyle */
     , (950036,  48, 3) /* WeaponSkill */
     , (950036,  49, 102) /* WeaponTime */
     , (950036,  50, 2) /* AmmoType */
     , (950036,  51, 2) /* CombatUse */
     , (950036,  52, 2) /* ParentLocation */
     , (950036,  53, 3) /* PlacementPosition */
     , (950036,  60, 192) /* WeaponRange */
     , (950036,  93, 1044) /* PhysicsState */
     , (950036, 105, 1) /* ItemWorkmanship */
     , (950036, 131, 75) /* MaterialType */
     , (950036, 150, 103) /* HookPlacement */
     , (950036, 151, 2) /* HookType */
     , (950036, 158, 2) /* WieldRequirements */
     , (950036, 159, 3) /* WieldSkillType */
     , (950036, 160, 360) /* WieldDifficulty */
     , (950036, 169, 101189386) /* TsysMutationData */
     , (950036, 204, 10) /* ElementalDamageBonus */
     , (950036, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950036,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950036,  26, 27.3) /* MaximumVelocity */
     , (950036,  29, 1.11) /* WeaponDefense */
     , (950036,  39, 1.25) /* DefaultScale */
     , (950036,  62, 1.0) /* WeaponOffense */
     , (950036,  63, 2.5) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950036,   1, 'Admin Test Crossbow 360 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950036,   1, 0x020012C2) /* Setup */
     , (950036,   3, 0x20000014) /* SoundTable */
     , (950036,   6, 0x0400196D) /* PaletteBase */
     , (950036,   7, 0x100005A7) /* ClothingBase */
     , (950036,   8, 0x060015A3) /* Icon */
     , (950036,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950036,  36, 0x0E00001D) /* MutateFilter */
     , (950036,  46, 0x38000048) /* TsysMutationFilter */;
