/* Admin Test Axe 350 Max -- clone of wcid 301 with Tier 6 loot-mutation values applied */
DELETE FROM `weenie` WHERE `class_Id` = 950042;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (950042, 'ace950042-admintestaxe350max', 6, '2026-08-16 00:00:00') /* MeleeWeapon */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (950042,   1, 1) /* ItemType */
     , (950042,   3, 20) /* PaletteTemplate */
     , (950042,   5, 800) /* EncumbranceVal */
     , (950042,   8, 320) /* Mass */
     , (950042,   9, 1048576) /* ValidLocations */
     , (950042,  16, 1) /* ItemUseable */
     , (950042,  19, 360) /* Value */
     , (950042,  44, 38) /* Damage */
     , (950042,  45, 1) /* DamageType */
     , (950042,  46, 2) /* DefaultCombatStyle */
     , (950042,  47, 4) /* AttackType */
     , (950042,  48, 1) /* WeaponSkill */
     , (950042,  49, 45) /* WeaponTime */
     , (950042,  51, 1) /* CombatUse */
     , (950042,  93, 1044) /* PhysicsState */
     , (950042, 105, 1) /* ItemWorkmanship */
     , (950042, 131, 64) /* MaterialType */
     , (950042, 150, 103) /* HookPlacement */
     , (950042, 151, 2) /* HookType */
     , (950042, 158, 2) /* WieldRequirements */
     , (950042, 159, 1) /* WieldSkillType */
     , (950042, 160, 350) /* WieldDifficulty */
     , (950042, 169, 101189386) /* TsysMutationData */
     , (950042, 10027, 10) /* TinkerMaxCountOverride */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (950042,  22, False) /* Inscribable */;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
VALUES (950042,  21, 0.75) /* WeaponLength */
     , (950042,  22, 0.4) /* DamageVariance */
     , (950042,  29, 1.15) /* WeaponDefense */
     , (950042,  62, 1.15) /* WeaponOffense */
     , (950042, 149, 1.025) /* WeaponMissileDefense */
     , (950042, 150, 1.025) /* WeaponMagicDefense */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (950042,   1, 'Admin Test Axe 350 Max') /* Name */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (950042,   1, 0x02000125) /* Setup */
     , (950042,   3, 0x20000014) /* SoundTable */
     , (950042,   6, 0x04000BEF) /* PaletteBase */
     , (950042,   7, 0x10000143) /* ClothingBase */
     , (950042,   8, 0x06001639) /* Icon */
     , (950042,  22, 0x3400002B) /* PhysicsEffectTable */
     , (950042,  30, 0x00000058) /* PhysicsScript */
     , (950042,  36, 0x0E00001D) /* MutateFilter */
     , (950042,  46, 0x38000002) /* TsysMutationFilter */;
