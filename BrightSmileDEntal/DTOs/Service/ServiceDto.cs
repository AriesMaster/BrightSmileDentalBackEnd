
namespace BrightSmileDEntal.DTOs.Service
{
    /// <summary>
    /// Represents a dental service returned by the API.
    /// </summary>
    public class ServiceDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }
    }
}
