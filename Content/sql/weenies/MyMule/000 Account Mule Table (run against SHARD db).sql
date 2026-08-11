-- My Mule: one row per account, shared by every character on that account.
--
-- IMPORTANT: unlike every other .sql file in this folder, this is SHARD-database schema, not
-- world-database weenie content. Run this against your ace_shard database (e.g.
-- ace_shard_classicpvp), not ace_world.
--
-- container_Id is the head GUID of that account's mule storage container chain (0 = no mule
-- summoned yet by anyone on the account). visual_Variant is the sticky monster-race look index
-- (see Entity.MuleInfo.VisualVariantSourceWcids) rolled the first time any character on the
-- account summons the mule (-1 = not rolled yet). See ACE.Database.Models.Shard.AccountMule /
-- ShardDatabase.GetAccountMuleContainerId, SetAccountMuleContainerId,
-- GetAccountMuleVisualVariant, SetAccountMuleVisualVariant.

CREATE TABLE IF NOT EXISTS `account_mule` (
    `account_Id`     INT UNSIGNED NOT NULL,
    `container_Id`   INT UNSIGNED NOT NULL DEFAULT 0,
    `visual_Variant` INT          NOT NULL DEFAULT -1,
    PRIMARY KEY (`account_Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
