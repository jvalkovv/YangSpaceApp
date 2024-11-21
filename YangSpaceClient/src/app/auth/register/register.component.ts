import { Component } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, finalize, map, of } from 'rxjs';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { AuthService } from '../auth-service';
import { environment } from '../../../environments/environment';
import { FooterComponent } from "../../shared/components/footer/footer.component";

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [NavbarComponent, CommonModule, ReactiveFormsModule, NavbarComponent, FooterComponent],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
})
export class RegisterComponent {
  registerForm: FormGroup;
  passwordVisible: boolean = false;
  confirmPasswordVisible: boolean = false;
  isUsernameChecking: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';
  private apiUrl = `${environment.apiUrl}`;

  constructor(private fb: FormBuilder, private http: HttpClient, private authService: AuthService, private router: Router) {
    this.registerForm = this.fb.group(
      {
        username: ['', [Validators.required, Validators.minLength(3), this.noWhitespaceValidator], [this.usernameValidator()]],
        email: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required, Validators.minLength(6), this.noWhitespaceValidator]],
        confirmPassword: ['', Validators.required],
        firstName: ['', [Validators.required, this.noWhitespaceValidator]],
        lastName: ['', [Validators.required, this.noWhitespaceValidator]],
        isServiceProvider: [null, Validators.required],
      },
      { validator: this.passwordMatchValidator }
    );
  }

  togglePasswordVisibility(field: 'password' | 'confirmPassword') {
    if (field === 'password') {
      this.passwordVisible = !this.passwordVisible;
    } else {
      this.confirmPasswordVisible = !this.confirmPasswordVisible;
    }
  }

  noWhitespaceValidator(control: AbstractControl): { [key: string]: any } | null {
    const value = control.value || '';
    return value.trim().length === 0 ? { whitespace: true } : null;
  }

  passwordMatchValidator(group: AbstractControl): { [key: string]: any } | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  usernameValidator() {
    return (control: AbstractControl) => {
      if (!control.value) {
        return of(null); // No validation if there's no input
      }

      this.isUsernameChecking = true;
      return this.http.get<{ isUsernameTaken: boolean }>
        (`${this.apiUrl}/check-username/${control.value}`).pipe(
          map(response => response.isUsernameTaken ? { isUsernameTaken: true } : null),
          catchError(() => of(null)),  // Handle error gracefully
          finalize(() => this.isUsernameChecking = false)
        );

    };
  }

  onSubmit() {
    if (this.registerForm.valid) {
      const formData = this.registerForm.value;
      this.authService.register(formData).subscribe({
        next: (response) => {
          this.successMessage = response.message;
          this.authService.saveUserDetails(response.token, response.username);
          this.router.navigate(['/user-profile']);
        },
        error: (err) => {
          this.errorMessage = err.error.message || 'Registration failed.';
        },
      });
    }
  }
}
