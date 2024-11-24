import { Component, OnInit } from '@angular/core';
import { Router, RouterLinkActive, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../auth/services/auth-service';


@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterModule, CommonModule, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit {
  isLoggedIn: boolean = false;
  username: string | null = '';

  constructor(private authService: AuthService, private router: Router) {}

  
  ngOnInit(): void {
    this.authService.isLoggedIn$.subscribe((isLoggedIn) => {
      this.isLoggedIn = isLoggedIn;
      this.username = localStorage.getItem('username');
    });
  }
  // Check if the user is logged in by looking for a token in localStorage
  checkLoginStatus(): void {
    this.isLoggedIn = !!localStorage.getItem('token'); // Check if token exists
  }

  // Logout method to clear token and user type
  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
