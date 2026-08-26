DELETE FROM `weenie` WHERE `class_Id` = 480643;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (480643, 'ace480643-allegiancereqmorphgem', 38, '2026-08-13 00:00:00') /* Gem */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (480643,   1,       2048) /* ItemType - Gem */
     , (480643,   5,         10) /* EncumbranceVal */
     , (480643,  11,          1) /* MaxStackSize */
     , (480643,  12,          1) /* StackSize */
     , (480643,  13,         10) /* StackUnitEncumbrance */
     , (480643,  15,         30) /* StackUnitValue - costs 30 Phials of Bloody Tears at Darkbeat */
     , (480643,  16,     524296) /* ItemUseable - SourceContainedTargetContained */
     , (480643,  18,          1) /* UiEffects - Magical */
     , (480643,  19,         30) /* Value - costs 30 Phials of Bloody Tears at Darkbeat */
     , (480643,  65,        101) /* Placement - Resting */
     , (480643,  93,       1044) /* PhysicsState - Ethereal, IgnoreCollisions, Gravity */
     , (480643,  94,      35215) /* TargetType - Vestements */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (480643,   1, False) /* Stuck */
     , (480643,  11, True ) /* IgnoreCollisions */
     , (480643,  13, True ) /* Ethereal */
     , (480643,  14, True ) /* GravityStatus */
     , (480643,  19, True ) /* Attackable */
     , (480643,  22, True ) /* Inscribable */
     , (480643,  69, False) /* IsSellable */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (480643,   1, 'Allegiance Rank Requirement Morph Gem') /* Name */
     , (480643,  14, 'Applying this gem to armor, a weapon or a magic caster will remove the allegiance rank required to activate its spells, leaving the item with no allegiance rank requirement at all.') /* Use */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (480643,   1, 0x02000179) /* Setup */
     , (480643,   3, 0x20000014) /* SoundTable */
     , (480643,   6, 0x04000BEF) /* PaletteBase */
     , (480643,   7, 0x1000010B) /* ClothingBase */
     , (480643,  22, 0x3400002B) /* PhysicsEffectTable */
     , (480643,   8, 0x06002971) /* Icon */
     , (480643,  50, 0x06001F64) /* IconOverlay */
     , (480643,  52, 0x060065FB) /* IconUnderlay */;
