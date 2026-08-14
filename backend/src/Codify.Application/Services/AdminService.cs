using Codify.Application.DTOs.Admin;
using Codify.Application.Interfaces;
using Codify.Domain.Enums;
using Codify.Domain.Exceptions;

namespace Codify.Application.Services;

public class AdminService(IUserRepository userRepo) : IAdminService
{
    public async Task<IReadOnlyList<PendingInstructorResponse>> GetPendingInstructorsAsync()
    {
        var instructors = await userRepo.GetPendingInstructorsAsync();

        return instructors.Select(u => new PendingInstructorResponse
        {
            UserId       = u.Id,
            FullName     = u.FullName,
            Email        = u.Email,
            Organization = u.Organization,
            RegisteredAt = u.CreatedAt
        }).ToList();
    }

    public async Task<ApproveInstructorResponse> ApproveInstructorAsync(Guid instructorId, Guid adminId)
    {
        var instructor = await userRepo.GetByIdAsync(instructorId)
            ?? throw new NotFoundException("Instructor not found.");

        if (instructor.Role != UserRole.Instructor)
            throw new ValidationException("The specified user is not an instructor.");

        if (instructor.Status == UserStatus.Active)
            throw new ValidationException("This instructor account is already active.");

        instructor.Approve(adminId);
        await userRepo.SaveChangesAsync();

        return new ApproveInstructorResponse
        {
            UserId     = instructor.Id,
            FullName   = instructor.FullName,
            Email      = instructor.Email,
            ApprovedAt = instructor.ReviewedAt!.Value
        };
    }
}
