import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Service } from '../create-service/service.model';

@Component({
  selector: 'app-service-item',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './service-item.component.html',
  styleUrls: ['./service-item.component.scss'],
})
export class ServiceItemComponent {
  @Input() service: Service | undefined; 

  bookService(): void {

    // console.log('Service booked:', this.service?.title);
    // console.log('Service Name:', this.service.categoryName); 
    // console.log('Service Category:', this.service.providerName);
  }
}
