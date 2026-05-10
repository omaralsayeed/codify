using System.ComponentModel.DataAnnotations;

namespace Codify.Application.DTOs.Tags;

// This DTO carries the data the client sends when creating a tag.
// [Required] ensures ASP.NET model validation rejects bad requests before they hit the service.
public class CreateConceptTagRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}
