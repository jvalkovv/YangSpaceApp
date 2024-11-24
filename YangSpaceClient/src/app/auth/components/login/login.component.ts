import { Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FooterComponent } from '../../../shared/components/footer/footer.component';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { AuthService } from '../../services/auth-service';

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
      password: ['', Validators.required],
    });
  }

  togglePasswordVisibility() {
    this.passwordVisible = !this.passwordVisible;
  }
  
  // Save JWT and user details
  saveUserDetails(token: string, username: string) {
    localStorage.setItem('jwt', token);
    localStorage.setItem('username', username);
  }
  onLogin() {
    if (this.loginForm.valid) {
      const loginData = this.loginForm.value;
      this.authService.login(loginData).subscribe(
        (response) => {
          // Store JWT token in localStorage upon successful login
          localStorage.setItem('jwt', response.token);
          this.authService.saveUserDetails(response.token, response.username);
          localStorage.setItem('token', response.token);

          this.errorMessage = '';
          // Redirect to UserProfile
          this.router.navigate(['/user-profile']); // Redirect to the home page or dashboard
        },
        (error) => {
          this.errorMessage = 'Invalid credentials';
        }
      );
    }
  }
}
