
using System.ComponentModel.DataAnnotations;

namespace BrightSmileDEntal.DTOs.Service
{
    /// <summary>
    /// Details used to update an existing dental service.
    /// </summary>
    public class UpdateServiceDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        /// <summary>
        /// Name of the dental service.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        /// <summary>
        /// Description of the dental service.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        [Range(
            typeof(decimal),
            "0.01",
            "9999999.99",
            ErrorMessage = "Price must be greater than zero.")]
        /// <summary>
        /// Service price in South African rand (ZAR).
        /// </summary>
        public decimal Price { get; set; }
    }
}
