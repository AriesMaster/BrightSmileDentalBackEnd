
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Service
{
    /// <summary>
    /// Defines the information required to update an existing dental service.
    /// </summary>
    public class UpdateServiceDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(
            typeof(decimal),
            "0.01",
            "9999999.99",
            ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }
    }
}
