/* Admin Test Crossbow 315 Max -- clone of wcid 29251 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950056;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950056, 'ace950056-admintestcrossbow315max', 3, '2026-08-16 00:00:00') /* MissileLauncher */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950056,   1, 256) /* ItemType */
     , (950056,   3, 20) /* PaletteTemplate */
     , (950056,   5, 1920) /* EncumbranceVal */
     , (950056,   8, 640) /* Mass */
     , (950056,   9, 4194304) /* ValidLocations */
     , (950056,  16, 1) /* ItemUseable */
     , (950056,  18, 1024) /* UiEffects */
     , (950056,  19, 375) /* Value */
     , (950056,  44, 0) /* Damage */
     , (950056,  45, 1) /* DamageType */
     , (950056,  46, 32) /* DefaultCombatStyle */
     , (950056,  48, 3) /* WeaponSkill */
     , (950056,  49, 90) /* WeaponTime */
     , (950056,  50, 2) /* AmmoType */
     , (950056,  51, 2) /* CombatUse */
     , (950056,  52, 2) /* ParentLocation */
     , (950056,  53, 3) /* PlacementPosition */
     , (950056,  60, 192) /* WeaponRange */
     , (950056,  93, 1044) /* PhysicsState */
     , (950056, 105, 1) /* ItemWorkmanship */
     , (950056, 131, 75) /* MaterialType */
     , (950056, 150, 103) /* HookPlacement */
     , (950056, 151, 2) /* HookType */
     , (950056, 158, 2) /* WieldRequirements */
     , (950056, 159, 3) /* WieldSkillType */
     , (950056, 160, 315) /* WieldDifficulty */
     , (950056, 169, 101189386) /* TsysMutationData */
     , (950056, 204, 4) /* ElementalDamageBonus */
     , (950056, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950056,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950056,  26, 27.3) /* MaximumVelocity */
     , (950056,  29, 1.15) /* WeaponDefense */
     , (950056,  39, 1.25) /* DefaultScale */
     , (950056,  62, 1.0) /* WeaponOffense */
     , (950056,  63, 2.55) /* DamageMod */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950056,   1, 'Admin Test Crossbow 315 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950056,   1, 0x020012C2) /* Setup */
     , (950056,   3, 0x20000014) /* SoundTable */
     , (950056,   6, 0x0400196D) /* PaletteBase */
     , (950056,   7, 0x100005A7) /* ClothingBase */
     , (950056,   8, 0x060015A3) /* Icon */
     , (950056,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950056,  36, 0x0E00001D) /* MutateFilter */
     , (950056,  46, 0x38000048) /* TsysMutationFilter */;
