using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Data.Repositories;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IAccountHolderRepository _accountHolderRepository;
    private readonly ICourseRepository _courseRepository;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        IAccountHolderRepository accountHolderRepository,
        ICourseRepository courseRepository)
    {
        _enrollmentRepository = enrollmentRepository;
        _accountHolderRepository = accountHolderRepository;
        _courseRepository = courseRepository;
    }

    public async Task<IEnumerable<EnrollmentDetailDto>> GetAllAsync(
        Guid? courseId = null,
        Guid? studentId = null,
        Guid? semesterId = null,
        EnrollmentType? type = null)
    {
        IEnumerable<Enrollment> enrollments;

        if (courseId.HasValue)
        {
            enrollments = await _enrollmentRepository.GetByCourseIdAsync(courseId.Value);
        }
        else if (studentId.HasValue)
        {
            enrollments = await _enrollmentRepository.GetByStudentIdAsync(studentId.Value, semesterId);
        }
        else if (semesterId.HasValue)
        {
            enrollments = await _enrollmentRepository.GetBySemesterAsync(semesterId.Value);
        }
        else
        {
            enrollments = await _enrollmentRepository.GetAllAsync();
        }

        if (type.HasValue)
            enrollments = enrollments.Where(e => e.EnrollmentType == type.Value);

        return enrollments.Select(MapToDetail);
    }

    public async Task<EnrollmentDetailDto?> GetByIdAsync(Guid id)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(id);
        return enrollment is null ? null : MapToDetail(enrollment);
    }

    public async Task<IEnumerable<EnrollmentDetailDto>> GetByStudentAsync(Guid studentId)
    {
        var enrollments = await _enrollmentRepository.GetByStudentIdAsync(studentId);
        return enrollments.Select(MapToDetail);
    }

    public async Task<IEnumerable<EnrollmentDetailDto>> GetByCourseAsync(Guid courseId)
    {
        var enrollments = await _enrollmentRepository.GetByCourseIdAsync(courseId);
        return enrollments.Select(MapToDetail);
    }

    public async Task<IEnumerable<EnrollmentDetailDto>> GetMyEnrollmentsAsync(string keycloakUserId)
    {
        var accountHolder = await _accountHolderRepository.GetByKeycloakUserIdAsync(keycloakUserId)
            ?? throw new InvalidOperationException("Account holder not found.");

        var studentIds = accountHolder.Students.Select(s => s.Id).ToList();

        var all = new List<Enrollment>();
        foreach (var sid in studentIds)
        {
            var enrollments = await _enrollmentRepository.GetByStudentIdAsync(sid);
            all.AddRange(enrollments);
        }

        return all.OrderByDescending(e => e.EnrollmentDate).Select(MapToDetail);
    }

    public async Task<EnrollmentDetailDto> WithdrawAsync(Guid id, string? keycloakUserId, string? reason)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Enrollment not found.");

        if (enrollment.EnrollmentType is not (EnrollmentType.Enrolled or EnrollmentType.Waitlisted))
            throw new InvalidOperationException(
                $"Cannot withdraw an enrollment with status '{enrollment.EnrollmentType}'.");

        // Non-admin path: verify the student belongs to the calling account holder.
        if (!string.IsNullOrWhiteSpace(keycloakUserId))
        {
            var accountHolder = await _accountHolderRepository.GetByKeycloakUserIdAsync(keycloakUserId)
                ?? throw new InvalidOperationException("Account holder not found.");

            var ownsStudent = accountHolder.Students.Any(s => s.Id == enrollment.StudentId);
            if (!ownsStudent)
                throw new UnauthorizedAccessException("This enrollment does not belong to your account.");
        }

        enrollment.EnrollmentType = EnrollmentType.Withdrawn;

        var info = enrollment.GetEnrollmentInfo();
        info.WithdrawalDate = DateTime.UtcNow;
        info.WithdrawalReason = reason;
        enrollment.SetEnrollmentInfo(info);

        if (!string.IsNullOrWhiteSpace(reason))
            enrollment.Notes = reason;

        var updated = await _enrollmentRepository.UpdateAsync(enrollment);
        return MapToDetail(updated);
    }

    public async Task<EnrollmentDetailDto> CancelAsync(Guid id)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Enrollment not found.");

        if (enrollment.EnrollmentType is EnrollmentType.Withdrawn or EnrollmentType.Cancelled)
            throw new InvalidOperationException(
                $"Enrollment is already '{enrollment.EnrollmentType}'.");

        enrollment.EnrollmentType = EnrollmentType.Cancelled;
        var updated = await _enrollmentRepository.UpdateAsync(enrollment);
        return MapToDetail(updated);
    }

    public async Task<EnrollmentDetailDto> PromoteFromWaitlistAsync(Guid id)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Enrollment not found.");

        if (enrollment.EnrollmentType != EnrollmentType.Waitlisted)
            throw new InvalidOperationException("Only waitlisted enrollments can be promoted.");

        var course = await _courseRepository.GetByIdAsync(enrollment.CourseId)
            ?? throw new InvalidOperationException("Associated course not found.");

        var oldPosition = enrollment.WaitlistPosition;

        enrollment.EnrollmentType = EnrollmentType.Enrolled;
        enrollment.WaitlistPosition = null;
        enrollment.FeeAmount = course.Fee;
        enrollment.PaymentStatus = course.Fee > 0 ? PaymentStatus.Pending : PaymentStatus.Paid;

        var updated = await _enrollmentRepository.UpdateAsync(enrollment);

        // Shift remaining waitlist positions down by one.
        if (oldPosition.HasValue)
        {
            var remaining = await _enrollmentRepository.GetWaitlistAsync(course.Id);
            foreach (var waiter in remaining.Where(w => w.WaitlistPosition > oldPosition.Value))
            {
                waiter.WaitlistPosition--;
                await _enrollmentRepository.UpdateAsync(waiter);
            }
        }

        return MapToDetail(updated);
    }

    private static EnrollmentDetailDto MapToDetail(Enrollment e) => new()
    {
        Id = e.Id.ToString(),
        StudentId = e.StudentId.ToString(),
        StudentName = e.Student is null
            ? string.Empty
            : $"{e.Student.FirstName} {e.Student.LastName}",
        CourseId = e.CourseId.ToString(),
        CourseName = e.Course?.Name ?? string.Empty,
        CourseCode = e.Course?.Code,
        SemesterName = e.Semester?.Name ?? e.Course?.Semester?.Name ?? string.Empty,
        EnrollmentType = e.EnrollmentType.ToString(),
        EnrollmentDate = e.EnrollmentDate,
        FeeAmount = e.FeeAmount,
        AmountPaid = e.AmountPaid,
        PaymentStatus = e.PaymentStatus.ToString(),
        WaitlistPosition = e.WaitlistPosition,
        Notes = e.Notes
    };
}
