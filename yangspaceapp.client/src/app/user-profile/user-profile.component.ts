import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { environment } from '../../environments/environment';
import { Service } from '../create-service/service.model';
import { FooterComponent } from "../shared/components/footer/footer.component";
import { NavbarComponent } from "../shared/components/navbar/navbar.component";
import { UserProfileService } from './user-profile.service';
import { Booking } from '../models/booking.model';
import { AuthService } from '../auth/services/auth-service';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, NavbarComponent, FooterComponent],
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.css']
})
export class UserProfileComponent implements OnInit {
  isServiceProvider: boolean = false;
  isLoggedIn: boolean = false;
  userProfile: any = {};
  userRole: any = {};
  bookedTasks: any[] = [];
  viewBookings: boolean = false;
  servicesToProvide: Service[] = [];
  servicesBooked: Service[] = [];
  recentBookings: Booking[] = [];
  constructor(private userProfileService: UserProfileService, private authService: AuthService) { }

  ngOnInit(): void {
    this.fetchUserProfile();
    this.authService.isLoggedIn$.subscribe((isLoggedIn) => {
      this.isLoggedIn = isLoggedIn;
      this.isServiceProvider = this.authService.isServiceProvider(); 
    });
   
    this.fetchServicesToProvide();
    this.fetchServicesBooked();
  }

  get profilePicture(): string {
    return this.userProfile?.profilePictureUrl
      ? `${environment.imageUrl}${this.userProfile.profilePictureUrl}`
      : '';
  }
  fetchUserProfile(): void {
    this.userProfileService.getUserProfile()
      .subscribe({
        next: (data) => {
          this.userProfile = data;
        },
        error: (err) => {
          console.error('Error fetching user profile:', err);
        }
      });
  }
  fetchServicesToProvide(): void {
    this.userProfileService.getServicesToProvide().subscribe((services) => {
      this.servicesToProvide = services;
    });
  }

  fetchServicesBooked(): void {
    this.userProfileService.getServicesBooked().subscribe((services) => {
      this.servicesBooked = services;
    });
  }
}

