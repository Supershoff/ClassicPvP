DELETE FROM `weenie` WHERE `class_Id` = 49090101;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (49090101, 'ace49090101-skillattrresetgem', 38, '2026-06-26 00:00:00') /* Gem */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (49090101,   1,    2048) /* ItemType - Gem */
     , (49090101,   5,       5) /* EncumbranceVal */
     , (49090101,  16,       8) /* ItemUseable - Contained */
     , (49090101,  18,       1) /* UiEffects - Magical */
     , (49090101,  19,      20) /* Value - costs 20 Phials of Bloody Tears from Darkbeat */
     , (49090101,  33,       1) /* Bonded - Bonded */
     , (49090101,  93,    1044) /* PhysicsState - Ethereal, IgnoreCollisions, Gravity */
     , (49090101, 114,       1) /* Attuned - Attuned */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (49090101,   1, False) /* Stuck */
     , (49090101,  11, True ) /* IgnoreCollisions */
     , (49090101,  13, True ) /* Ethereal */
     , (49090101,  14, True ) /* GravityStatus */
     , (49090101,  19, True ) /* Attackable */
     , (49090101,  69, False) /* IsSellable */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (49090101,   1, 'Skill and Attribute Reset Gem') /* Name */
     , (49090101,  16, 'This gem clears your quest stamps for the Temple of Enlightenment and the Temple of Forgetfulness, allowing you to pick up a gem. Each use costs an increasing number of PK Trophies, starting at 100 and growing exponentially.') /* LongDesc */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (49090101,   1, 0x02000179) /* Setup */
     , (49090101,   3, 0x20000014) /* SoundTable */
     , (49090101,   6, 0x04000BEF) /* PaletteBase */
     , (49090101,   8, 100692712) /* Icon */
     , (49090101,  22, 0x3400002B) /* PhysicsEffectTable */
     , (49090101,  27, 0x13000093) /* UseUserAnimation - MimeEat */
     , (49090101,  52, 0x06005B0C) /* IconUnderlay */;
