using CatalogAPI.DTOs;
using CatalogAPI.Models;
using System.Collections.Concurrent;

namespace CatalogAPI.Services
{
    public class CatalogItemService : ICatalogItemService
    {
        private readonly ConcurrentDictionary<int, CatalogItem> _catalogItems = new();
        private int _nextId = 1;

        public CatalogItemService()
        {
            SeedData();
        }

        public Task<PagedResult<CatalogItem>> GetCatalogItemsAsync(int? categoryId = null, int pageNumber = 1, int pageSize = 10)
        {
            var query = _catalogItems.Values.AsEnumerable();

            if (categoryId.HasValue)
            {
                query = query.Where(item => item.CategoryId == categoryId.Value);
            }

            var totalCount = query.Count();
            var items = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new PagedResult<CatalogItem>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Task.FromResult(result);
        }

        public Task<CatalogItem?> GetCatalogItemByIdAsync(int id)
        {
            _catalogItems.TryGetValue(id, out var item);
            return Task.FromResult(item);
        }

        public Task<CatalogItem> CreateCatalogItemAsync(CatalogItem item)
        {
            item.Id = Interlocked.Increment(ref _nextId);
            _catalogItems[item.Id] = item;
            return Task.FromResult(item);
        }

        public Task<CatalogItem?> UpdateCatalogItemAsync(int id, CatalogItem updatedItem)
        {
            if (!_catalogItems.ContainsKey(id))
                return Task.FromResult<CatalogItem?>(null);

            updatedItem.Id = id;
            _catalogItems[id] = updatedItem;
            return Task.FromResult<CatalogItem?>(updatedItem);
        }

        public Task<bool> DeleteCatalogItemAsync(int id)
        {
            return Task.FromResult(_catalogItems.TryRemove(id, out _));
        }

        public Task DeleteCatalogItemsByCategoryAsync(int categoryId)
        {
            var itemsToDelete = _catalogItems.Values
                .Where(item => item.CategoryId == categoryId)
                .Select(item => item.Id)
                .ToList();

            foreach (var itemId in itemsToDelete)
            {
                _catalogItems.TryRemove(itemId, out _);
            }

            return Task.CompletedTask;
        }

        private void SeedData()
        {
            var items = new List<CatalogItem>
            {
                new CatalogItem { Name = "Book1", Description = "This is book 1", Price = 10.99m, CategoryId = 1 },
                new CatalogItem { Name = "Book2", Description = "This is book 2", Price = 20.99m, CategoryId = 1 },
                new CatalogItem { Name = "Record1", Description = "This is record 1", Price = 19.99m, CategoryId = 2 },
                new CatalogItem { Name = "Record2", Description = "This is record 2", Price = 29.99m, CategoryId = 2 }
            };

            foreach (var item in items)
            {
                CreateCatalogItemAsync(item);
            }
        }
    }
}
