import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormGroup, FormControl, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

// Custom validator for password matching
function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password');
  const confirmPassword = control.get('confirmPassword');
  
  if (!password || !confirmPassword) {
    return null;
  }
  
  if (confirmPassword.value === '') {
    return null;
  }
  
  return password.value === confirmPassword.value ? null : { passwordMismatch: true };
}

// Custom validator for phone number
function phoneValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) {
    return null; // Optional field
  }
  
  const phoneRegex = /^\d{10,15}$/;
  return phoneRegex.test(control.value) ? null : { invalidPhone: true };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  showPassword = false;
  showConfirmPassword = false;
  profilePicturePreview: string | null = null;

  registerForm = new FormGroup({
    fullName: new FormControl('', [Validators.required]),
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required]),
    confirmPassword: new FormControl('', [Validators.required]),
    role: new FormControl<'student' | 'instructor' | ''>('', [Validators.required]),
    organization: new FormControl('', [Validators.required]),
    organizationOther: new FormControl(''),
    phoneNumber: new FormControl('', [phoneValidator]),
    country: new FormControl(''),
    city: new FormControl('')
  }, { validators: passwordMatchValidator });

  organizations = [
    'Cairo University',
    'Ain Shams University',
    'Alexandria University',
    'Assiut University',
    'Mansoura University',
    'Zagazig University',
    'Helwan University',
    'ITI',
    'Tanta University',
    'The American University in Cairo',
    'Other'
  ];

  countries = [
    'Egypt',
    'Saudi Arabia',
    'UAE',
    'Kuwait',
    'Jordan',
    'USA',
    'UK',
    'Canada',
    'Other'
  ];

  get showOrganizationOther(): boolean {
    return this.registerForm.get('organization')?.value === 'Other';
  }

  onProfilePictureSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      const reader = new FileReader();
      
      reader.onload = (e) => {
        this.profilePicturePreview = e.target?.result as string;
      };
      
      reader.readAsDataURL(file);
    }
  }

  togglePasswordVisibility(field: 'password' | 'confirmPassword'): void {
    if (field === 'password') {
      this.showPassword = !this.showPassword;
    } else {
      this.showConfirmPassword = !this.showConfirmPassword;
    }
  }

  getErrorMessage(field: string): string {
    const control = this.registerForm.get(field);
    
    if (!control || !control.touched || !control.errors) {
      return '';
    }

    if (control.errors['required']) {
      if (field === 'email') return 'Email is required';
      if (field === 'password') return 'Password is required';
      if (field === 'fullName') return 'Full name is required';
      if (field === 'confirmPassword') return 'Please confirm your password';
      if (field === 'role') return 'Please select a role';
      if (field === 'organization') return 'Please select an organization';
    }

    if (control.errors['email']) {
      return 'Please enter a valid email address';
    }

    if (control.errors['invalidPhone']) {
      return 'Phone number must be 10-15 digits';
    }

    return '';
  }

  getPasswordMismatchError(): string {
    const confirmPasswordControl = this.registerForm.get('confirmPassword');
    
    if (confirmPasswordControl?.touched && this.registerForm.errors?.['passwordMismatch']) {
      return 'Passwords do not match';
    }
    
    return '';
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    const { fullName, email, password, role } = this.registerForm.value;
    
    this.authService.register({
      fullName: fullName!,
      email: email!,
      password: password!,
      role: role as 'student' | 'instructor'
    }).subscribe(result => {
      if (result.success) {
        this.router.navigate(['/']);
      }
    });
  }
}
