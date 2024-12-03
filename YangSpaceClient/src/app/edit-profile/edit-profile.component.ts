import { Component, OnInit } from '@angular/core';
import { AbstractControl, AsyncValidatorFn, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { AuthService } from '../auth/services/auth-service';
import { Observable } from 'rxjs';
import { FooterComponent } from '../shared/components/footer/footer.component';
import { NavbarComponent } from '../shared/components/navbar/navbar.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-edit-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FooterComponent, NavbarComponent],
  templateUrl: './edit-profile.component.html',
  styleUrls: ['./edit-profile.component.css'],
})
export class EditProfileComponent implements OnInit {
  private apiUrl = `${environment.apiUrl}/UserProfile/user-profile`;
  editProfileForm: FormGroup;

  constructor(private fb: FormBuilder, private http: HttpClient, private authService: AuthService) {
    // Initialize form with validation rules and async validator for firstName
    this.editProfileForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.pattern('^[a-zA-Z]+$')]],  // Only letters, no spaces
      lastName: ['', [Validators.required, Validators.pattern('^[a-zA-Z]+$')]],   // Only letters, no spaces
    });
  }

  ngOnInit(): void {
    
    this.getUserProfile().subscribe(
      (profile) => {
        this.editProfileForm.patchValue(profile); 
      },
      (error) => {
        console.error('Error fetching user profile:', error);
      }
    );
  }

  // Fetch the user profile from the backend
  getUserProfile(): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.get(this.apiUrl, { headers });
  }

  // Get Authorization headers with token
  private getAuthHeaders(): HttpHeaders {
    const tokenKey = localStorage.getItem(environment.tokenKey);
    return new HttpHeaders({
      Authorization: `${tokenKey}`,
    });
  }


  // Save the profile updates (including password)
  saveProfile(): void {
    if (this.editProfileForm.valid) {
      const userProfile = this.editProfileForm.value;
      this.updateProfile(userProfile).subscribe(
        (response) => {
          alert('Profile updated successfully')
        },
        (error) => {
          console.error('Error updating profile:', error);
        }
      );
    }
  }

  // Update user profile with new data (including password)
  updateProfile(userProfile: any): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.put(this.apiUrl, userProfile, { headers });
  }
}
