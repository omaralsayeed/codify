export interface TopicStat { topic: string; percentage: number; trend: 'up' | 'down' | 'flat'; }
export interface LanguageStat { language: string; solved: number; }
export interface ActivityDay { date: string; count: number; }
export type TopicStrength = 'strong' | 'average' | 'weak';
export interface TopicPerformance { topicId: string; topicName: string; attempted: number; solved: number; strengthScore: number; strength: TopicStrength; aiInsight: string | null; }
export interface DifficultyBreakdown { easy: number; medium: number; hard: number; }
export interface DifficultyTotals { easy: number; medium: number; hard: number; }
export interface SuccessRateDataPoint { label: string; successRate: number; solved: number; }
export interface RecentSubmission { submissionId: string; problemId: string; problemTitle: string; difficulty: 'Easy' | 'Medium' | 'Hard'; status: 'Accepted' | 'Wrong Answer' | 'Runtime Error' | 'Time Limit Exceeded'; language: string; submittedAt: string; }
export interface ProgressRecommendedProblem { problemId: string; title: string; difficulty: 'Easy' | 'Medium' | 'Hard'; topic: string; reason: string; }
export interface HintUsageStats { totalHintsUsed: number; averageHintsPerProblem: number; solvedWithZeroHints: number; solvedUsingAllHints: number; }
export interface DailyActivity { date: string; submitted: boolean; }
export interface StreakData { currentStreak: number; longestStreak: number; lastSevenDays: DailyActivity[]; }
export interface ProgressSummary { studentName: string; totalAttempted: number; totalSolved: number; successRate: number; streak: StreakData; }
export interface StudentAnalytics { summary: ProgressSummary; topics: TopicPerformance[]; difficultyBreakdown: DifficultyBreakdown; successRateHistory: SuccessRateDataPoint[]; recentSubmissions: RecentSubmission[]; recommendations: ProgressRecommendedProblem[]; hintUsage: HintUsageStats; }
export interface PublicProfileData { user: { username: string; name: string; avatarInitials: string; avatarUrl?: string; role: 'student' | 'instructor'; joinedAt: string; headline?: string; bio?: string; social?: { linkedin?: string; github?: string; twitter?: string; }; }; totalSolved: number; totalAttempted: number; successRate: number; streak: { currentStreak: number; longestStreak: number; totalActiveDays: number; totalSubmissionsLastYear: number; }; difficultyBreakdown: DifficultyBreakdown; difficultyTotals: DifficultyTotals; languageStats: LanguageStat[]; topicStats: TopicPerformance[]; activityGrid: ActivityDay[]; recentAccepted: RecentSubmission[]; }
export interface DashboardSummary { problemsSolved: number; avgScore: number; streak: number; totalAttempts: number; acceptanceRate: number; hintsUsedToday: number; hintsLimit: number; }
export interface WeeklyActivity { date: string; solved: number; attempted: number; }
export interface ScorePoint { date: string; score: number; }
export interface RecommendedProblem { id: string; title: string; difficulty: 'easy' | 'medium' | 'hard'; topic: string; topicLabel: string; reason: string; estimatedMinutes: number; }
export interface StudentDashboardData { summary: DashboardSummary; topicStats: TopicStat[]; weeklyActivity: WeeklyActivity[]; scoreHistory: ScorePoint[]; recommendations: RecommendedProblem[]; }