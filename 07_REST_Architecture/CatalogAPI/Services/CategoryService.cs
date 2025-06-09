using CatalogAPI.Models;
using System.Collections.Concurrent;

namespace CatalogAPI.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ConcurrentDictionary<int, Category> _categories = new();
        private int _nextId = 0;

        public CategoryService()
        {
            SeedData();
        }

        public Task<Category> CreateCategoryAsync(Category category)
        {
            category.Id = Interlocked.Increment(ref _nextId);
            _categories[category.Id] = category;
            return Task.FromResult(category);
        }

        public Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return Task.FromResult(_categories.Values.AsEnumerable());
        }

        public Task<Category?> GetCategoryByIdAsync(int id)
        {
            _categories.TryGetValue(id, out var category);
            return Task.FromResult(category);
        }

        public Task<Category?> UpdateCategoryAsync(int id, Category updatedCategory)
        {
            if (!_categories.ContainsKey(id))
                return Task.FromResult<Category?>(null);

            updatedCategory.Id = id;
            _categories[id] = updatedCategory;
            return Task.FromResult<Category?>(updatedCategory);
        }

        public Task<bool> DeleteCategoryAsync(int id)
        {
            return Task.FromResult(_categories.TryRemove(id, out _));
        }

        public Task<bool> CategoryExistsAsync(int id)
        {
            return Task.FromResult(_categories.ContainsKey(id));
        }

        private void SeedData()
        {
            var categories = new List<Category>
            {
                new Category { Name = "Books", Description = "This is the category for books" },
                new Category { Name = "Records", Description = "This is the category for records" }
            };

            foreach (var category in categories)
            {
                CreateCategoryAsync(category);
            }
        }
    }
}
