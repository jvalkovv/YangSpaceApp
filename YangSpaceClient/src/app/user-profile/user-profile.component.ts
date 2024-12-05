import { Component, OnInit } from '@angular/core';
import { UserProfileService } from './user-profile.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavbarComponent } from "../shared/components/navbar/navbar.component";
import { FooterComponent } from "../shared/components/footer/footer.component";
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, NavbarComponent, FooterComponent],
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.css']
})
export class UserProfileComponent implements OnInit {
  userProfile: any = {};
  userRole: any={};
  bookedTasks: any[] = [];
  viewBookings: boolean = false;

  constructor(private userProfileService: UserProfileService) { }

  ngOnInit(): void {
    this.fetchUserProfile();
    
  }

  fetchUserProfile(): void {
    this.userProfileService.getUserProfile()
      .subscribe({
        next: (data) => {
          this.userProfile = data;
                   
        },
        error: (err) => {
        }
      });
  }
}
