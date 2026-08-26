using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using ACE.Common;
using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.WorldObjects;

namespace ACE.Server.Tests
{
    /// <summary>
    /// Regression coverage for issue #1 ("[My Mule] Prevent vendor lifecycle from destroying
    /// persistent storage items"): a mule vendor shell's DefaultItemsForSale/UniqueItemsForSale
    /// dictionaries hold borrowed references into a player's persistent mule storage containers
    /// (see Player_Mule.cs), never inventory the shell itself owns. Generic vendor destruction
    /// (WorldObject.Destroy()) must never cascade-destroy those items, no matter which path
    /// triggers it.
    /// <para/>
    /// These tests exercise the shared lifecycle primitives -- the mule guard in
    /// WorldObject.Destroy() and Player.TeardownMuleShell, the single method every mule
    /// disposal path (dismiss, a failed EnterWorld() during spawn, owner logout, owner landblock
    /// departure) now goes through -- directly at the WorldObject level, rather than driving a
    /// live Player.SpawnMuleVendor() call end to end. Player is tightly coupled to a real
    /// network Session (Player_Networking.SendTransientError etc. call straight into
    /// Session.Network.EnqueueSend with no substitutable seam), so reproducing the bug through
    /// the full summon flow would require a live game client connection. Testing at this
    /// boundary instead proves the invariant holds for every caller -- including any future one
    /// -- which is exactly what a defense-in-depth guard is for.
    /// <para/>
    /// Before the fix, MuleVendorDestroy_DoesNotDestroyBorrowedStorageItems and
    /// TeardownMuleShell_DestroysShellButNotBorrowedStorageItems both fail: the unconditional
    /// vendor cascade in WorldObject.Destroy() destroyed every item referenced by
    /// UniqueItemsForSale, mule or not.
    /// <para/>
    /// Like the rest of ACE.Server.Tests (see StartupTests), these require a configured
    /// Config.js and a reachable shard/world database -- constructing a Vendor always looks up
    /// its currency weenie (Vendor.ValidateVendorRequirements), and destroying any object with a
    /// dynamic guid recycles it via GuidManager, which is backed by the shard database.
    /// </summary>
    [TestClass]
    public class MuleVendorLifecycleTests
    {
        private static uint nextTestGuid = ObjectGuid.DynamicMin;

        [ClassInitialize]
        public static void TestSetup(TestContext context)
        {
            // copy config.js and initialize configuration
            File.Copy(Path.Combine(Environment.CurrentDirectory, "..\\..\\..\\..\\..\\ACE.Server\\Config.js"), ".\\Config.js", true);
            ConfigManager.Initialize();
            DatabaseManager.Initialize();
        }

        private static ObjectGuid NextGuid() => new ObjectGuid(nextTestGuid++);

        private static Container CreateTestContainer()
        {
            var weenie = new Weenie
            {
                WeenieClassId = 90000101,
                ClassName = "muletest_container",
                WeenieType = WeenieType.Container
            };

            return new Container(weenie, NextGuid());
        }

        private static WorldObject CreateTestItem()
        {
            var weenie = new Weenie
            {
                WeenieClassId = 90000102,
                ClassName = "muletest_item",
                WeenieType = WeenieType.Generic
            };

            return new GenericObject(weenie, NextGuid());
        }

        private static Vendor CreateTestVendor()
        {
            var weenie = new Weenie
            {
                WeenieClassId = 90000103,
                ClassName = "muletest_vendor",
                WeenieType = WeenieType.Vendor
            };

            return new Vendor(weenie, NextGuid());
        }

        /// <summary>
        /// Deposits a test item directly into a test container's Inventory, mirroring what a
        /// persisted mule storage container looks like once items have been deposited into it
        /// (Player_Mule.AddToMuleStorage). Bypasses Container.TryAddToInventory, which does a lot
        /// of burden/placement bookkeeping this test doesn't need.
        /// </summary>
        private static void DepositIntoContainer(Container container, WorldObject item)
        {
            container.Inventory.Add(item.Guid, item);
            item.ContainerId = container.Guid.Full;
        }

        /// <summary>
        /// Invokes the private Player.TeardownMuleShell(Vendor) -- the single method
        /// DespawnMule() and SpawnMuleVendor()'s EnterWorld() failure branch both call to get rid
        /// of a mule shell -- via reflection, since it's an implementation-private abstraction
        /// with no public caller that doesn't also require a live Player/Session.
        /// </summary>
        private static void InvokeTeardownMuleShell(Vendor vendor)
        {
            var method = typeof(Player).GetMethod("TeardownMuleShell", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "Player.TeardownMuleShell(Vendor) was not found -- has it been renamed or removed?");

            method.Invoke(null, new object[] { vendor });
        }

        [TestMethod]
        public void MuleVendorDestroy_DoesNotDestroyBorrowedStorageItems()
        {
            // Arrange: a persisted mule storage container with two items, and a mule vendor
            // shell whose UniqueItemsForSale references them -- exactly what
            // Player_Mule.SpawnMuleVendor() sets up before calling vendor.EnterWorld().
            var container = CreateTestContainer();
            var item1 = CreateTestItem();
            var item2 = CreateTestItem();
            DepositIntoContainer(container, item1);
            DepositIntoContainer(container, item2);

            var vendor = CreateTestVendor();
            vendor.MuleOwnerId = 1;
            vendor.MuleContainers = new List<Container> { container };
            vendor.UniqueItemsForSale[item1.Guid] = item1;
            vendor.UniqueItemsForSale[item2.Guid] = item2;

            // Act: destroy the mule shell directly, the way a missed/future caller (or the old,
            // buggy SpawnMuleVendor()) would -- with no explicit dictionary-clearing first.
            vendor.Destroy();

            // Assert: the shell itself is gone, but its borrowed storage items and their real
            // container are completely untouched.
            Assert.IsTrue(vendor.IsDestroyed);

            Assert.IsFalse(item1.IsDestroyed);
            Assert.IsFalse(item2.IsDestroyed);
            Assert.IsFalse(container.IsDestroyed);

            Assert.IsTrue(container.Inventory.ContainsKey(item1.Guid));
            Assert.IsTrue(container.Inventory.ContainsKey(item2.Guid));
            Assert.AreSame(item1, container.Inventory[item1.Guid]);
            Assert.AreSame(item2, container.Inventory[item2.Guid]);
        }

        [TestMethod]
        public void NonMuleVendorDestroy_StillDestroysOwnedSaleItems()
        {
            // Regression guard: a normal (non-mule) vendor's sale items are inventory it
            // actually owns, and must still be cleaned up on destruction exactly as before.
            var vendor = CreateTestVendor();
            Assert.IsFalse(vendor.MuleOwnerId.HasValue);

            var stockItem = CreateTestItem();
            vendor.DefaultItemsForSale.Add(stockItem.Guid, stockItem);

            vendor.Destroy();

            Assert.IsTrue(vendor.IsDestroyed);
            Assert.IsTrue(stockItem.IsDestroyed);
        }

        [TestMethod]
        public void TeardownMuleShell_DestroysShellButNotBorrowedStorageItems()
        {
            // Arrange, matching the EnterWorld()-failure branch of SpawnMuleVendor() and the
            // normal DespawnMule() path (dismiss / owner logout / owner landblock departure all
            // funnel through DespawnMule(), which now delegates straight to this method).
            var container = CreateTestContainer();
            var item = CreateTestItem();
            DepositIntoContainer(container, item);

            var vendor = CreateTestVendor();
            vendor.MuleOwnerId = 1;
            vendor.MuleContainers = new List<Container> { container };
            vendor.UniqueItemsForSale[item.Guid] = item;

            InvokeTeardownMuleShell(vendor);

            // The shell is torn all the way down, including its now-pointless display state...
            Assert.IsTrue(vendor.IsDestroyed);
            Assert.AreEqual(0, vendor.DefaultItemsForSale.Count);
            Assert.AreEqual(0, vendor.UniqueItemsForSale.Count);

            // ...but the persistent storage behind it never was.
            Assert.IsFalse(item.IsDestroyed);
            Assert.IsFalse(container.IsDestroyed);
            Assert.IsTrue(container.Inventory.ContainsKey(item.Guid));
            Assert.AreSame(item, container.Inventory[item.Guid]);
        }
    }
}
