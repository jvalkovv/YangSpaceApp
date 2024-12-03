import { Component, OnInit } from '@angular/core';
import { Router, RouterLinkActive, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../auth/services/auth-service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterModule, CommonModule, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit {
  isLoggedIn: boolean = false;
  username: string | null = null;
  tokenKey: string | null = null;

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.authService.isLoggedIn$.subscribe((isLoggedIn) => {
      this.isLoggedIn = isLoggedIn;
      this.username = localStorage.getItem(environment.usernameKey);
      this.tokenKey = localStorage.getItem(environment.tokenKey);
      
    });

    this.checkLoginStatus();
  }

  // Check login status by checking both token and username in localStorage
  checkLoginStatus(): void {
    const token = localStorage.getItem(environment.tokenKey); // Get the token
    this.isLoggedIn = !!token && !!localStorage.getItem(environment.usernameKey); // User is logged in if both token and username exist
    if (this.isLoggedIn) {
      this.username = localStorage.getItem(environment.usernameKey);
    }
  }

  // Logout method to clear both token and username
  logout(): void {
    this.authService.logout();
    localStorage.removeItem(environment.tokenKey); // Remove the token
    localStorage.removeItem(environment.usernameKey); // Remove the username
    this.router.navigate(['/login']);
  }
}
