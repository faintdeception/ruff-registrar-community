# Changelog

## [0.3.0](https://github.com/faintdeception/ruff-registrar-community/compare/v0.2.0...v0.3.0) (2026-02-09)


### Features

* add tenancy foundation ([#1](https://github.com/faintdeception/ruff-registrar-community/issues/1)) ([54f688a](https://github.com/faintdeception/ruff-registrar-community/commit/54f688ac6111fda2312c536ffcc7629ffdcaf930))
* enhance Keycloak configuration with tenancy support ([622bf72](https://github.com/faintdeception/ruff-registrar-community/commit/622bf729f5cbce44aa05042388ae1e2d827f3952))


### Bug Fixes

* restore frontend Dockerfile context ([8bcdb88](https://github.com/faintdeception/ruff-registrar-community/commit/8bcdb881badb5086420469e6821ceafc6d5aa516))
* update frontend Dockerfile entrypoint path ([b4876aa](https://github.com/faintdeception/ruff-registrar-community/commit/b4876aaa80262e7717230e835d237da51cdf5626))

## [0.2.0](https://github.com/faintdeception/ruff-registrar/compare/v0.1.0...v0.2.0) (2026-02-04)


### Features

* add application version to environment variables and display in login page ([3e6dff4](https://github.com/faintdeception/ruff-registrar/commit/3e6dff4a4649a9ad9114353aab121543d75822d5))
* Add Aspire deployment manifest script and Keycloak realm configuration template ([e9956bf](https://github.com/faintdeception/ruff-registrar/commit/e9956bf3f4129cfea9115cffa7eaf3f5f955b286))
* Add end-to-end tests for login functionality and implement base test infrastructure ([4267ac9](https://github.com/faintdeception/ruff-registrar/commit/4267ac9c2a50cbc319ec804dc1e5c378b9942fce))
* Add GitHub Actions workflows for Azure Container Apps and AKS deployments; include Keycloak bootstrap and seeding scripts ([d8003c3](https://github.com/faintdeception/ruff-registrar/commit/d8003c3e8a21c3e8bc18dbcd6d09ff3361410723))
* Add initial implementation of Student Registrar API with CRUD operations ([6477644](https://github.com/faintdeception/ruff-registrar/commit/6477644af356b8a6eac4a361ef5646b9e8eacd16))
* Add mapping for Student to StudentDto with legacy compatibility adjustments ([2b396e4](https://github.com/faintdeception/ruff-registrar/commit/2b396e46429bec60ee7949205f395dbc7f5b40d9))
* Add member credentials display with copy functionality and security notice ([5cf816d](https://github.com/faintdeception/ruff-registrar/commit/5cf816d7b0c8c4b0b0d18db5b744105d1eeab052))
* Add role-based end-to-end tests for Admin, Educator, and Member functionalities; update README with testing instructions ([e90a4f8](https://github.com/faintdeception/ruff-registrar/commit/e90a4f8f6bc7fdab7fad9d9312653cf64855e529))
* Add service interfaces for CourseInstructor, Course, Educator, Grade, Keycloak, Room, and Password management; refactor existing interfaces into individual files ([78e8fce](https://github.com/faintdeception/ruff-registrar/commit/78e8fced76745042521b29f4dee85de8404ffbe7))
* Add test runner script and implement unit tests for AccountHoldersController ([a619363](https://github.com/faintdeception/ruff-registrar/commit/a619363134556b218b0399db2699752776074757))
* Add unit tests for CourseService, EnrollmentService, GradeService, and StudentService ([75cf1d2](https://github.com/faintdeception/ruff-registrar/commit/75cf1d275020e50e6a8619281894e7880d430ad0))
* Add unit tests for EducatorsController, EnrollmentsController, and PaymentsController to enhance test coverage ([9854271](https://github.com/faintdeception/ruff-registrar/commit/9854271c33d88d439049c3b14a1bd49187127c7c))
* Enhance E2E tests with improved navigation and room selection logic; add AssemblyInfo for test configuration ([5ad5e6a](https://github.com/faintdeception/ruff-registrar/commit/5ad5e6a690a8d8a72ef1824b2a62daf846d446aa))
* Enhance member management tests with improved success/error message handling and data-testid attributes ([ad94204](https://github.com/faintdeception/ruff-registrar/commit/ad94204b80301141e015f28dfbfc1dac2dacdfd6))
* Enhance WebDriverFactory and BaseTest with improved error handling, implicit wait settings, and additional Chrome options for better stability and performance ([9bcff2f](https://github.com/faintdeception/ruff-registrar/commit/9bcff2f7cb18a52700bdc5a46301b2893db7ee99))
* Implement API endpoints for courses and students, update frontend pages ([5fc9964](https://github.com/faintdeception/ruff-registrar/commit/5fc996455f8b3a37757a4d33822a2e622927c57f))
* Implement course creation functionality with modal and instructor name handling ([c6b9564](https://github.com/faintdeception/ruff-registrar/commit/c6b956480c4683fd5fa627d99c014034b743fef5))
* Implement dashboard statistics fetching and loading state; add error handling for API requests ([950dc37](https://github.com/faintdeception/ruff-registrar/commit/950dc37a03bf66bc284d64b3a8d302c494e7b5bd))
* Implement EnrollmentsController with CRUD operations and update tests for GUID usage ([91e3ed4](https://github.com/faintdeception/ruff-registrar/commit/91e3ed461f53aa8233e2fdd1448995b3dae3bbf1))
* implement health check response builder and tests ([#12](https://github.com/faintdeception/ruff-registrar/issues/12)) ([b6639f3](https://github.com/faintdeception/ruff-registrar/commit/b6639f345d1d8d18134a44db892c83ca743bd71b))
* Implement instructor management endpoints and update course instructor models for co-op member integration ([4cc8afa](https://github.com/faintdeception/ruff-registrar/commit/4cc8afa64a0933aaaa5d23cf4aa7f9414cb2651c))
* Implement logout functionality and enhance login tests with detailed diagnostics and error handling ([9dad111](https://github.com/faintdeception/ruff-registrar/commit/9dad1114b79eef014cc9b2b776f53da09d90a151))
* Implement member management functionality with create member feature and admin access control ([d997d98](https://github.com/faintdeception/ruff-registrar/commit/d997d9810c562dd568f831d6c2a34157acda5e11))
* Implement password management service and integrate with Keycloak user creation ([a96cb0a](https://github.com/faintdeception/ruff-registrar/commit/a96cb0a10da64e887082050adafe8bc821531288))
* Refactor API calls in courses, enrollments, grades, and rooms pages to use apiClient; remove direct token handling ([ab37986](https://github.com/faintdeception/ruff-registrar/commit/ab37986e52640f39a5a2e67b2dea51714266b22d))
* Refactor API calls to use apiClient for member and student management; remove direct token handling ([38f1250](https://github.com/faintdeception/ruff-registrar/commit/38f1250ef7803582fcb35e6703095ff3f0c6bd26))
* Update account holder creation to include temporary password handling and response structure ([bf75b69](https://github.com/faintdeception/ruff-registrar/commit/bf75b6998828bbdd72edccb19c631037d5f51129))
* Update AccountHolders creation in seed script to include test users from Keycloak ([c0c8b5b](https://github.com/faintdeception/ruff-registrar/commit/c0c8b5beb463b34539c11f44b664f5f2f02e6b21))
* Update API URL configuration for frontend and adjust HTTPS redirection to production only ([59f35bb](https://github.com/faintdeception/ruff-registrar/commit/59f35bb8a07ef8c7103bf70120be518e49e91e83))
* Update Keycloak setup scripts and configuration; enhance hot reload support and package references ([165d24b](https://github.com/faintdeception/ruff-registrar/commit/165d24b2d07c0c6978ebb7697d64ad795946998c))
* Update user roles to reflect Keycloak integration; modify test credentials for consistency ([8d71e1c](https://github.com/faintdeception/ruff-registrar/commit/8d71e1c654498cebc88c061b21ead98eb67c39cf))


### Bug Fixes

* Update AGENTS.md to reference Program.cs instead of apphost.cs ([3f244a5](https://github.com/faintdeception/ruff-registrar/commit/3f244a557c010f7e9be0f920fc12168cfa16e5dc))
* Update AGENTS.md to reference Program.cs instead of apphost.cs ([3a10eee](https://github.com/faintdeception/ruff-registrar/commit/3a10eeec8e35da86b575c5c6282d550c3dc004d8))
* Update Keycloak client secret configuration and adjust frontend API URL for proper endpoint usage ([88db4a3](https://github.com/faintdeception/ruff-registrar/commit/88db4a3ba4ebd61ace56be66f768329b9baac371))
