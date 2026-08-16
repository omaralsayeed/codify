namespace Codify.Application.DTOs.Analytics;

public class PublicProfileResponse
{
    public ProfileUserDto User { get; set; } = new();
    public int TotalSolved { get; set; }
    public int TotalAttempted { get; set; }
    public double SuccessRate { get; set; }
    public ProfileStreakDto Streak { get; set; } = new();
    public DifficultyCountDto DifficultyBreakdown { get; set; } = new();
    public DifficultyCountDto DifficultyTotals { get; set; } = new();
    public List<ProfileLanguageDto> LanguageStats { get; set; } = [];
    public List<ProfileTopicPerformanceDto> TopicStats { get; set; } = [];
    public List<ProfileActivityDayDto> ActivityGrid { get; set; } = [];
    public List<ProfileRecentSubmissionDto> RecentAccepted { get; set; } = [];
}

public class ProfileUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AvatarInitials { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "student";
    public DateTime JoinedAt { get; set; }
    public string? Headline { get; set; }
    public string? Bio { get; set; }
    public ProfileSocialDto? Social { get; set; }
}

public class ProfileSocialDto
{
    public string? Linkedin { get; set; }
    public string? Github { get; set; }
    public string? Twitter { get; set; }
}

public class ProfileStreakDto
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalActiveDays { get; set; }
    public int TotalSubmissionsLastYear { get; set; }
}

public class DifficultyCountDto
{
    public int Easy { get; set; }
    public int Medium { get; set; }
    public int Hard { get; set; }
}

public class ProfileLanguageDto
{
    public string Language { get; set; } = string.Empty;
    public int Solved { get; set; }
}

public class ProfileTopicPerformanceDto
{
    public string TopicId { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public int Attempted { get; set; }
    public int Solved { get; set; }
    public double StrengthScore { get; set; }
    public string Strength { get; set; } = "average"; // strong | average | weak
    public string? AiInsight { get; set; }
}

public class ProfileActivityDayDto
{
    public string Date { get; set; } = string.Empty; // YYYY-MM-DD
    public int Count { get; set; }
}

public class ProfileRecentSubmissionDto
{
    public string SubmissionId { get; set; } = string.Empty;
    public string ProblemId { get; set; } = string.Empty;
    public string ProblemTitle { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Medium";
    public string Status { get; set; } = "Accepted";
    public string Language { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}
