import { TestBed, ComponentFixture } from '@angular/core/testing';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authService: AuthService;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        AuthService,
        provideRouter([])
      ]
    }).compileComponents();

    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render email input with type="email"', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const emailInput = compiled.querySelector('input[type="email"]');
    expect(emailInput).toBeTruthy();
  });

  it('should render password input', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const passwordInput = compiled.querySelector('input[formControlName="password"]');
    expect(passwordInput).toBeTruthy();
  });

  it('should render remember me checkbox', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const checkbox = compiled.querySelector('input[type="checkbox"]');
    expect(checkbox).toBeTruthy();
  });

  it('should render login button', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const button = compiled.querySelector('button[type="submit"]');
    expect(button?.textContent).toContain('Log In');
  });

  it('should render link to register', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const link = compiled.querySelector('a[href*="register"]');
    expect(link).toBeTruthy();
  });

  it('should render link to forgot password', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const link = compiled.querySelector('a[href*="forgot-password"]');
    expect(link).toBeTruthy();
  });

  it('should toggle password visibility', () => {
    expect(component.showPassword).toBe(false);
    component.togglePasswordVisibility();
    expect(component.showPassword).toBe(true);
    component.togglePasswordVisibility();
    expect(component.showPassword).toBe(false);
  });

  it('should display validation error for empty email', () => {
    const emailControl = component.loginForm.get('email');
    emailControl?.markAsTouched();
    emailControl?.setValue('');
    fixture.detectChanges();

    expect(component.getErrorMessage('email')).toBe('Email is required');
  });

  it('should display validation error for invalid email format', () => {
    const emailControl = component.loginForm.get('email');
    emailControl?.markAsTouched();
    emailControl?.setValue('invalid-email');
    fixture.detectChanges();

    expect(component.getErrorMessage('email')).toBe('Please enter a valid email address');
  });

  it('should display validation error for empty password', () => {
    const passwordControl = component.loginForm.get('password');
    passwordControl?.markAsTouched();
    passwordControl?.setValue('');
    fixture.detectChanges();

    expect(component.getErrorMessage('password')).toBe('Password is required');
  });

  it('should disable submit button when form is invalid', () => {
    component.loginForm.patchValue({ email: '', password: '' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const button = compiled.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });

  it('should enable submit button when form is valid', () => {
    component.loginForm.patchValue({ 
      email: 'test@example.com', 
      password: 'password123' 
    });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const button = compiled.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(button.disabled).toBe(false);
  });

  it('should call authService.login() on valid form submission', () => {
    const loginSpy = vi.spyOn(authService, 'login').mockReturnValue(of({ success: true }));
    
    component.loginForm.patchValue({
      email: 'test@example.com',
      password: 'password123'
    });

    component.onSubmit();

    expect(loginSpy).toHaveBeenCalledWith('test@example.com', 'password123');
  });

  it('should navigate to home on successful login', () => {
    vi.spyOn(authService, 'login').mockReturnValue(of({ success: true }));
    const navigateSpy = vi.spyOn(router, 'navigate');
    
    component.loginForm.patchValue({
      email: 'test@example.com',
      password: 'password123'
    });

    component.onSubmit();

    expect(navigateSpy).toHaveBeenCalledWith(['/']);
  });

  it('should display error message on failed login', () => {
    vi.spyOn(authService, 'login').mockReturnValue(of({ 
      success: false, 
      error: 'Invalid email or password' 
    }));
    
    component.loginForm.patchValue({
      email: 'test@example.com',
      password: 'wrongpassword'
    });

    component.onSubmit();

    expect(component.loginError).toBe('Invalid email or password');
  });

  it('should not submit form when invalid', () => {
    const loginSpy = vi.spyOn(authService, 'login');
    component.loginForm.patchValue({ email: '', password: '' });
    component.onSubmit();

    expect(loginSpy).not.toHaveBeenCalled();
  });
});
