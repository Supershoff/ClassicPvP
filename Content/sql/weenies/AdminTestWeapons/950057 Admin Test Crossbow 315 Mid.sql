/* Admin Test Crossbow 315 Mid -- clone of wcid 29251 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950057;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950057, 'ace950057-admintestcrossbow315mid', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950057,   1, 256) /* ItemType */
     , (950057,   3, 20) /* PaletteTemplate */
     , (950057,   5, 1920) /* EncumbranceVal */
     , (950057,   8, 640) /* Mass */
     , (950057,   9, 4194304) /* ValidLocations */
     , (950057,  16, 1) /* ItemUseable */
     , (950057,  18, 1024) /* UiEffects */
     , (950057,  19, 375) /* Value */
     , (950057,  44, 0) /* Damage */
     , (950057,  45, 1) /* DamageType */
     , (950057,  46, 32) /* DefaultCombatStyle */
     , (950057,  48, 3) /* WeaponSkill */
     , (950057,  49, 102) /* WeaponTime */
     , (950057,  50, 2) /* AmmoType */
     , (950057,  51, 2) /* CombatUse */
     , (950057,  52, 2) /* ParentLocation */
     , (950057,  53, 3) /* PlacementPosition */
     , (950057,  60, 192) /* WeaponRange */
     , (950057,  93, 1044) /* PhysicsState */
     , (950057, 105, 1) /* ItemWorkmanship */
     , (950057, 131, 75) /* MaterialType */
     , (950057, 150, 103) /* HookPlacement */
     , (950057, 151, 2) /* HookType */
     , (950057, 158, 2) /* WieldRequirements */
     , (950057, 159, 3) /* WieldSkillType */
     , (950057, 160, 315) /* WieldDifficulty */
     , (950057, 169, 101189386) /* TsysMutationData */
     , (950057, 204, 2) /* ElementalDamageBonus */
     , (950057, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950057,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950057,  26, 27.3) /* MaximumVelocity */
     , (950057,  29, 1.11) /* WeaponDefense */
     , (950057,  39, 1.25) /* DefaultScale */
     , (950057,  62, 1.0) /* WeaponOffense */
     , (950057,  63, 2.5) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950057,   1, 'Admin Test Crossbow 315 Mid') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950057,   1, 0x020012C2) /* Setup */
     , (950057,   3, 0x20000014) /* SoundTable */
     , (950057,   6, 0x0400196D) /* PaletteBase */
     , (950057,   7, 0x100005A7) /* ClothingBase */
     , (950057,   8, 0x060015A3) /* Icon */
     , (950057,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950057,  36, 0x0E00001D) /* MutateFilter */
     , (950057,  46, 0x38000048) /* TsysMutationFilter */;
