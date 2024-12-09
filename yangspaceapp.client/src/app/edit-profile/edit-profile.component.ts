import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { AuthService } from '../auth/services/auth-service';
import { Observable } from 'rxjs';
import { UserProfileService } from '../user-profile/user-profile.service';
import { UserProfileUpdateModel } from '../user-profile/user.model';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../shared/components/navbar/navbar.component';
import { FooterComponent } from '../shared/components/footer/footer.component';
import { DialogComponent } from '../dialog/dialog.component';

@Component({
  selector: 'app-edit-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, NavbarComponent, FooterComponent, DialogComponent],
  templateUrl: './edit-profile.component.html',
  styleUrls: ['./edit-profile.component.css'],
})
export class EditProfileComponent implements OnInit {
  private apiUrl = `${environment.apiUrl}/UserProfile/user-profile`;
  private editUrl = `${environment.apiUrl}/UserProfile/edit-profile`;
  editProfileForm: FormGroup;
  userProfile: UserProfileUpdateModel = {
    firstName: "",
    lastName: "",
    email: "",
    phoneNumber: "",
    bio: "",
    location: "",
    profilePictureUrl: "",
  };
  profilePictureFile: File | null = null;
  imageError: string | null = null; // Store image validation error
  dialogMessage: string = '';
  dialogTitle: string = '';
  isDialogVisible: boolean = false;
  errorMessage: string = '';
  profilePictureInput: any = '';
  constructor(private fb: FormBuilder, private http: HttpClient,
    private authService: AuthService,
    private userProfileService: UserProfileService) {
    // Initialize form with validation rules and async validator for firstName
    this.editProfileForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.pattern('^[a-zA-Z]+$')]],  // Only letters, no spaces
      lastName: ['', [Validators.required, Validators.pattern('^[a-zA-Z]+$')]],   // Only letters, no spaces
      email: ['', [Validators.required, Validators.email]], // Email validation
      phoneNumber: ['', [Validators.required, Validators.pattern('^[0-9]{10}$')]], // Assuming 10-digit phone number
      bio: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]], // Optional, max length of 500 characters
      location: ['', [Validators.required, Validators.maxLength(100)]], // Optional, max length of 100 characters
      profilePicture: [null]
    });
  }

  ngOnInit(): void {
    this.getUserProfile().subscribe(
      (profile) => {
        this.editProfileForm.patchValue(profile);
        this.userProfile.profilePictureUrl = profile.profilePicture; // Set profile picture URL
      },
      (error) => {
        console.error('Error fetching user profile:', error);
      }
    );
  }

  // Handle image selection
  onImageChange(event: any): void {
    const file = event.target.files[0];
    if (file) {
      // Validate the image file type (e.g., only accept images)
      const validImageTypes = ['image/jpeg', 'image/png', 'image/gif'];
      if (validImageTypes.includes(file.type)) {
        this.profilePictureFile = file;
        this.imageError = null;
        const reader = new FileReader();
        reader.onload = (e: any) => {
          this.userProfile.profilePictureUrl = e.target.result; // Base64 image URL for preview
        };
        reader.readAsDataURL(file);
      } else {
        this.profilePictureFile = null;
        this.imageError = 'Invalid image type. Only JPG, PNG, and GIF are allowed.';
        this.openDialog(this.imageError, "Failed");
      }
    }
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

  openDialog(message: string, title: string): void {
    this.dialogMessage = message;
    this.dialogTitle = title;
    this.isDialogVisible = true;
    this.profilePictureInput = ''; // Reset file input
    this.profilePictureFile = null; // Clear the file
    this.imageError = null; // Reset error message

  }

  closeDialog(): void {
    this.isDialogVisible = false;
    this.profilePictureInput = '';
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

  saveProfile(): void {
    if (this.editProfileForm.valid) {
      const formData = new FormData();

      // Append form data (other form fields)
      for (const key in this.editProfileForm.value) {
        if (this.editProfileForm.value[key] !== null) {
          formData.append(key, this.editProfileForm.value[key]);
        }
      }

      // Ensure the profile picture is appended correctly
      if (this.profilePictureFile) {
        formData.append('profilePicture', this.profilePictureFile, this.profilePictureFile.name);
      } else if (!this.profilePictureFile && this.userProfile.profilePictureUrl) {
        // Add existing profile picture URL (if no new file is uploaded)
        formData.append('profilePicture', this.userProfile.profilePictureUrl);
      }

      this.editProfile(formData).subscribe(
        (response) => {
          this.openDialog('Profile updated successfully.', 'Success');
        },
        (error) => {
          const errorMessage = this.handleDialogError(error);
          this.openDialog(errorMessage, 'Update Failed');
        }
      );
    } else {
      this.openDialog('Please fix form errors before submitting.', 'Invalid Form');
    }
  }

  // Update user profile with new data
  editProfile(formData: FormData): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.put(this.editUrl, formData, { headers });
  }
  // Update user profile with new data
  updateProfile(formData: FormData): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.put(this.apiUrl, formData, { headers });
  }
}
