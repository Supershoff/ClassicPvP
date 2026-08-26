/* Admin Test Dagger 350 Max -- clone of wcid 22440 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950048;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950048, 'ace950048-admintestdagger350max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950048,   1, 1) /* ItemType */
     , (950048,   3, 20) /* PaletteTemplate */
     , (950048,   5, 200) /* EncumbranceVal */
     , (950048,   9, 1048576) /* ValidLocations */
     , (950048,  16, 1) /* ItemUseable */
     , (950048,  19, 100) /* Value */
     , (950048,  44, 20) /* Damage */
     , (950048,  45, 3) /* DamageType */
     , (950048,  46, 2) /* DefaultCombatStyle */
     , (950048,  47, 6) /* AttackType */
     , (950048,  48, 4) /* WeaponSkill */
     , (950048,  49, 30) /* WeaponTime */
     , (950048,  51, 1) /* CombatUse */
     , (950048,  93, 1044) /* PhysicsState */
     , (950048, 105, 1) /* ItemWorkmanship */
     , (950048, 131, 64) /* MaterialType */
     , (950048, 150, 103) /* HookPlacement */
     , (950048, 151, 2) /* HookType */
     , (950048, 158, 2) /* WieldRequirements */
     , (950048, 159, 4) /* WieldSkillType */
     , (950048, 160, 350) /* WieldDifficulty */
     , (950048, 169, 101254146) /* TsysMutationData */
     , (950048, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950048,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950048,  21, 0.4) /* WeaponLength */
     , (950048,  22, 0.3) /* DamageVariance */
     , (950048,  29, 1.15) /* WeaponDefense */
     , (950048,  62, 1.15) /* WeaponOffense */
     , (950048, 149, 1.025) /* WeaponMissileDefense */
     , (950048, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950048,   1, 'Admin Test Dagger 350 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950048,   1, 0x02000E49) /* Setup */
     , (950048,   3, 0x20000014) /* SoundTable */
     , (950048,   6, 0x04000BEF) /* PaletteBase */
     , (950048,   7, 0x10000415) /* ClothingBase */
     , (950048,   8, 0x06002900) /* Icon */
     , (950048,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950048,  36, 0x0E00001D) /* MutateFilter */
     , (950048,  46, 0x38000031) /* TsysMutationFilter */;
