DELETE FROM `weenie` WHERE `class_Id` = 490050;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (490050, 'ace490050-LesserImpenetrabilitymorphgem', 38, '2022-01-29 01:15:03') /* Gem */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
VALUES (490050,   1,       2048) /* ItemType - Gem */
     , (490050,   5,         10) /* EncumbranceVal */
     , (490050,  16,     524296) /* ItemUseable - SourceContainedTargetContained */
     , (490050,  18,          1) /* UiEffects - Magical */
     , (490050,  19,         30) /* Value */
     , (490050,  65,        101) /* Placement - Resting */
     , (490050,  93,       1044) /* PhysicsState - Ethereal, IgnoreCollisions, Gravity */
     , (490050,  94,          35215) /* TargetType - Vestements */;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
VALUES (490050,   1, False) /* Stuck */
     , (490050,  11, True ) /* IgnoreCollisions */
     , (490050,  13, True ) /* Ethereal */
     , (490050,  14, True ) /* GravityStatus */
     , (490050,  19, True ) /* Attackable */
     , (490050,  22, True ) /* Inscribable */
     , (490050,  69, False) /* IsSellable */;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (490050,   1, 'Lesser Impenetrability Morph Gem') /* Name */
     , (490050,  14, 'Applying this gem to loot generated armor will add Minor Impenetrability with a small chance to upgrade that to Major Impenetrability.') /* Use */;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
VALUES (490050,   1, 0x02000179) /* Setup */
     , (490050,   3, 0x20000014) /* SoundTable */
     , (490050,   6, 0x04000BEF) /* PaletteBase */
     , (490050,   7, 0x1000010B) /* ClothingBase */
     , (490050,  22, 0x3400002B) /* PhysicsEffectTable */
     , (490050,  8, 100668271) /* Icon */
     , (490050,  52, 0x06005B0C) /* IconUnderlay */;

/* Lifestoned Changelog:
{
  "Changelog": [
    {
      "created": "2022-01-17T02:18:55.5489445Z",
      "author": "ACE.Adapter",
      "comment": "Weenie exported from ACEmulator world database using ACE.Adapter"
    }
  ],
  "IsDone": false
}
*/
