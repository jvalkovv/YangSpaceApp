import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../auth/services/auth-service';
import { UserProfileService } from '../../../user-profile/user-profile.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit {
  isLoggedIn: boolean = false;
  isServiceProvider: boolean = false;
  username: string | null = null;
  userProfile: any = {};
  isSidebarOpen: boolean = false;
  constructor(private authService: AuthService, private router: Router, private userProfileService: UserProfileService) { }

  ngOnInit(): void {
    this.authService.isLoggedIn$.subscribe((isLoggedIn) => {
      this.isLoggedIn = isLoggedIn;
      this.username = localStorage.getItem(environment.usernameKey);
      this.isServiceProvider = this.authService.isServiceProvider();
      this.fetchUserProfile();
    });
  }
  toggleSidebar() {
    this.isSidebarOpen = !this.isSidebarOpen;
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
  get profilePicture(): string {
    return this.userProfile?.profilePictureUrl
      ? `${environment.imageUrl}${this.userProfile.profilePictureUrl}`
      : '';
  }
  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
