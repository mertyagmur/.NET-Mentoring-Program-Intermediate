using System.ComponentModel.DataAnnotations;

namespace CatalogAPI.DTOs
{
    public class AddCategoryRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}
