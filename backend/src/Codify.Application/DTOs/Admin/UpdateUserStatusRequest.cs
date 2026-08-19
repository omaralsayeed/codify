using System.ComponentModel.DataAnnotations;

namespace Codify.Application.DTOs.Admin;

/// <summary>
/// Request body for PATCH /api/admin/users/:id/status.
/// </summary>
public class UpdateUserStatusRequest
{
    /// <summary>Must be "active" or "pending". Any other value returns 400.</summary>
    [Required]
    public string Status { get; set; } = string.Empty;
}
