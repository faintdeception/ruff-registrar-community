export interface Student {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  dateOfBirth: string;
  phoneNumber?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateStudentDto {
  firstName: string;
  lastName: string;
  email: string;
  dateOfBirth: string;
  phoneNumber?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
}

export interface Course {
  id: string;
  name: string;
  code: string;
  description?: string;
  creditHours: number;
  instructor: string;
  academicYear: string;
  semester: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCourseDto {
  name: string;
  code: string;
  description?: string;
  creditHours: number;
  instructor: string;
  academicYear: string;
  semester: string;
}

export interface Enrollment {
  id: string;
  studentId: string;
  student: Student;
  courseId: string;
  course: Course;
  enrollmentDate: string;
  completionDate?: string;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateEnrollmentDto {
  studentId: string;
  courseId: string;
  enrollmentDate: string;
  status?: string;
}

// Independent Educator types (not tied to courses)
export interface EducatorDto {
  id: string;
  accountHolderId?: string;
  keycloakUserId?: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email?: string;
  phone?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  educatorInfo: EducatorInfo;
}

export interface CreateEducatorDto {
  accountHolderId?: string;
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  isActive?: boolean;
  educatorInfo?: EducatorInfo;
}

export interface InviteEducatorDto {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  accountHolderId?: string;
  educatorInfo?: EducatorInfo;
}

export interface UserCredentials {
  username: string;
  temporaryPassword: string;
  mustChangePassword: boolean;
}

export interface InviteEducatorResponse {
  educator: EducatorDto;
  credentials?: UserCredentials;
  message: string;
}

export interface AccountHolderDto {
  id: string;
  firstName: string;
  lastName: string;
  emailAddress: string;
  homePhone?: string;
  mobilePhone?: string;
}

export interface UpdateEducatorDto {
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  isActive?: boolean;
  educatorInfo?: EducatorInfo;
}

export interface EducatorInfo {
  bio?: string;
  qualifications: string[];
  specializations: string[];
  department?: string;
  customFields: Record<string, string>;
}

export interface GradeRecord {
  id: number;
  studentId: string;
  student: Student;
  courseId: string;
  course: Course;
  letterGrade?: string;
  numericGrade?: number;
  gradePoints?: number;
  comments?: string;
  gradeDate: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateGradeRecordDto {
  studentId: string;
  courseId: string;
  letterGrade?: string;
  numericGrade?: number;
  gradePoints?: number;
  comments?: string;
  gradeDate: string;
}
