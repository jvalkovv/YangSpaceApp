import { Component, OnInit } from '@angular/core';
import { NavbarComponent } from '../shared/components/navbar/navbar.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FooterComponent } from "../shared/components/footer/footer.component";
import { UserProfileService } from './user-profile.service';  // Import the UserProfileService

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [NavbarComponent, CommonModule, FormsModule, FooterComponent],
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.css']
})
export class UserProfileComponent implements OnInit {
  userProfile: any = {};  // Holds the user profile data
  bookedTasks: any[] = [];  // Define bookedTasks as an empty array

  // Make userProfileService public to be accessed in the template
  constructor(public userProfileService: UserProfileService) { }

  ngOnInit(): void {
    this.fetchUserProfile();  // Fetch profile on initialization
  }

  fetchUserProfile(): void {
    this.userProfileService.getUserProfile().subscribe({
      next: (data) => {
        this.userProfile = data;  // Set the profile data to userProfile
      },
      error: (err) => {
        console.error('Error fetching user profile:', err);
      }
    });
  }

  updateProfile(): void {
    this.userProfileService.updateUserProfile(this.userProfile).subscribe({
      next: () => {
        alert('Profile updated successfully!');
      },
      error: (err) => {
        console.error('Error updating profile:', err);
        alert('Failed to update profile.');
      }
    });
  }

  // Use the getter to access viewBookings
  toggleBookings(): void {
    this.userProfileService.toggleBookings();
    if (this.userProfileService.viewBookings) {
      this.fetchBookedTasks();  // Fetch booked tasks if viewBookings is true
    } else {
      this.bookedTasks = [];  // Clear booked tasks if viewBookings is false
    }
  }

  // New method to fetch booked tasks
  fetchBookedTasks(): void {
    // Simulating the API call or service to fetch booked tasks
    // Replace this with a real service call if you have one
    this.bookedTasks = [
      { name: 'Service 1', date: '2024-11-30', status: 'Confirmed' },
      { name: 'Service 2', date: '2024-12-05', status: 'Pending' }
    ];
  }
}
