using System.ComponentModel.DataAnnotations;

namespace Codify.Application.DTOs.Problems;

/// <summary>
/// Request body for PATCH /api/problems/:id/status.
/// Single-purpose endpoint that toggles a problem's active/inactive state.
/// </summary>
public class ProblemStatusUpdateRequest
{
    [Required]
    public bool IsActive { get; set; }
}
