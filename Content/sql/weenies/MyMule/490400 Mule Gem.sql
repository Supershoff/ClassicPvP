DELETE FROM `weenie` WHERE `class_Id` = 490400;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (490400, 'ace490400-mule', 38, '2026-08-11 00:00:00') /* Gem */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (490400,   1,    2048) /* ItemType - Gem */
     , (490400,   5,       0) /* EncumbranceVal */
     , (490400,  16,       8) /* ItemUseable - Contained */
     , (490400,  18,       1) /* UiEffects - Magical */
     , (490400,  19,       0) /* Value */
     , (490400,  33,       1) /* Bonded - Bonded */
     , (490400,  63,       1) /* UnlimitedUse */
     , (490400,  93,    1044) /* PhysicsState - Ethereal, IgnoreCollisions, Gravity */
     , (490400, 114,       1) /* Attuned - Attuned */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (490400,   1, False) /* Stuck */
     , (490400,  11, True ) /* IgnoreCollisions */
     , (490400,  13, True ) /* Ethereal */
     , (490400,  14, True ) /* GravityStatus */
     , (490400,  19, True ) /* Attackable */
     , (490400,  63, True ) /* UnlimitedUse */
     , (490400,  69, False) /* IsSellable */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (490400,   1, 'My Mule') /* Name */
     , (490400,  16, 'Summons your personal mule -- a private storage vendor, shared account-wide across every character on your account, that only you can deposit to or withdraw from. It disappears if you leave the landblock, and can only be summoned on a landblock with player housing. Reusable.') /* LongDesc */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (490400,   1, 0x02000179) /* Setup */
     , (490400,   3, 0x20000014) /* SoundTable */
     , (490400,   6, 0x04000BEF) /* PaletteBase */
     , (490400,   8, 100674444) /* Icon - Gem of Greater Protection */
     , (490400,  22, 0x3400002B) /* PhysicsEffectTable */
     , (490400,  27, 0x13000081) /* UseUserAnimation - MimeEat */
     , (490400,  52, 0x06005B0C) /* IconUnderlay */;
