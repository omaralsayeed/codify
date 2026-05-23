import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  submitted = false;

  forgotForm = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email])
  });

  getErrorMessage(field: string): string {
    const control = this.forgotForm.get(field);
    
    if (!control || !control.touched || !control.errors) {
      return '';
    }

    if (control.errors['required']) {
      return 'Email is required';
    }

    if (control.errors['email']) {
      return 'Please enter a valid email address';
    }

    return '';
  }

  onSubmit(): void {
    if (this.forgotForm.invalid) {
      this.forgotForm.markAllAsTouched();
      return;
    }

    this.submitted = true;
  }
}
