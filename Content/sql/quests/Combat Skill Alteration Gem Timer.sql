/* CombatSkillAlterationGemPickedUp

   Splits combat skill respec gems onto their own pickup timer, separate from the
   14-day SkillAlterationGemPickedUp timer that non-combat skills stay on.

   Allows 3 gem pickups per 7-day window, shared across Enlightenment (spec/train)
   and Forgetfulness (unspec/untrain) and across every combat skill. The window
   opens on the first pickup, so a player may take all 3 at once and the allowance
   refreshes 7 days after that first gem.

   NOTE: max_Solves is a PER-WINDOW allowance for this quest, not a lifetime cap.
   That behavior comes from the WindowedQuests set in QuestManager.cs - the stock
   ACE reading of max_Solves would block the player permanently after 3 pickups.
   Adding a quest here without adding it to that set will NOT give windowed behavior.

   Gems repointed to this quest (24) - Content/sql/weenies/Respec:
     Forgetfulness: 22318 Axe, 22319 Bow, 22323 Crossbow, 22324 Dagger,
                    22332 Life Magic, 22335 Mace, 22343 Spear, 22344 Staff,
                    22345 Sword, 22346 Thrown Weapon, 22347 Unarmed Combat,
                    22348 War Magic
     Enlightenment: 22353 Axe, 22354 Bow, 22358 Crossbow, 22359 Dagger,
                    22367 Life Magic, 22370 Mace, 22378 Spear, 22379 Staff,
                    22380 Sword, 22381 Thrown Weapon, 22382 Unarmed Combat,
                    22383 War Magic

   Left on SkillAlterationGemPickedUp:
     22355 Cooking Gem of Enlightenment (not a combat skill)

   Left on their own quests (unchanged):
     22937-22942 Gem of Lowering  -> AttributeLoweringGemPickedUp
     22943-22948 Gem of Raising   -> AttributeRaisingGemPickedUp
*/

DELETE FROM `quest` WHERE `name` = 'CombatSkillAlterationGemPickedUp';

INSERT INTO `quest` (`name`, `min_Delta`, `max_Solves`, `message`, `last_Modified`)
VALUES ('CombatSkillAlterationGemPickedUp', 604800, 3, 'Picked up a combat skill alteration gem.', '2026-08-21 00:00:00');
