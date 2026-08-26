DELETE FROM `weenie` WHERE `class_Id` = 480642;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (480642, 'ace480642-racialreqmorphgem', 38, '2026-08-10 00:00:00') /* Gem */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (480642,   1,       2048) /* ItemType - Gem */
     , (480642,   5,         10) /* EncumbranceVal */
     , (480642,  11,          1) /* MaxStackSize */
     , (480642,  12,          1) /* StackSize */
     , (480642,  13,         10) /* StackUnitEncumbrance */
     , (480642,  15,         30) /* StackUnitValue - costs 30 Phials of Bloody Tears at Darkbeat */
     , (480642,  16,     524296) /* ItemUseable - SourceContainedTargetContained */
     , (480642,  18,          1) /* UiEffects - Magical */
     , (480642,  19,         30) /* Value - costs 30 Phials of Bloody Tears at Darkbeat */
     , (480642,  65,        101) /* Placement - Resting */
     , (480642,  93,       1044) /* PhysicsState - Ethereal, IgnoreCollisions, Gravity */
     , (480642,  94,      35215) /* TargetType - Vestements */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (480642,   1, False) /* Stuck */
     , (480642,  11, True ) /* IgnoreCollisions */
     , (480642,  13, True ) /* Ethereal */
     , (480642,  14, True ) /* GravityStatus */
     , (480642,  19, True ) /* Attackable */
     , (480642,  22, True ) /* Inscribable */
     , (480642,  69, False) /* IsSellable */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (480642,   1, 'Racial Requirement Morph Gem') /* Name */
     , (480642,  14, 'Applying this gem to armor, a weapon or a magic caster will remove its racial requirement, leaving the item with no racial restriction at all. This removes both the racial activation requirement on the item''s spells and any racial wield requirement.') /* Use */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (480642,   1, 0x02000179) /* Setup */
     , (480642,   3, 0x20000014) /* SoundTable */
     , (480642,   6, 0x04000BEF) /* PaletteBase */
     , (480642,   7, 0x1000010B) /* ClothingBase */
     , (480642,  22, 0x3400002B) /* PhysicsEffectTable */
     , (480642,   8, 0x06002971) /* Icon */
     , (480642,  50, 0x060026FD) /* IconOverlay */
     , (480642,  52, 0x060065FB) /* IconUnderlay */;
