# Requirements Document

## Introduction

This document defines the requirements for a complete authentication system for Codify, an AI-powered programming education platform built with Angular 19. The authentication system provides login, registration, and password recovery functionality for two user roles: Student and Instructor. The implementation uses mock data and local storage for session persistence, with no backend integration required at this stage.

## Glossary

- **Auth_System**: The complete authentication subsystem including login, registration, and password recovery components
- **Login_Component**: The Angular component that renders the login page at route /auth/login
- **Register_Component**: The Angular component that renders the registration page at route /auth/register
- **Forgot_Password_Component**: The Angular component that renders the password recovery page at route /auth/forgot-password
- **Auth_Service**: The Angular service that manages authentication state, user sessions, and mock user data
- **User**: A person using the Codify platform, represented by the User model with properties: id, name, email, role, avatarInitials, and optional streak
- **Student**: A user with role 'student' who accesses learning content
- **Instructor**: A user with role 'instructor' who creates and manages learning content
- **Session**: The authenticated state of a user, persisted in localStorage with user object and token
- **Mock_User_List**: A hardcoded array of user objects used for authentication validation in the absence of a backend
- **Form_Validation**: Angular Reactive Forms validation rules applied to input fields
- **Router**: Angular Router service used for navigation between pages
- **LocalStorage**: Browser storage mechanism used to persist authentication session data

## Requirements

### Requirement 1: User Login

**User Story:** As a Student or Instructor, I want to log in with my email and password, so that I can access my personalized dashboard and content.

#### Acceptance Criteria

1. THE Login_Component SHALL display an email input field with type="email"
2. THE Login_Component SHALL display a password input field with type="password"
3. THE Login_Component SHALL display a toggle control that switches the password field between type="password" and type="text"
4. THE Login_Component SHALL display a "Remember me" checkbox
5. THE Login_Component SHALL display a login button
6. THE Login_Component SHALL display a link to the Register_Component at route /auth/register
7. THE Login_Component SHALL display a link to the Forgot_Password_Component at route /auth/forgot-password
8. THE Login_Component SHALL use Angular Reactive Forms for form management
9. THE Login_Component SHALL validate that the email field is required and matches email format
10. THE Login_Component SHALL validate that the password field is required
11. WHEN the login button is clicked AND form validation passes, THE Login_Component SHALL call Auth_Service.login() with email and password
12. WHEN Auth_Service.login() returns success, THE Login_Component SHALL navigate to route /
13. WHEN Auth_Service.login() returns failure, THE Login_Component SHALL display an inline error message "Invalid email or password"
14. WHEN form validation fails, THE Login_Component SHALL display inline validation error messages for each invalid field

### Requirement 2: User Registration

**User Story:** As a new user, I want to register for a Codify account by providing my details and selecting my role, so that I can start using the platform.

#### Acceptance Criteria

1. THE Register_Component SHALL display a full name input field
2. THE Register_Component SHALL display an email input field with type="email"
3. THE Register_Component SHALL display a password input field with type="password"
4. THE Register_Component SHALL display a toggle control that switches the password field between type="password" and type="text"
5. THE Register_Component SHALL display a confirm password input field with type="password"
6. THE Register_Component SHALL display a role selector control that allows selection between "Student" and "Instructor"
7. THE Register_Component SHALL display a register button
8. THE Register_Component SHALL display a link to the Login_Component at route /auth/login
9. THE Register_Component SHALL use Angular Reactive Forms for form management
10. THE Register_Component SHALL validate that the full name field is required
11. THE Register_Component SHALL validate that the email field is required and matches email format
12. THE Register_Component SHALL validate that the password field is required
13. THE Register_Component SHALL validate that the confirm password field matches the password field
14. THE Register_Component SHALL validate that the role field is required
15. WHEN the register button is clicked AND form validation passes, THE Register_Component SHALL call Auth_Service.register() with user data
16. WHEN Auth_Service.register() returns success, THE Register_Component SHALL navigate to route /
17. WHEN form validation fails, THE Register_Component SHALL display inline validation error messages for each invalid field

### Requirement 3: Password Recovery Flow

**User Story:** As a user who forgot my password, I want to request a password reset link, so that I can regain access to my account.

#### Acceptance Criteria

1. THE Forgot_Password_Component SHALL display an email input field on the initial screen
2. THE Forgot_Password_Component SHALL display a "Send Reset Link" button on the initial screen
3. WHEN the "Send Reset Link" button is clicked, THE Forgot_Password_Component SHALL transition to a success screen
4. THE Forgot_Password_Component SHALL display the message "If this email exists, a reset link has been sent." on the success screen
5. THE Forgot_Password_Component SHALL display a link to the Login_Component at route /auth/login on the success screen
6. THE Forgot_Password_Component SHALL NOT send actual emails or make backend requests

### Requirement 4: Authentication Service - Mock Implementation

**User Story:** As a developer, I want a mock authentication service that simulates backend behavior, so that I can develop and test the authentication UI without a real backend.

#### Acceptance Criteria

1. THE Auth_Service SHALL maintain a Mock_User_List containing exactly two users:
   - User 1: { email: 'student@codify.com', password: '123456', role: 'student', name: 'Test Student' }
   - User 2: { email: 'instructor@codify.com', password: '123456', role: 'instructor', name: 'Test Instructor' }
2. THE Auth_Service SHALL provide a login(email: string, password: string) method
3. WHEN Auth_Service.login() is called, THE Auth_Service SHALL check if the email and password match any user in Mock_User_List
4. WHEN credentials match a user in Mock_User_List, THE Auth_Service SHALL generate a mock token string
5. WHEN credentials match a user in Mock_User_List, THE Auth_Service SHALL store the user object in localStorage with key 'codify_user'
6. WHEN credentials match a user in Mock_User_List, THE Auth_Service SHALL store the token in localStorage with key 'codify_token'
7. WHEN credentials match a user in Mock_User_List, THE Auth_Service SHALL update the currentUser signal with the authenticated user
8. WHEN credentials match a user in Mock_User_List, THE Auth_Service SHALL return a success result
9. WHEN credentials do not match any user in Mock_User_List, THE Auth_Service SHALL return a failure result
10. THE Auth_Service SHALL provide a register(userData: RegisterData) method
11. WHEN Auth_Service.register() is called, THE Auth_Service SHALL add the new user to Mock_User_List
12. WHEN Auth_Service.register() is called, THE Auth_Service SHALL generate a unique user id
13. WHEN Auth_Service.register() is called, THE Auth_Service SHALL generate avatarInitials from the user's name
14. WHEN Auth_Service.register() is called, THE Auth_Service SHALL store the new user in localStorage with key 'codify_user'
15. WHEN Auth_Service.register() is called, THE Auth_Service SHALL generate and store a mock token in localStorage with key 'codify_token'
16. WHEN Auth_Service.register() is called, THE Auth_Service SHALL update the currentUser signal with the new user
17. THE Auth_Service SHALL provide a logout() method
18. WHEN Auth_Service.logout() is called, THE Auth_Service SHALL remove 'codify_user' from localStorage
19. WHEN Auth_Service.logout() is called, THE Auth_Service SHALL remove 'codify_token' from localStorage
20. WHEN Auth_Service.logout() is called, THE Auth_Service SHALL set the currentUser signal to null
21. THE Auth_Service SHALL expose a currentUser signal of type Signal<User | null>
22. THE Auth_Service SHALL expose an isLoggedIn computed signal that returns true when currentUser is not null
23. WHEN the Auth_Service is initialized, THE Auth_Service SHALL check localStorage for 'codify_user' and 'codify_token'
24. WHEN localStorage contains valid 'codify_user' and 'codify_token', THE Auth_Service SHALL restore the user session by setting the currentUser signal

### Requirement 5: Authentication Routing

**User Story:** As a user, I want seamless navigation between authentication pages and the main application, so that I have a smooth user experience.

#### Acceptance Criteria

1. THE Auth_System SHALL define route /auth/login that loads Login_Component
2. THE Auth_System SHALL define route /auth/register that loads Register_Component
3. THE Auth_System SHALL define route /auth/forgot-password that loads Forgot_Password_Component
4. WHEN a user successfully logs in, THE Auth_System SHALL navigate to route /
5. WHEN a user successfully registers, THE Auth_System SHALL navigate to route /
6. WHEN an authenticated user navigates to /auth/login, THE Auth_System SHALL redirect to route /
7. WHEN an authenticated user navigates to /auth/register, THE Auth_System SHALL redirect to route /

### Requirement 6: Form Validation and Error Display

**User Story:** As a user filling out authentication forms, I want clear validation feedback, so that I know what information is required and in what format.

#### Acceptance Criteria

1. WHEN an email field loses focus AND the field is empty, THE form SHALL display error message "Email is required"
2. WHEN an email field loses focus AND the field contains invalid email format, THE form SHALL display error message "Please enter a valid email address"
3. WHEN a password field loses focus AND the field is empty, THE form SHALL display error message "Password is required"
4. WHEN a full name field loses focus AND the field is empty, THE form SHALL display error message "Full name is required"
5. WHEN the confirm password field loses focus AND the value does not match the password field, THE form SHALL display error message "Passwords do not match"
6. WHEN a role selector has not been selected AND the form is submitted, THE form SHALL display error message "Please select a role"
7. THE form SHALL display validation errors inline below or next to the relevant input field
8. THE form SHALL disable the submit button WHILE any validation errors exist

### Requirement 7: Session Persistence

**User Story:** As a user, I want my login session to persist across browser refreshes, so that I don't have to log in every time I visit the site.

#### Acceptance Criteria

1. WHEN a user successfully authenticates, THE Auth_Service SHALL store the user object as JSON in localStorage with key 'codify_user'
2. WHEN a user successfully authenticates, THE Auth_Service SHALL store the authentication token in localStorage with key 'codify_token'
3. WHEN the application initializes, THE Auth_Service SHALL read 'codify_user' from localStorage
4. WHEN the application initializes, THE Auth_Service SHALL read 'codify_token' from localStorage
5. WHEN both 'codify_user' and 'codify_token' exist in localStorage, THE Auth_Service SHALL restore the user session
6. WHEN the user logs out, THE Auth_Service SHALL remove both 'codify_user' and 'codify_token' from localStorage

### Requirement 8: UI Styling and Design Consistency

**User Story:** As a user, I want the authentication pages to match the existing Codify design system, so that I have a consistent visual experience.

#### Acceptance Criteria

1. THE Login_Component SHALL use SCSS variables from src/app/styles/_variables.scss for colors, fonts, and spacing
2. THE Register_Component SHALL use SCSS variables from src/app/styles/_variables.scss for colors, fonts, and spacing
3. THE Forgot_Password_Component SHALL use SCSS variables from src/app/styles/_variables.scss for colors, fonts, and spacing
4. THE authentication forms SHALL use a centered card-style layout with border-radius from $r or $r-lg
5. THE authentication forms SHALL use box-shadow from $shadow for card elevation
6. THE authentication forms SHALL use $navy for primary headings
7. THE authentication forms SHALL use $blue for primary action buttons
8. THE authentication forms SHALL use $charcoal for body text
9. THE authentication forms SHALL use $muted for secondary text and labels
10. THE authentication forms SHALL use $ff-body font family for all text
11. THE authentication forms SHALL display validation errors in a red color (#D32F2F or similar)
12. THE authentication forms SHALL use consistent spacing and alignment with existing Codify components

### Requirement 9: User Role Management

**User Story:** As the system, I want to track user roles, so that I can provide role-specific features and content to Students and Instructors.

#### Acceptance Criteria

1. THE User model SHALL include a role property of type 'student' | 'instructor'
2. WHEN a user registers, THE Register_Component SHALL capture the selected role
3. WHEN a user authenticates, THE Auth_Service SHALL include the role property in the stored user object
4. THE Auth_Service SHALL expose the current user's role through the currentUser signal
5. THE authenticated user's role SHALL be accessible to all components via Auth_Service.currentUser().role

### Requirement 10: Password Visibility Toggle

**User Story:** As a user entering my password, I want to toggle password visibility, so that I can verify I typed it correctly.

#### Acceptance Criteria

1. THE password input field SHALL display an icon or button adjacent to the input
2. WHEN the password field type is "password", THE toggle control SHALL display an "show" icon (e.g., eye icon)
3. WHEN the password field type is "text", THE toggle control SHALL display a "hide" icon (e.g., eye-slash icon)
4. WHEN the toggle control is clicked AND the password field type is "password", THE password field type SHALL change to "text"
5. WHEN the toggle control is clicked AND the password field type is "text", THE password field type SHALL change to "password"
6. THE toggle control SHALL apply to both password and confirm password fields in the Register_Component
