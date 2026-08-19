using Codify.Domain.Entities;
using Codify.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Codify.Infrastructure.Persistence.Seed;

public static class ContestSeed
{
    public static async Task SeedAsync(CodifyDbContext db)
    {
        if (await db.Contests.AnyAsync()) return;

        var instructor = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Instructor)
            ?? await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);

        if (instructor == null) return;

        var problems = await db.Problems.Take(6).ToListAsync();
        if (problems.Count < 2) return;

        var students = await db.Users.Where(u => u.Role == UserRole.Student).Take(5).ToListAsync();
        if (students.Count == 0) return;

        var now = DateTime.UtcNow;

        // 1. Live Contest
        var liveContest = new Contest
        {
            Id = Guid.NewGuid(),
            Title = "Algorithm Sprint: Arrays & Strings",
            Description = "A live 2-hour competitive programming challenge focused on arrays, two pointers, and string manipulations.",
            CreatedByInstructorId = instructor.Id,
            StartAt = now.AddHours(-1),
            EndAt = now.AddHours(3),
            Status = ContestStatus.Live,
            CreatedAt = now.AddDays(-1)
        };

        for (int i = 0; i < Math.Min(3, problems.Count); i++)
        {
            liveContest.ContestProblems.Add(new ContestProblem
            {
                ContestId = liveContest.Id,
                ProblemId = problems[i].Id,
                Order = i + 1,
                Points = 100
            });
        }

        // Seed Instructor-Student relationships
        foreach (var student in students)
        {
            if (!await db.InstructorStudents.AnyAsync(x => x.InstructorId == instructor.Id && x.StudentId == student.Id))
            {
                await db.InstructorStudents.AddAsync(new InstructorStudent
                {
                    InstructorId = instructor.Id,
                    StudentId = student.Id,
                    EnrolledAt = now.AddDays(-14)
                });
            }
        }

        foreach (var student in students)
        {
            liveContest.ContestParticipants.Add(new ContestParticipant
            {
                ContestId = liveContest.Id,
                StudentId = student.Id,
                InvitedEmail = student.Email,
                InvitationStatus = InvitationStatus.Accepted,
                RespondedAt = now.AddHours(-1),
                Score = 0,
                ProblemsSolved = 0,
                Accuracy = 0,
                Rank = 0,
                JoinedAt = now.AddHours(-1)
            });
        }

        // 2. Ended / Historical Contest
        var endedContest = new Contest
        {
            Id = Guid.NewGuid(),
            Title = "Data Structures Cup: Stacks & Queues",
            Description = "Comprehensive contest testing understanding of stack and queue implementations and monotonic structures.",
            CreatedByInstructorId = instructor.Id,
            StartAt = now.AddDays(-7),
            EndAt = now.AddDays(-7).AddHours(3),
            Status = ContestStatus.Ended,
            CreatedAt = now.AddDays(-8)
        };

        for (int i = 0; i < Math.Min(3, problems.Count); i++)
        {
            endedContest.ContestProblems.Add(new ContestProblem
            {
                ContestId = endedContest.Id,
                ProblemId = problems[i].Id,
                Order = i + 1,
                Points = 100
            });
        }

        int rank = 1;
        int baseScore = 300;
        foreach (var student in students)
        {
            endedContest.ContestParticipants.Add(new ContestParticipant
            {
                ContestId = endedContest.Id,
                StudentId = student.Id,
                InvitedEmail = student.Email,
                InvitationStatus = InvitationStatus.Accepted,
                RespondedAt = now.AddDays(-7),
                Score = baseScore,
                ProblemsSolved = baseScore == 300 ? 3 : (baseScore == 200 ? 2 : 1),
                Accuracy = baseScore == 300 ? 100 : (baseScore == 200 ? 75 : 50),
                Rank = rank++,
                FinishedAt = now.AddDays(-7).AddHours(2),
                JoinedAt = now.AddDays(-7)
            });
            baseScore = Math.Max(baseScore - 100, 100);
        }

        // 3. Upcoming Contest
        var upcomingContest = new Contest
        {
            Id = Guid.NewGuid(),
            Title = "Dynamic Programming & Trees Championship",
            Description = "Master the art of memoization, tree traversals, and dynamic programming state transitions.",
            CreatedByInstructorId = instructor.Id,
            StartAt = now.AddDays(3),
            EndAt = now.AddDays(3).AddHours(4),
            Status = ContestStatus.Upcoming,
            CreatedAt = now.AddDays(-1)
        };

        for (int i = 0; i < Math.Min(3, problems.Count); i++)
        {
            upcomingContest.ContestProblems.Add(new ContestProblem
            {
                ContestId = upcomingContest.Id,
                ProblemId = problems[i].Id,
                Order = i + 1,
                Points = 100
            });
        }

        // Add 1 student as Pending invite and others as Accepted for demonstration
        bool isFirst = true;
        foreach (var student in students)
        {
            upcomingContest.ContestParticipants.Add(new ContestParticipant
            {
                ContestId = upcomingContest.Id,
                StudentId = student.Id,
                InvitedEmail = student.Email,
                InvitationStatus = isFirst ? InvitationStatus.Pending : InvitationStatus.Accepted,
                RespondedAt = isFirst ? null : now,
                Score = 0,
                ProblemsSolved = 0,
                Accuracy = 0,
                Rank = 0,
                JoinedAt = now
            });
            isFirst = false;
        }

        await db.Contests.AddRangeAsync(liveContest, endedContest, upcomingContest);
        await db.SaveChangesAsync();
    }
}
