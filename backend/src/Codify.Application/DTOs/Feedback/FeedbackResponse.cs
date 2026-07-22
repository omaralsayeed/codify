using Codify.Domain.Enums;

namespace Codify.Application.DTOs.Feedback;

public class FeedbackResponse
{
    public Guid   Id           { get; set; }
    public string FeedbackType { get; set; } = string.Empty;
    public string Message      { get; set; } = string.Empty;
    public DateTime CreatedAt  { get; set; }
}
