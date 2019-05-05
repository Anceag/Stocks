﻿using Stocks.Data;
using Stocks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Stocks.Services
{
    public interface IItemsService
    {
        Item AddItem(Item item, int stockId, int ownerId);
        IEnumerable<Item> GetStockItems(int stockId, int ownerId);
        bool RemoveItem(int itemId, int ownerId);
        bool MoveItem(int itemId, int stockId, int ownerId);
        ItemState AddItemState(ItemState itemState, int itemId, int ownerId);
    }

    public class ItemsService : IItemsService
    {
        private readonly StocksDbContext _db;

        public ItemsService(StocksDbContext db)
        {
            _db = db;
        }

        public Item AddItem(Item item, int stockId, int ownerId)
        {
            if (item != null && _db.UsersStocks.FirstOrDefault(us => us.StockId == stockId && us.UserId == ownerId) != null)
            {
                _db.Items.Add(item);
                _db.SaveChanges();
                _db.ItemsStocksHistory.Add(new ItemStockHistory { ItemId = item.Id, StockId = stockId, ArrivalDate = DateTime.Now });
                _db.SaveChanges();
                item.ItemsStocksHistory = null;
                return item;
            }
            return null;
        }

        public IEnumerable<Item> GetStockItems(int stockId, int ownerId)
        {
            var userStock = _db.UsersStocks.FirstOrDefault(us => us.StockId == stockId && us.UserId == ownerId);

            if (userStock == null)
                return null;

            var items = _db.ItemsStocksHistory
                .Select(ish => ish.ItemId)
                .Distinct()
                .Select(id => _db.ItemsStocksHistory
                    .FirstOrDefault(ish1 => ish1.ItemId == id &&
                        ish1.ArrivalDate == _db.ItemsStocksHistory
                            .Where(ish2 => ish2.ItemId == ish1.ItemId)
                            .Max(ish2 => ish2.ArrivalDate))
                ).Distinct()
                .Where(ish => ish.StockId == stockId)
                .ToList()
                .Select(ish =>
                {
                    var item = _db.Items.Find(ish.ItemId);
                    item.ItemsStocksHistory = null;
                    item.ItemState = null;
                    return item;
                });

            return items;
        }

        public bool RemoveItem(int itemId, int ownerId)
        {
            var itemStockHistory = _db.ItemsStocksHistory
                .FirstOrDefault(ish => ish.ItemId == itemId &&
                    ish.ArrivalDate == _db.ItemsStocksHistory
                        .Where(ish1 => ish1.ItemId == itemId)
                        .Max(ish1 => ish1.ArrivalDate));

            if (itemStockHistory == null)
                return false;

            var userStock = _db.UsersStocks.FirstOrDefault(us => us.StockId == itemStockHistory.StockId && us.UserId == ownerId);

            if (userStock == null)
                return false;

            var item = _db.Items.Find(itemStockHistory.ItemId);

            if (item == null)
            {
                return false;
            }

            _db.Items.Remove(item);
            _db.SaveChanges();
            return true;
        }

        public bool MoveItem(int itemId, int stockId, int ownerId)
        {
            var itemStockHistory = _db.ItemsStocksHistory
                .FirstOrDefault(ish => ish.ItemId == itemId &&
                    ish.ArrivalDate == _db.ItemsStocksHistory
                        .Where(ish1 => ish1.ItemId == itemId)
                        .Max(ish1 => ish1.ArrivalDate));

            if (itemStockHistory == null)
                return false;

            var userStock = _db.UsersStocks.FirstOrDefault(us => us.StockId == itemStockHistory.StockId && us.UserId == ownerId);

            if (userStock == null)
                return false;

            _db.ItemsStocksHistory.Add(new ItemStockHistory { ItemId = itemId, StockId = stockId, ArrivalDate = DateTime.Now });
            _db.SaveChanges();
            return true;
        }

        public ItemState AddItemState(ItemState itemState, int itemId, int ownerId)
        {
            var itemStockHistory = _db.ItemsStocksHistory
                .FirstOrDefault(ish => ish.ItemId == itemId &&
                    ish.ArrivalDate == _db.ItemsStocksHistory
                        .Where(ish1 => ish1.ItemId == itemId)
                        .Max(ish1 => ish1.ArrivalDate));

            if (itemStockHistory == null)
                return null;

            var userStock = _db.UsersStocks.FirstOrDefault(us => us.StockId == itemStockHistory.StockId && us.UserId == ownerId);

            if (userStock == null)
                return null;

            _db.ItemStates.Add(itemState);
            _db.SaveChanges();

            if (itemStockHistory.ItemStateId == null)
            {
                itemStockHistory.ItemStateId = itemState.Id;
            }
            else
            {
                var itemStockHistory1 = _db.ItemsStocksHistory.Add(new ItemStockHistory { ItemId = itemId, StockId = itemStockHistory.StockId, ArrivalDate = DateTime.Now });
                _db.SaveChanges();
                itemStockHistory1.Entity.ItemStateId = itemState.Id;
            }

            _db.SaveChanges();

            itemState.ItemStockHistory = null;
            return itemState;
        }
    }
}