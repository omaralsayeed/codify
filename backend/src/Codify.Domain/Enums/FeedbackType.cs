namespace Codify.Domain.Enums;

public enum FeedbackType
{
    CodeQuality,
    Optimization,
    IntegrityFlag,

    /// <summary>Raised when the Code Analysis Agent judges the submission likely AI-generated.</summary>
    AiGenerated
}
