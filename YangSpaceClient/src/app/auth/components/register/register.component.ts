import { Component } from '@angular/core';
import { AbstractControl, AsyncValidatorFn, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, debounceTime, finalize, map, Observable, of, switchMap } from 'rxjs';
import { CommonModule } from '@angular/common';
import { environment } from '../../../../environments/environment';
import { FooterComponent } from '../../../shared/components/footer/footer.component';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { AuthService } from '../../services/auth-service';


@Component({
  selector: 'app-register',
  standalone: true,
  imports: [NavbarComponent, CommonModule, ReactiveFormsModule, FooterComponent],
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

  // Save JWT and user details
  saveUserDetails(token: string, username: string) {
    localStorage.setItem('jwt', token);
    localStorage.setItem('username', username);
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

  usernameValidator(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value) {
        return of(null);  // If no value is provided, no validation is needed.
      }

      this.isUsernameChecking = true;

      return this.http.get<any>(`${this.apiUrl}/account/check-username/${control.value}`).pipe(
        debounceTime(500),  // Debounce for 500ms to wait before making the API call.
        map((response) => {
          return response.isUsernameTaken ? { usernameTaken: true } : null;  // If the username is taken, return error.
        }),
        catchError((error) => {
          console.error('Error during username validation:', error);
          return of({ usernameTaken: false });  // Return false for usernameTaken if error occurs.
        }),
        finalize(() => {
          this.isUsernameChecking = false;  // Reset the checking flag after API call.
        })
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
