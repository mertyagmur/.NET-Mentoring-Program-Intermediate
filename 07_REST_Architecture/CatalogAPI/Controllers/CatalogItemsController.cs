using CatalogAPI.DTOs;
using CatalogAPI.Models;
using CatalogAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.Controllers
{
    public class CatalogItemsController : ControllerBase
    {
        private readonly ICatalogItemService _catalogItemService;
        private readonly ICategoryService _categoryService;

        public CatalogItemsController(ICatalogItemService catalogItemService, ICategoryService categoryService)
        {
            _catalogItemService = catalogItemService;
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<CatalogItem>>> GetCatalogItems(
            [FromQuery] int? categoryId = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            if (categoryId.HasValue)
            {
                var categoryExists = await _categoryService.CategoryExistsAsync(categoryId.Value);
                if (!categoryExists)
                {
                    return BadRequest(new { message = $"Category with id {categoryId} does not exist." });
                }
            }

            var result = await _catalogItemService.GetCatalogItemsAsync(categoryId, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CatalogItem>> GetCatalogItem(int id)
        {
            var item = await _catalogItemService.GetCatalogItemByIdAsync(id);
            if (item == null)
            {
                return NotFound(new { message = $"Catalog item with id {id} not found." });
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CatalogItem>> CreateCatalogItem([FromBody] AddCatalogItemRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var categoryExists = await _categoryService.CategoryExistsAsync(request.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { message = $"Category with id {request.CategoryId} does not exist." });
            }

            var catalogItem = new CatalogItem
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                CategoryId = request.CategoryId
            };

            var createdItem = await _catalogItemService.CreateCatalogItemAsync(catalogItem);
            return CreatedAtAction(nameof(GetCatalogItem), new { id = createdItem.Id }, createdItem);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CatalogItem>> UpdateCatalogItem(int id, [FromBody] UpdateCatalogItemRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var categoryExists = await _categoryService.CategoryExistsAsync(request.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { message = $"Category with id {request.CategoryId} does not exist." });
            }

            var catalogItem = new CatalogItem
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                CategoryId = request.CategoryId
            };

            var updatedItem = await _catalogItemService.UpdateCatalogItemAsync(id, catalogItem);
            if (updatedItem == null)
            {
                return NotFound(new { message = $"Catalog item with id {id} not found." });
            }

            return Ok(updatedItem);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCatalogItem(int id)
        {
            var deleted = await _catalogItemService.DeleteCatalogItemAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Catalog item with id {id} not found." });
            }

            return NoContent();
        }
    }
}
