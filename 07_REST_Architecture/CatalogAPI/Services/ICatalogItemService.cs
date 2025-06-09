using CatalogAPI.DTOs;
using CatalogAPI.Models;

namespace CatalogAPI.Services
{
    public interface ICatalogItemService
    {
        Task<PagedResult<CatalogItem>> GetCatalogItemsAsync(int? categoryId = null, int pageNumber = 1, int pageSize = 10);
        Task<CatalogItem?> GetCatalogItemByIdAsync(int id);
        Task<CatalogItem> CreateCatalogItemAsync(CatalogItem item);
        Task<CatalogItem?> UpdateCatalogItemAsync(int id, CatalogItem item);
        Task<bool> DeleteCatalogItemAsync(int id);
        Task DeleteCatalogItemsByCategoryAsync(int categoryId);
    }
}
