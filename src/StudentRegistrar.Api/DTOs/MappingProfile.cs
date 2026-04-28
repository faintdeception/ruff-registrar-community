using AutoMapper;
using StudentRegistrar.Models;
using StudentRegistrar.Api.DTOs;
using System.Text.Json;

namespace StudentRegistrar.Api.DTOs;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Student mappings (using new Student model)
        CreateMap<Student, StudentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.GetHashCode())) // Convert Guid to int for legacy API compatibility
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.AccountHolder != null ? src.AccountHolder.EmailAddress : ""))
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth.HasValue ? DateOnly.FromDateTime(src.DateOfBirth.Value) : new DateOnly()))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.AccountHolder != null ? src.AccountHolder.HomePhone : ""))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => "")) // Will be filled from AccountHolder address if needed
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => ""))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => ""))
            .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src => ""))
            .ForMember(dest => dest.EmergencyContactName, opt => opt.MapFrom(src => ""))
            .ForMember(dest => dest.EmergencyContactPhone, opt => opt.MapFrom(src => ""));
        
        CreateMap<CreateStudentDto, Student>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AccountHolderId, opt => opt.Ignore())
            .ForMember(dest => dest.AccountHolder, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollments, opt => opt.Ignore())
            .ForMember(dest => dest.StudentInfoJson, opt => opt.Ignore())
            .ForMember(dest => dest.Grade, opt => opt.Ignore())
            .ForMember(dest => dest.Notes, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        
        CreateMap<UpdateStudentDto, Student>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AccountHolderId, opt => opt.Ignore())
            .ForMember(dest => dest.AccountHolder, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollments, opt => opt.Ignore())
            .ForMember(dest => dest.StudentInfoJson, opt => opt.Ignore())
            .ForMember(dest => dest.Grade, opt => opt.Ignore())
            .ForMember(dest => dest.Notes, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // Course mappings (using new Course model)
        CreateMap<Course, CourseDto>()
            .ForMember(dest => dest.TimeSlot, opt => opt.MapFrom(src => src.TimeSlot))
            .ForMember(dest => dest.CurrentEnrollment, opt => opt.MapFrom(src => src.CurrentEnrollment))
            .ForMember(dest => dest.AvailableSpots, opt => opt.MapFrom(src => src.AvailableSpots))
            .ForMember(dest => dest.IsFull, opt => opt.MapFrom(src => src.IsFull))
            .ForMember(dest => dest.Instructors, opt => opt.MapFrom(src => src.CourseInstructors))
            .ForMember(dest => dest.InstructorNames, opt => opt.MapFrom(src => src.CourseInstructors.Select(ci => ci.FullName).ToList()))
            .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
            .ForMember(dest => dest.Semester, opt => opt.Ignore()); // Ignore to prevent circular reference
        
        CreateMap<CreateCourseDto, Course>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SemesterId, opt => opt.Ignore())
            .ForMember(dest => dest.Semester, opt => opt.Ignore())
            .ForMember(dest => dest.CourseInstructors, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollments, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CourseConfigJson, opt => opt.Ignore())
            .ForMember(dest => dest.MaxCapacity, opt => opt.MapFrom(src => 20)) // Default capacity
            .ForMember(dest => dest.AgeGroup, opt => opt.MapFrom(src => "General"))
            .ForMember(dest => dest.Fee, opt => opt.Ignore())
            .ForMember(dest => dest.Room, opt => opt.Ignore())
            .ForMember(dest => dest.PeriodCode, opt => opt.Ignore());
        
        CreateMap<UpdateCourseDto, Course>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SemesterId, opt => opt.Ignore())
            .ForMember(dest => dest.Semester, opt => opt.Ignore())
            .ForMember(dest => dest.CourseInstructors, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollments, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CourseConfigJson, opt => opt.Ignore())
            .ForMember(dest => dest.MaxCapacity, opt => opt.Ignore())
            .ForMember(dest => dest.AgeGroup, opt => opt.Ignore())
            .ForMember(dest => dest.Fee, opt => opt.Ignore())
            .ForMember(dest => dest.Room, opt => opt.Ignore())
            .ForMember(dest => dest.PeriodCode, opt => opt.Ignore());

        // Enrollment mappings (using new Enrollment model)
        CreateMap<Enrollment, EnrollmentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.GetHashCode())) // Convert Guid to int for legacy API compatibility
            .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId.GetHashCode()))
            .ForMember(dest => dest.CourseId, opt => opt.MapFrom(src => src.CourseId.GetHashCode()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.EnrollmentType.ToString()))
            .ForMember(dest => dest.CompletionDate, opt => opt.MapFrom(src => src.GetEnrollmentInfo().WithdrawalDate));
        
        CreateMap<CreateEnrollmentDto, Enrollment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.StudentId, opt => opt.Ignore())
            .ForMember(dest => dest.CourseId, opt => opt.Ignore())
            .ForMember(dest => dest.SemesterId, opt => opt.Ignore())
            .ForMember(dest => dest.Student, opt => opt.Ignore())
            .ForMember(dest => dest.Course, opt => opt.Ignore())
            .ForMember(dest => dest.Semester, opt => opt.Ignore())
            .ForMember(dest => dest.Payments, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.EnrollmentType, opt => opt.MapFrom(src => EnrollmentType.Enrolled))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => PaymentStatus.Pending))
            .ForMember(dest => dest.EnrollmentInfoJson, opt => opt.Ignore())
            .ForMember(dest => dest.FeeAmount, opt => opt.Ignore())
            .ForMember(dest => dest.AmountPaid, opt => opt.Ignore())
            .ForMember(dest => dest.Notes, opt => opt.Ignore())
            .ForMember(dest => dest.WaitlistPosition, opt => opt.Ignore());

        // Grade record mappings
        CreateMap<GradeRecord, GradeRecordDto>();
        CreateMap<CreateGradeRecordDto, GradeRecord>();

        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Profile, opt => opt.MapFrom(src => src.UserProfile));
        CreateMap<CreateUserRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.KeycloakId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));
        CreateMap<UpdateUserRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.KeycloakId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // UserProfile mappings
        CreateMap<UserProfile, UserProfileDto>().ReverseMap();
        CreateMap<UserProfileDto, UserProfile>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());

        // CourseInstructor mappings
        CreateMap<CourseInstructor, CourseInstructorDto>()
            .ForMember(dest => dest.InstructorInfo, opt => opt.MapFrom(src => src.GetInstructorInfo()))
            .ForMember(dest => dest.Course, opt => opt.Ignore()) // Prevent circular reference
            .ForMember(dest => dest.AccountHolder, opt => opt.MapFrom(src => src.AccountHolder));
        CreateMap<CreateCourseInstructorDto, CourseInstructor>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Course, opt => opt.Ignore())
            .ForMember(dest => dest.AccountHolder, opt => opt.Ignore())
            .AfterMap((src, dest) => {
                if (src.InstructorInfo != null)
                {
                    var modelInfo = new Models.InstructorInfo
                    {
                        Bio = src.InstructorInfo.Bio,
                        Qualifications = src.InstructorInfo.Qualifications,
                        CustomFields = src.InstructorInfo.CustomFields
                    };
                    dest.SetInstructorInfo(modelInfo);
                }
            });
            
        // Create mapping for Update DTO
        CreateMap<UpdateCourseInstructorDto, CourseInstructor>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CourseId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Course, opt => opt.Ignore())
            .AfterMap((src, dest) => {
                if (src.InstructorInfo != null)
                {
                    var modelInfo = new Models.InstructorInfo
                    {
                        Bio = src.InstructorInfo.Bio,
                        Qualifications = src.InstructorInfo.Qualifications,
                        CustomFields = src.InstructorInfo.CustomFields
                    };
                    dest.SetInstructorInfo(modelInfo);
                }
            });

        // DTO to Model mappings for InstructorInfo and EducatorInfo
        CreateMap<DTOs.InstructorInfo, Models.InstructorInfo>();
        CreateMap<Models.InstructorInfo, DTOs.InstructorInfo>();
        CreateMap<DTOs.EducatorInfo, Models.EducatorInfo>();
        CreateMap<Models.EducatorInfo, DTOs.EducatorInfo>();

        // Educator mappings (replaces CourseInstructor system)
        CreateMap<Educator, EducatorDto>()
            .ForMember(dest => dest.EducatorInfo, opt => opt.MapFrom(src => src.GetEducatorInfo()));
        CreateMap<CreateEducatorDto, Educator>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.AccountHolder, opt => opt.Ignore())
            .ForMember(dest => dest.KeycloakUserId, opt => opt.Ignore())
            .AfterMap((src, dest) => {
                if (src.EducatorInfo != null)
                {
                    var modelInfo = new Models.EducatorInfo
                    {
                        Bio = src.EducatorInfo.Bio,
                        Qualifications = src.EducatorInfo.Qualifications,
                        Specializations = src.EducatorInfo.Specializations,
                        Department = src.EducatorInfo.Department,
                        CustomFields = src.EducatorInfo.CustomFields
                    };
                    dest.SetEducatorInfo(modelInfo);
                }
            });
        CreateMap<UpdateEducatorDto, Educator>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.AccountHolder, opt => opt.Ignore())
            .ForMember(dest => dest.KeycloakUserId, opt => opt.Ignore())
            .AfterMap((src, dest) => {
                if (src.EducatorInfo != null)
                {
                    var modelInfo = new Models.EducatorInfo
                    {
                        Bio = src.EducatorInfo.Bio,
                        Qualifications = src.EducatorInfo.Qualifications,
                        Specializations = src.EducatorInfo.Specializations,
                        Department = src.EducatorInfo.Department,
                        CustomFields = src.EducatorInfo.CustomFields
                    };
                    dest.SetEducatorInfo(modelInfo);
                }
            });
        // AccountHolder mappings
        CreateMap<AccountHolder, AccountHolderDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.AddressJson, opt => opt.MapFrom(src => src.GetAddress()))
            .ForMember(dest => dest.EmergencyContactJson, opt => opt.MapFrom(src => src.GetEmergencyContact()))
            .ForMember(dest => dest.Students, opt => opt.MapFrom(src => src.Students))
            .ForMember(dest => dest.Payments, opt => opt.MapFrom(src => src.Payments));
            
        CreateMap<CreateAccountHolderDto, AccountHolder>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastEdit, opt => opt.Ignore())
            .ForMember(dest => dest.Students, opt => opt.Ignore())
            .ForMember(dest => dest.Payments, opt => opt.Ignore())
            .ForMember(dest => dest.AddressJson, opt => opt.Ignore())
            .ForMember(dest => dest.EmergencyContactJson, opt => opt.Ignore())
            .AfterMap((src, dest, context) => 
            {
                if (src.AddressJson != null)
                {
                    var address = context.Mapper.Map<StudentRegistrar.Models.Address>(src.AddressJson);
                    dest.SetAddress(address);
                }
                if (src.EmergencyContactJson != null)
                {
                    var contact = context.Mapper.Map<StudentRegistrar.Models.EmergencyContact>(src.EmergencyContactJson);
                    dest.SetEmergencyContact(contact);
                }
                dest.MemberSince = DateTime.UtcNow;
                dest.LastEdit = DateTime.UtcNow;
            });
            
        CreateMap<Student, StudentDetailDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.StudentInfoJson, opt => opt.MapFrom(src => src.GetStudentInfo()))
            .ForMember(dest => dest.Enrollments, opt => opt.MapFrom(src => src.Enrollments));
            
        CreateMap<Student, StudentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => (int)src.Id.GetHashCode())) // Convert Guid to int for legacy compatibility
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => "")) // Student model doesn't have email, set empty
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth.HasValue ? DateOnly.FromDateTime(src.DateOfBirth.Value) : new DateOnly()))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => "")) // Student model doesn't have phone, set empty
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => "")) // Student model doesn't have address, set empty
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => ""))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => ""))
            .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src => ""))
            .ForMember(dest => dest.EmergencyContactName, opt => opt.MapFrom(src => ""))
            .ForMember(dest => dest.EmergencyContactPhone, opt => opt.MapFrom(src => ""));
            
        CreateMap<CreateStudentForAccountDto, Student>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AccountHolderId, opt => opt.Ignore())
            .ForMember(dest => dest.AccountHolder, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollments, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.StudentInfoJson, opt => opt.Ignore())
            .AfterMap((src, dest, context) => 
            {
                if (src.StudentInfoJson != null)
                {
                    var modelInfo = context.Mapper.Map<StudentRegistrar.Models.StudentInfo>(src.StudentInfoJson);
                    dest.SetStudentInfo(modelInfo);
                }
                dest.CreatedAt = DateTime.UtcNow;
                dest.UpdatedAt = DateTime.UtcNow;
            });
            
        CreateMap<Enrollment, EnrollmentDetailDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.CourseId, opt => opt.MapFrom(src => src.CourseId.ToString()))
            .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.Course != null ? src.Course.Name : ""))
            .ForMember(dest => dest.CourseCode, opt => opt.MapFrom(src => src.Course != null ? src.Course.Code : null))
            .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Semester != null ? src.Semester.Name : ""));
            
        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()));
            
        CreateMap<CreatePaymentDto, Payment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.AccountHolder, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollment, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentInfoJson, opt => opt.MapFrom(src => "{}"));
            
        CreateMap<UpdatePaymentDto, Payment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AccountHolderId, opt => opt.Ignore())
            .ForMember(dest => dest.EnrollmentId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.AccountHolder, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollment, opt => opt.Ignore());
            
        // Supporting object mappings
        CreateMap<StudentRegistrar.Models.Address, AddressInfo>().ReverseMap();
        CreateMap<StudentRegistrar.Models.EmergencyContact, EmergencyContactInfo>().ReverseMap();
        CreateMap<StudentRegistrar.Models.StudentInfo, StudentInfoDetails>().ReverseMap();
        
        // New Course System mappings
        CreateMap<Semester, SemesterDto>()
            .ForMember(dest => dest.IsRegistrationOpen, opt => opt.MapFrom(src => src.IsRegistrationOpen))
            .ForMember(dest => dest.Courses, opt => opt.Ignore()); // Ignore to prevent circular reference
        CreateMap<CreateSemesterDto, Semester>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Courses, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollments, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.PeriodConfigJson, opt => opt.Ignore());
        CreateMap<UpdateSemesterDto, Semester>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Courses, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollments, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.PeriodConfigJson, opt => opt.Ignore());
        CreateMap<CreateCourseDto, Course>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Semester, opt => opt.Ignore())
            .ForMember(dest => dest.CourseInstructors, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollments, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CourseConfigJson, opt => opt.Ignore());
        CreateMap<UpdateCourseDto, Course>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SemesterId, opt => opt.Ignore())
            .ForMember(dest => dest.Semester, opt => opt.Ignore())
            .ForMember(dest => dest.CourseInstructors, opt => opt.Ignore())
            .ForMember(dest => dest.Enrollments, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CourseConfigJson, opt => opt.Ignore());

        // Room mappings
        CreateMap<Room, RoomDto>()
            .ForMember(dest => dest.CourseCount, opt => opt.MapFrom(src => src.Courses.Count));
        CreateMap<CreateRoomDto, Room>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Courses, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        CreateMap<UpdateRoomDto, Room>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Courses, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }
}
