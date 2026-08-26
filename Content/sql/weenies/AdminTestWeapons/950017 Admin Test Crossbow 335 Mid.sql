/* Admin Test Crossbow 335 Mid -- clone of wcid 29251 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950017;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950017, 'ace950017-admintestcrossbow335mid', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950017,   1, 256) /* ItemType */
     , (950017,   3, 20) /* PaletteTemplate */
     , (950017,   5, 1920) /* EncumbranceVal */
     , (950017,   8, 640) /* Mass */
     , (950017,   9, 4194304) /* ValidLocations */
     , (950017,  16, 1) /* ItemUseable */
     , (950017,  18, 1024) /* UiEffects */
     , (950017,  19, 375) /* Value */
     , (950017,  44, 0) /* Damage */
     , (950017,  45, 1) /* DamageType */
     , (950017,  46, 32) /* DefaultCombatStyle */
     , (950017,  48, 3) /* WeaponSkill */
     , (950017,  49, 102) /* WeaponTime */
     , (950017,  50, 2) /* AmmoType */
     , (950017,  51, 2) /* CombatUse */
     , (950017,  52, 2) /* ParentLocation */
     , (950017,  53, 3) /* PlacementPosition */
     , (950017,  60, 192) /* WeaponRange */
     , (950017,  93, 1044) /* PhysicsState */
     , (950017, 105, 1) /* ItemWorkmanship */
     , (950017, 131, 75) /* MaterialType */
     , (950017, 150, 103) /* HookPlacement */
     , (950017, 151, 2) /* HookType */
     , (950017, 158, 2) /* WieldRequirements */
     , (950017, 159, 3) /* WieldSkillType */
     , (950017, 160, 335) /* WieldDifficulty */
     , (950017, 169, 101189386) /* TsysMutationData */
     , (950017, 204, 6) /* ElementalDamageBonus */
     , (950017, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950017,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950017,  26, 27.3) /* MaximumVelocity */
     , (950017,  29, 1.11) /* WeaponDefense */
     , (950017,  39, 1.25) /* DefaultScale */
     , (950017,  62, 1.0) /* WeaponOffense */
     , (950017,  63, 2.5) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950017,   1, 'Admin Test Crossbow 335 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950017,   1, 0x020012C2) /* Setup */
     , (950017,   3, 0x20000014) /* SoundTable */
     , (950017,   6, 0x0400196D) /* PaletteBase */
     , (950017,   7, 0x100005A7) /* ClothingBase */
     , (950017,   8, 0x060015A3) /* Icon */
     , (950017,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950017,  36, 0x0E00001D) /* MutateFilter */
     , (950017,  46, 0x38000048) /* TsysMutationFilter */;
