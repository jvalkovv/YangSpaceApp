import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { NavbarComponent } from "../shared/components/navbar/navbar.component";
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { FooterComponent } from "../shared/components/footer/footer.component";

@Component({
  selector: 'app-homepage',
  standalone: true,
  imports: [NavbarComponent, CommonModule, HttpClientModule, FooterComponent],
  templateUrl: './homepage.component.html',
  styleUrls: ['./homepage.component.scss'],
})
export class HomepageComponent implements OnInit {
  apiUrl = 'http://localhost:5290/image'; 
  services = [
    {
      title: 'Find Service Providers',
      description: 'Connect with experts in your area for various services.',
      image: `${this.apiUrl}/service1.jpg`,
    },
    {
      title: 'Book Appointments',
      description: 'Schedule your appointments with ease and convenience.',
      image: `${this.apiUrl}/service2.jpg`,
    },
    {
      title: 'Manage Your Schedule',
      description: 'Keep track of your bookings with our advanced scheduler.',
      image: `${this.apiUrl}/s3.jpg`,
    },
  ];

  constructor(private router: Router) { }

  ngOnInit(): void {
  }

  navigateToRegister() {
    this.router.navigate(['/register']);
  }
}
