using ACE.Database;
using ACE.Entity.Models;
using ACE.Server.WorldObjects;
using System;

namespace ACE.Server.Network.GameEvent.Events
{
    public class GameEventApproachVendor : GameEventMessage
    {
        public GameEventApproachVendor(Session session, Vendor vendor, uint altCurrencySpent)
            : base(GameEventType.ApproachVendor, GameMessageGroup.UIQueue, session, 8192) // 5,376 is the average seen in retail pcaps, 15,272 is the max seen in retail pcaps
        {        
            Writer.Write(vendor.Guid.Full);

            // the types of items vendor will purchase
            Writer.Write((uint)vendor.MerchandiseItemTypes);
            Writer.Write((uint)vendor.MerchandiseMinValue);
            Writer.Write((uint)vendor.MerchandiseMaxValue);

            Writer.Write(Convert.ToUInt32(vendor.DealMagicalItems ?? false));

            Writer.Write((float)vendor.BuyPrice);
            Writer.Write((float)vendor.SellPrice);

            // the wcid of the alternate currency
            Writer.Write(vendor.AlternateCurrency ?? 0);

            // if this vendor accepts items as alternate currency, instead of pyreals
            if (vendor.AlternateCurrency != null)
            {
                var altCurrency = DatabaseManager.World.GetCachedWeenie(vendor.AlternateCurrency.Value);
                var pluralName = altCurrency.GetPluralName();

                // the total amount of alternate currency the player currently has.
                // NOTE: by the time this packet is built after a purchase, SpendCurrency has already
                // removed the spent currency from inventory synchronously, so GetNumInventoryItemsOfWCID
                // returns the post-purchase amount. We add altCurrencySpent back to send the
                // pre-purchase amount, because the client independently applies the inventory
                // stack-update from the purchase to this vendor count; sending the already-decremented
                // amount makes the client decrement it a second time and display double the deduction.
                var altCurrencyInInventory = (uint)session.Player.GetNumInventoryItemsOfWCID(vendor.AlternateCurrency.Value, true);
                Writer.Write(altCurrencyInInventory + altCurrencySpent);

                // the plural name of alt currency
                Writer.WriteString16L(pluralName);
            }
            else
            {
                Writer.Write(0);
                Writer.WriteString16L(string.Empty);
            }

            var numItems = vendor.DefaultItemsForSale.Count + vendor.UniqueItemsForSale.Count;

            Writer.Write(numItems);

            vendor.forEachItem((obj) =>
            {
                int stackSize = obj.VendorShopCreateListStackSize ?? obj.StackSize ?? 1; // -1 = unlimited supply

                // packed value: (stackSize & 0xFFFFFF) | (pwdType << 24)
                // pwdType: flag indicating whether the new or old PublicWeenieDesc is used; -1 = PublicWeenieDesc, 1 = OldPublicWeenieDesc; -1 always used.
                Writer.Write(stackSize & 0xFFFFFF | -1 << 24);

                // Work-around for the client not showing tinkering materials for sale on vendors: Temporarily change it's category to Misc.
                var originalItemType = obj.ItemType;
                bool isSalvage = originalItemType == ACE.Entity.Enum.ItemType.TinkeringMaterial;

                // My Mule: PromissoryNote (Trade Notes) always price at a fixed rate that ignores
                // the vendor's own BuyPrice/SellPrice (see Vendor.GetSellCost/GetBuyCost's explicit
                // PromissoryNote overrides) -- the client mirrors this locally when computing the
                // price it displays/enforces, so a mule vendor (BuyPrice/SellPrice = 0, everything
                // free) still shows a real price for trade notes specifically. Same category-swap
                // trick as the salvage work-around above neutralizes it, gated to mule vendors only
                // so real vendors keep their intended trade note pricing.
                bool isMulePromissoryNote = vendor.MuleOwnerId.HasValue && originalItemType == ACE.Entity.Enum.ItemType.PromissoryNote;

                if (isSalvage || isMulePromissoryNote)
                {
                    obj.ItemType = ACE.Entity.Enum.ItemType.Misc;
                    obj.CalculateObjDesc(); // We have to calculate this or the icon will be wrong.
                }

                obj.SerializeGameDataOnly(Writer);

                if (isSalvage || isMulePromissoryNote)
                    obj.ItemType = originalItemType; // Rollback the ItemType
            });

            Writer.Align();
        }
    }
}
