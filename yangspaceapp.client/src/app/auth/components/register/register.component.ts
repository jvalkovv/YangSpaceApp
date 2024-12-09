import { Component } from '@angular/core';
import { AbstractControl,  FormBuilder, FormGroup, ReactiveFormsModule,  Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
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

  constructor(private fb: FormBuilder, private http: HttpClient, private authService: AuthService, private router: Router) {
    this.registerForm = this.fb.group(
      {
        username: ['', [Validators.required, Validators.minLength(3), this.noWhitespaceValidator], [this.authService.usernameValidator()]],
        email: ['', [Validators.required, Validators.email], [this.authService.emailValidator()]],
        password: ['', [Validators.required, Validators.minLength(6), this.noWhitespaceValidator]],
        confirmPassword: ['', Validators.required],
        firstName: ['', [Validators.required, Validators.pattern('^[a-zA-Z]+$'), this.noWhitespaceValidator]],
        lastName: ['', [Validators.required, Validators.pattern('^[a-zA-Z]+$'), this.noWhitespaceValidator]],
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
    const value = control.value;  // Keep the value as is, without trimming

    // Check if the value is empty or consists of only spaces
    return value && value.length > 0 && !/^\s+$/.test(value) ? null : { whitespace: true };
  }

  passwordMatchValidator(group: AbstractControl): { [key: string]: any } | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }
  onSubmit() {
    if (this.registerForm.valid) {
      const formData = this.registerForm.value;
      this.authService.register(formData).subscribe({
        next: (response) => {
          this.successMessage = response.message;
          // Call the centralized saveUserDetails method in the AuthService
          this.authService.saveUserDetails(response.token, response.username, response.role);        
          this.router.navigate(['/user-profile']);
        },
        error: (err) => {
          this.errorMessage = err.error.message || 'Registration failed.';
        },
      });
    }
  }
}
