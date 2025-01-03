import { Component, OnInit, Renderer2 } from '@angular/core';
import { Router } from '@angular/router';
import { NavbarComponent } from "../shared/components/navbar/navbar.component";
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { FooterComponent } from "../shared/components/footer/footer.component";
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-homepage',
  standalone: true,
  imports: [NavbarComponent, CommonModule, HttpClientModule, FooterComponent],
  templateUrl: './homepage.component.html',
  styleUrls: ['./homepage.component.scss'],
})
export class HomepageComponent implements OnInit {
  private apiUrl = environment.apiUrl;
  services = [
    {
      title: 'Find Service Providers',
      description: 'Connect with experts in your area for various services.',
      image: `${this.apiUrl}/image/service1.jpg`,
    },
    {
      title: 'Book Appointments',
      description: 'Schedule your appointments with ease and convenience.',
      image: `${this.apiUrl}/image/service2.jpg`,
    },
    {
      title: 'Manage Your Schedule',
      description: 'Keep track of your bookings with our advanced scheduler.',
      image: `${this.apiUrl}/image/s3.jpg`,
    },
  ];

  constructor(private router: Router, private renderer: Renderer2) { }

  ngOnInit(): void {
    // Select the element and set the background image
    const element = document.getElementsByClassName('html')[0];  // Get the first element
    if (element) {
      this.renderer.setStyle(element, 'background-image', `url(${this.apiUrl}/image/hero-bg.jpg)`);
    }
  }

  navigateToRegister() {
    this.router.navigate(['/register']);
  }
}
