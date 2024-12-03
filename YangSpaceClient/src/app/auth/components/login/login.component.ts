import { Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FooterComponent } from '../../../shared/components/footer/footer.component';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { AuthService } from '../../services/auth-service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, NavbarComponent, FooterComponent],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent {
  loginForm: FormGroup;
  passwordVisible: boolean = false;
  isLoading: boolean = false;
  errorMessage: string = '';

  constructor(private fb: FormBuilder, private http: HttpClient, private router: Router, private authService: AuthService) {
    this.loginForm = this.fb.group({
      username: ['', Validators.required],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  togglePasswordVisibility() {
    this.passwordVisible = !this.passwordVisible;
  }

  // Save JWT and user details using environment keys
  saveUserDetails(token: string, username: string) {
    localStorage.setItem(environment.tokenKey, token); // Save token
    localStorage.setItem(environment.usernameKey, username); // Save username
  }
  onLogin() {
    if (this.loginForm.valid) {
      const loginData = this.loginForm.value;
      this.isLoading = true;
      this.authService.login(loginData).subscribe(
        (response) => {
          // Store JWT token and username once
          this.saveUserDetails(response.token, response.username);

          this.errorMessage = '';
          this.isLoading = false;
          // Redirect to UserProfile
          this.router.navigate(['/user-profile']);
        },
        (error ) => {
          this.isLoading = false;
          this.errorMessage = error.message;
        }
      );
    }
  }
}
