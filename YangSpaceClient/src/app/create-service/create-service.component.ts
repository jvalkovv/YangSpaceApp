import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ServiceService } from '../models/service.service'; // Adjust to your service
import { UserService } from '../models/user.service'; // Adjust to your service
import { CategoryService } from './category-service';
import { CommonModule } from '@angular/common';
import { Category } from './category.module';
import { NavbarComponent } from '../shared/components/navbar/navbar.component';
import { FooterComponent } from '../shared/components/footer/footer.component';

@Component({
  selector: 'app-create-service',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, NavbarComponent, FooterComponent],
  templateUrl: './create-service.component.html',
  styleUrls: ['./create-service.component.css'],
})
export class CreateServiceComponent implements OnInit {
  createServiceForm!: FormGroup;
  categories: Category[] = []; // Array to store categories
  providers: any[] = [];
  currentUser: any = {}; // Store current user info
  

  constructor(
    private fb: FormBuilder,
    private serviceService: ServiceService,
    private userService: UserService,
    private categoryService: CategoryService, 
    private router: Router
  ) { }

  ngOnInit(): void {
    // Initialize the form
    this.createServiceForm = this.fb.group({
      title: ['', [Validators.required]],
      description: ['', [Validators.required]],
      price: ['', [Validators.required, Validators.min(1)]],
      categoryId: ['', [Validators.required]],
    });

    // Fetch categories from backend
    this.categoryService.getCategories().subscribe(categories => {
      this.categories = categories;
    });
    
    // Fetch providers from the backend
    this.userService.getProviders().subscribe(
      (data) => {
        this.providers = data.filter(user => user.isServiceProvider);    
         // Only service providers
      },
      (error) => {
        console.error('Error fetching providers:', error);
      }
    );
  }

  onSubmit(): void {
 if (this.createServiceForm.invalid) {
    return;
  }

  // const selectedCategory = this.categories.find(
  //   (cat) => cat.id === this.createServiceForm.value.categoryId
  // );
  // const selectedProvider = this.providers.find(
  //   (prov) => prov.id === this.createServiceForm.value.providerId
  // );

  const newService = {
    ...this.createServiceForm.value,
  };

  this.serviceService.createService(newService).subscribe(
    (response) => {
      console.log('Service created successfully:', response);
      this.router.navigate(['/all-services']);
    },
    (error) => {
      console.error('Error creating service:', error);
    }
  );
  }
}
