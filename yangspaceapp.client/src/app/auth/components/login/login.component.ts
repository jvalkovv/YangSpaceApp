import { Component, ErrorHandler, Input } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FooterComponent } from '../../../shared/components/footer/footer.component';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { AuthService } from '../../services/auth-service';
import { DialogComponent } from '../../../dialog/dialog.component';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, NavbarComponent, FooterComponent, DialogComponent],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent {

  loginForm: FormGroup;
  passwordVisible: boolean = false;
  isLoading: boolean = false;
  errorMessage: string = '';

  dialogMessage: string = '';
  dialogTitle: string = '';
  isDialogVisible: boolean = false;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private authService: AuthService
  ) {
    this.loginForm = this.fb.group({
      username: ['', [Validators.required, Validators.minLength(3)]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  openDialog(message: string, title: string): void {
    this.dialogMessage = message;
    this.dialogTitle = title;
    this.isDialogVisible = true;
  }

  closeDialog(): void {
    this.isDialogVisible = false;
  }
  //Error Handling
  public handleDialogError(error: HttpErrorResponse): string {
    const backendMessage = error.error?.message; // Extract backend message
    if (error && error.status === 401) {


      if (backendMessage === "Invalid username.") {
        return 'Invalid username. Please check your username again.';
      } else if (backendMessage === "Invalid password.") {
        return 'Invalid password. Please check your password again.';
      }
      // Default for other 401 cases
      return 'Unauthorized. Please log in again.';
    } else if (error && error.status) {
      return `Error Code: ${error.status}\nMessage: ${error.error?.message || error.message}`;
    }

    return 'An unknown error occurred!';
  }

  togglePasswordVisibility() {
    this.passwordVisible = !this.passwordVisible;
  }

  onLogin() {
    if (this.loginForm.valid) {
      const loginData = this.loginForm.value;
      this.isLoading = true;

      this.authService.login(loginData).pipe(
        catchError((error: HttpErrorResponse) => {
          const errorMessage = this.handleDialogError(error);  // Get the error message
          this.openDialog(errorMessage, 'Login Failed');  // Display error message in dialog
          this.isLoading = false;
          return throwError(() => new Error(errorMessage)); // Ensure we return an observable with the error message
        })
      ).subscribe({
        next: () => {
          this.isLoading = false;
          this.isDialogVisible = false;
          this.router.navigate(['/user-profile']);
        },
        error: () => {
          this.isLoading = false; // Error already handled in catchError
        }
      });
    }
  }
}