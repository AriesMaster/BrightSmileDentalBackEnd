
namespace BrightSmileDEntal.DTOs.Service
{
    /// <summary>
    /// Dental service details returned by the API.
    /// </summary>
    public class ServiceDto
    {
        /// <summary>
        /// Unique identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Dental service name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Dental service description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Service price in South African rand (ZAR).
        /// </summary>
        public decimal Price { get; set; }
    }
}
