using System.ComponentModel.DataAnnotations;

namespace Codify.Application.DTOs.Tags;

// Same shape as CreateConceptTagRequest — used for PUT requests.
// Keeping them separate is good practice: update requests might evolve differently over time.
public class UpdateConceptTagRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}
