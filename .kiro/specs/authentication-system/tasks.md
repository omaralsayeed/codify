# Implementation Plan: Authentication System

## Overview

This plan implements a complete authentication system for Codify with login, registration, and password recovery functionality. The implementation uses Angular 19 with signals, reactive forms, mock data, and localStorage for session persistence. Tasks are organized to build incrementally, with testing integrated throughout.

## Tasks

- [ ] 1. Set up authentication module structure and core models
  - Create auth feature directory structure (login, register, forgot-password components)
  - Update User model in src/app/core/models/user.model.ts to include password property (optional, for mock data only)
  - Create auth-related TypeScript interfaces (AuthResult, RegisterData, LoginFormData, RegisterFormData, ForgotPasswordFormData)
  - _Requirements: 1.1, 2.1, 4.1, 9.1_

- [ ] 2. Implement AuthService with mock authentication
  - [ ] 2.1 Create AuthService with signal-based state management
    - Implement currentUser writable signal and readonly accessor
    - Implement isLoggedIn computed signal
    - Add mock user list with two test users (student@codify.com, instructor@codify.com)
    - _Requirements: 4.1, 4.2, 4.21, 4.22_

  - [ ] 2.2 Implement login method with localStorage persistence
    - Validate credentials against mock user list
    - Generate mock token on success
    - Store user and token in localStorage (keys: 'codify_user', 'codify_token')
    - Update currentUser signal on success
    - Return Observable<AuthResult> with success/error
    - _Requirements: 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 7.1, 7.2_

  - [ ]* 2.3 Write property test for login credential validation
    - **Property 13: Login Credential Validation**
    - **Validates: Requirements 4.3, 4.8, 4.9**

  - [ ]* 2.4 Write property test for authentication session storage
    - **Property 14: Authentication Session Storage**
    - **Validates: Requirements 4.5, 4.6, 4.14, 4.15, 7.1, 7.2**

  - [ ]* 2.5 Write property test for authentication signal update
    - **Property 15: Authentication Signal Update**
    - **Validates: Requirements 4.7, 4.16**

  - [ ] 2.6 Implement register method with user creation
    - Generate unique user ID (Date.now().toString())
    - Generate avatarInitials from full name (first letter of first and last word)
    - Create User object with provided data and generated fields
    - Add user to mock user list
    - Store user and token in localStorage
    - Update currentUser signal
    - Return Observable<AuthResult>
    - _Requirements: 4.10, 4.11, 4.12, 4.13, 4.14, 4.15, 4.16, 9.2, 9.3_

  - [ ]* 2.7 Write property test for user registration creation
    - **Property 16: User Registration Creation**
    - **Validates: Requirements 4.11, 4.12, 4.13**

  - [ ]* 2.8 Write property test for avatar initials generation
    - **Property 17: Avatar Initials Generation**
    - **Validates: Requirements 4.13**

  - [ ] 2.9 Implement logout method
    - Remove 'codify_user' from localStorage
    - Remove 'codify_token' from localStorage
    - Set currentUser signal to null
    - _Requirements: 4.17, 4.18, 4.19, 4.20, 7.6_

  - [ ]* 2.10 Write property test for logout cleanup
    - **Property 18: Logout Cleanup**
    - **Validates: Requirements 4.18, 4.19, 4.20, 7.6**

  - [ ] 2.11 Implement session restoration in constructor
    - Read 'codify_user' and 'codify_token' from localStorage
    - Parse user JSON and validate structure
    - Update currentUser signal if valid data exists
    - Handle JSON parsing errors gracefully
    - _Requirements: 4.23, 4.24, 7.3, 7.4, 7.5_

  - [ ]* 2.12 Write property test for isLoggedIn computed signal
    - **Property 19: IsLoggedIn Computed Signal**
    - **Validates: Requirements 4.22**

  - [ ]* 2.13 Write property test for session restoration
    - **Property 20: Session Restoration**
    - **Validates: Requirements 4.23, 4.24, 7.3, 7.4, 7.5**

  - [ ]* 2.14 Write unit tests for AuthService
    - Test mock user list initialization
    - Test login with valid credentials
    - Test login with invalid credentials
    - Test registration flow
    - Test logout clears state
    - Test session restoration with valid data
    - Test session restoration with corrupted data
    - Test localStorage error handling

- [ ] 3. Checkpoint - Verify AuthService functionality
  - Ensure all AuthService tests pass, ask the user if questions arise.

- [ ] 4. Implement LoginComponent with reactive forms
  - [ ] 4.1 Create LoginComponent with template and styles
    - Create component files (login.component.ts, login.component.html, login.component.scss)
    - Set up centered card layout using design system variables
    - Create form structure with email, password, rememberMe fields
    - Add links to register and forgot-password routes
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 8.1, 8.4, 8.5, 8.6, 8.7, 8.8, 8.9, 8.10_

  - [ ] 4.2 Implement reactive form with validation
    - Create FormGroup with email, password, rememberMe controls
    - Add Validators.required and Validators.email to email field
    - Add Validators.required to password field
    - Implement getErrorMessage() method for validation feedback
    - Display inline error messages in template
    - _Requirements: 1.8, 1.9, 1.10, 1.14, 6.1, 6.2, 6.3, 6.7, 6.8_

  - [ ]* 4.3 Write property test for email field validation
    - **Property 2: Email Field Validation**
    - **Validates: Requirements 1.9, 2.11, 6.1, 6.2**

  - [ ]* 4.4 Write property test for required field validation
    - **Property 3: Required Field Validation**
    - **Validates: Requirements 1.10, 2.10, 2.12, 2.14, 6.3, 6.4, 6.6**

  - [ ]* 4.5 Write property test for form validation error display
    - **Property 5: Form Validation Error Display**
    - **Validates: Requirements 1.14, 2.17**

  - [ ]* 4.6 Write property test for submit button disabled state
    - **Property 6: Submit Button Disabled State**
    - **Validates: Requirements 6.8**

  - [ ] 4.7 Implement password visibility toggle
    - Add showPassword boolean property
    - Add togglePasswordVisibility() method
    - Add toggle icon button in template
    - Bind password input type to showPassword state
    - Style toggle icon using design system colors
    - _Requirements: 1.3, 10.1, 10.2, 10.3, 10.4, 10.5_

  - [ ]* 4.8 Write property test for password visibility toggle
    - **Property 1: Password Visibility Toggle**
    - **Validates: Requirements 1.3, 2.4, 10.4, 10.5**

  - [ ]* 4.9 Write property test for password toggle icon display
    - **Property 23: Password Toggle Icon Display**
    - **Validates: Requirements 10.2, 10.3**

  - [ ] 4.10 Implement form submission and navigation
    - Inject AuthService and Router
    - Implement onSubmit() method
    - Call authService.login() with form values
    - Navigate to '/' on success
    - Display error message on failure
    - _Requirements: 1.11, 1.12, 1.13_

  - [ ]* 4.11 Write property test for login service integration
    - **Property 7: Login Service Integration**
    - **Validates: Requirements 1.11**

  - [ ]* 4.12 Write property test for successful authentication navigation
    - **Property 9: Successful Authentication Navigation**
    - **Validates: Requirements 1.12, 2.16, 5.4, 5.5**

  - [ ]* 4.13 Write property test for failed login error display
    - **Property 10: Failed Login Error Display**
    - **Validates: Requirements 1.13**

  - [ ]* 4.14 Write unit tests for LoginComponent
    - Test component renders all required elements
    - Test form validation behavior
    - Test password toggle functionality
    - Test successful login flow
    - Test failed login error display
    - Test navigation to register and forgot-password

- [ ] 5. Implement RegisterComponent with role selection
  - [ ] 5.1 Create RegisterComponent with template and styles
    - Create component files (register.component.ts, register.component.html, register.component.scss)
    - Set up centered card layout using design system variables
    - Create form structure with fullName, email, password, confirmPassword, role fields
    - Add role selector with Student and Instructor options
    - Add link to login route
    - _Requirements: 2.1, 2.2, 2.3, 2.5, 2.6, 2.7, 2.8, 8.2, 8.4, 8.5, 8.6, 8.7, 8.8, 8.9, 8.10_

  - [ ] 5.2 Implement reactive form with cross-field validation
    - Create FormGroup with all registration fields
    - Add validators to each field (required, email format)
    - Implement custom passwordMatchValidator for cross-field validation
    - Implement getErrorMessage() method
    - Display inline error messages in template
    - _Requirements: 2.9, 2.10, 2.11, 2.12, 2.13, 2.14, 2.17, 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8_

  - [ ]* 5.3 Write property test for password match validation
    - **Property 4: Password Match Validation**
    - **Validates: Requirements 2.13, 6.5**

  - [ ] 5.4 Implement password visibility toggles for both fields
    - Add showPassword and showConfirmPassword boolean properties
    - Add togglePasswordVisibility(field) method
    - Add toggle icon buttons for both password fields
    - Bind input types to state
    - _Requirements: 2.4, 2.5, 10.1, 10.6_

  - [ ] 5.5 Implement form submission and navigation
    - Inject AuthService and Router
    - Implement onSubmit() method
    - Call authService.register() with form values
    - Navigate to '/' on success
    - _Requirements: 2.15, 2.16, 9.2_

  - [ ]* 5.6 Write property test for registration service integration
    - **Property 8: Registration Service Integration**
    - **Validates: Requirements 2.15, 9.2**

  - [ ]* 5.7 Write property test for role persistence
    - **Property 22: Role Persistence**
    - **Validates: Requirements 9.3, 9.4**

  - [ ]* 5.8 Write unit tests for RegisterComponent
    - Test component renders all required elements
    - Test form validation behavior
    - Test password match validation
    - Test role selection
    - Test password toggle for both fields
    - Test successful registration flow
    - Test navigation to login

- [ ] 6. Implement ForgotPasswordComponent with two-screen flow
  - [ ] 6.1 Create ForgotPasswordComponent with template and styles
    - Create component files (forgot-password.component.ts, forgot-password.component.html, forgot-password.component.scss)
    - Set up centered card layout using design system variables
    - Create initial screen with email input and submit button
    - Create success screen with message and login link
    - Use @if control flow to toggle between screens
    - _Requirements: 3.1, 3.2, 3.4, 3.5, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8, 8.9, 8.10_

  - [ ] 6.2 Implement form submission with state transition
    - Create FormGroup with email field
    - Add submitted boolean property (default false)
    - Implement onSubmit() method that sets submitted = true
    - Ensure no HTTP requests are made
    - _Requirements: 3.3, 3.6_

  - [ ]* 6.3 Write property test for forgot password state transition
    - **Property 11: Forgot Password State Transition**
    - **Validates: Requirements 3.3**

  - [ ]* 6.4 Write property test for no backend requests
    - **Property 12: No Backend Requests in Forgot Password**
    - **Validates: Requirements 3.6**

  - [ ]* 6.5 Write unit tests for ForgotPasswordComponent
    - Test initial screen renders email input and button
    - Test form submission transitions to success screen
    - Test success screen displays message and login link
    - Test no HTTP requests are made

- [ ] 7. Checkpoint - Verify all components render and function
  - Ensure all component tests pass, ask the user if questions arise.

- [ ] 8. Configure authentication routing
  - [ ] 8.1 Create auth.routes.ts with route definitions
    - Define /auth/login route loading LoginComponent
    - Define /auth/register route loading RegisterComponent
    - Define /auth/forgot-password route loading ForgotPasswordComponent
    - Export routes array
    - _Requirements: 5.1, 5.2, 5.3_

  - [ ] 8.2 Create auth guards (authGuard and guestGuard)
    - Implement authGuard as CanActivateFn (protects authenticated routes)
    - Implement guestGuard as CanActivateFn (redirects authenticated users from auth pages)
    - Use inject() to access AuthService and Router
    - Check isLoggedIn signal for authentication state
    - Redirect to appropriate routes based on auth state
    - _Requirements: 5.6, 5.7_

  - [ ]* 8.3 Write property test for authenticated user guard redirect
    - **Property 21: Authenticated User Guard Redirect**
    - **Validates: Requirements 5.6, 5.7**

  - [ ] 8.3 Update main app.routes.ts to lazy load auth routes
    - Add route: { path: 'auth', loadChildren: () => import('./features/auth/auth.routes') }
    - Apply guestGuard to auth routes
    - _Requirements: 5.1, 5.2, 5.3, 5.6, 5.7_

  - [ ]* 8.4 Write unit tests for routing configuration
    - Test auth routes are defined correctly
    - Test authGuard allows authenticated users
    - Test authGuard redirects unauthenticated users to /auth/login
    - Test guestGuard allows unauthenticated users
    - Test guestGuard redirects authenticated users to /

- [ ] 9. Style all authentication components
  - [ ] 9.1 Create shared auth styles
    - Create auth-shared.scss with common card, form, and button styles
    - Use design system variables ($navy, $blue, $charcoal, $muted, $ivory, $white, $ff-body, $r, $r-lg, $shadow, $border)
    - Define .auth-container, .auth-card, .form-field, .btn-primary, .error-message classes
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8, 8.9, 8.10, 8.11, 8.12_

  - [ ] 9.2 Style LoginComponent
    - Import auth-shared.scss
    - Add component-specific styles for password toggle
    - Add styles for remember me checkbox
    - Add styles for links (register, forgot password)
    - Ensure responsive design for mobile (max-width: 480px)
    - _Requirements: 8.1, 8.12_

  - [ ] 9.3 Style RegisterComponent
    - Import auth-shared.scss
    - Add component-specific styles for role selector
    - Add styles for password toggles (both fields)
    - Add styles for link to login
    - Ensure responsive design for mobile
    - _Requirements: 8.2, 8.12_

  - [ ] 9.4 Style ForgotPasswordComponent
    - Import auth-shared.scss
    - Add styles for two-screen transition
    - Add styles for success message
    - Ensure responsive design for mobile
    - _Requirements: 8.3, 8.12_

- [ ] 10. Integration testing and final verification
  - [ ]* 10.1 Write integration tests for complete authentication flows
    - Test complete login flow (form submission → service → navigation)
    - Test complete registration flow (form submission → service → navigation)
    - Test session persistence across page refresh
    - Test logout flow (logout → storage cleared → navigation)
    - Test guard behavior with router navigation
    - Test role-based authentication (student vs instructor)

  - [ ]* 10.2 Write end-to-end property tests
    - Test complete user journey from registration to logout
    - Test session restoration after browser refresh
    - Test navigation between all auth pages
    - Test form validation across all components

- [ ] 11. Final checkpoint - Complete system verification
  - Ensure all tests pass (unit, property-based, integration)
  - Verify all 23 correctness properties are validated
  - Verify all acceptance criteria are met
  - Ask the user if questions arise or if manual testing is needed

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Property-based tests use fast-check library with minimum 100 iterations
- All components use Angular 19 signals and reactive forms
- Mock data approach enables frontend development without backend
- Session persistence uses localStorage with keys 'codify_user' and 'codify_token'
- Design system integration ensures visual consistency with existing Codify components
