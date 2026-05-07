using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public interface IEnrollmentService
{
    Task<IEnumerable<EnrollmentDetailDto>> GetAllAsync(
        Guid? courseId = null,
        Guid? studentId = null,
        Guid? semesterId = null,
        EnrollmentType? type = null);

    Task<EnrollmentDetailDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<EnrollmentDetailDto>> GetByStudentAsync(Guid studentId);
    Task<IEnumerable<EnrollmentDetailDto>> GetByCourseAsync(Guid courseId);
    Task<IEnumerable<EnrollmentDetailDto>> GetMyEnrollmentsAsync(string keycloakUserId);

    /// <summary>
    /// Transitions an Enrolled or Waitlisted enrollment to Withdrawn.
    /// If keycloakUserId is provided (non-admin path) the student must belong to that account.
    /// </summary>
    Task<EnrollmentDetailDto> WithdrawAsync(Guid id, string? keycloakUserId, string? reason);

    /// <summary>Transitions any active enrollment to Cancelled. Admin-only action.</summary>
    Task<EnrollmentDetailDto> CancelAsync(Guid id);

    /// <summary>
    /// Promotes the top waitlisted enrollment for a course to Enrolled,
    /// or promotes a specific enrollment by id when id is supplied.
    /// Admin-only action.
    /// </summary>
    Task<EnrollmentDetailDto> PromoteFromWaitlistAsync(Guid id);
}
