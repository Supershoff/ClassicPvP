-- Shared off-world Container weenie. Instances of this are created for each account's shared
-- mule storage chain (see ShardDatabase.GetAccountMuleContainerId/SetAccountMuleContainerId and
-- the account_mule shard-DB table) and never placed in the world -- they're the durable backing
-- store for that account's mule contents, addressed purely by GUID. Each container's 255-item
-- ItemsCapacity is a hard structural ceiling, so once one fills up, a new instance of this same
-- weenie is created and linked onto the end of the chain via PropertyInstanceId
-- MuleNextContainerId (see Player_Mule.GetOrCreateMuleDepositTarget), up to
-- MuleInfo.MaxContainers (10) containers total. Cloned from an existing working container (Sack,
-- wcid 166) so it starts from known-good baseline properties, then overridden.

DELETE FROM `weenie` WHERE `class_Id` = 490408;

INSERT INTO `weenie` (`class_Id`, `class_Name`, `type`, `last_Modified`)
VALUES (490408, 'ace490408-mulestoragecontainer', 21, '2026-08-11 00:00:00') /* Container */;

INSERT INTO `weenie_properties_int` (`object_Id`, `type`, `value`)
SELECT 490408, `type`, `value` FROM `weenie_properties_int` WHERE `object_Id` = 166;

INSERT INTO `weenie_properties_bool` (`object_Id`, `type`, `value`)
SELECT 490408, `type`, `value` FROM `weenie_properties_bool` WHERE `object_Id` = 166;

INSERT INTO `weenie_properties_float` (`object_Id`, `type`, `value`)
SELECT 490408, `type`, `value` FROM `weenie_properties_float` WHERE `object_Id` = 166;

INSERT INTO `weenie_properties_d_i_d` (`object_Id`, `type`, `value`)
SELECT 490408, `type`, `value` FROM `weenie_properties_d_i_d` WHERE `object_Id` = 166;

INSERT INTO `weenie_properties_string` (`object_Id`, `type`, `value`)
VALUES (490408, 1, 'Mule Storage');

-- large capacity so structural slots are never the limiting factor -- the mule vendor's
-- MerchandiseItemTypes mask (set per-instance in Player_Mule.cs) is what actually restricts
-- what can be stored; no nested containers are ever placed inside (blocked in code too).
UPDATE `weenie_properties_int` SET `value` = 255   WHERE `object_Id` = 490408 AND `type` = 6;  /* ItemsCapacity -- byte-backed ceiling */
UPDATE `weenie_properties_int` SET `value` = 0     WHERE `object_Id` = 490408 AND `type` = 7;  /* ContainersCapacity */
