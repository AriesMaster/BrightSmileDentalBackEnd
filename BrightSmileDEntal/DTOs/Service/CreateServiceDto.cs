
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Service
{
    /// <summary>
    /// Details required to create a dental service.
    /// </summary>
    public class CreateServiceDto
    {
        /// <summary>
        /// Dental service name.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Dental service description.
        /// </summary>
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Service price in South African rand (ZAR).
        /// </summary>
        [Range(
            typeof(decimal),
            "0.01",
            "9999999.99",
            ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }
    }
}
