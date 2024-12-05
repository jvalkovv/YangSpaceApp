import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ServiceService } from '../models/service.service';
import { UserService } from '../models/user.service';
import { CategoryService } from './category-service';
import { CommonModule } from '@angular/common';
import { Category } from './category.module';
import { NavbarComponent } from '../shared/components/navbar/navbar.component';
import { FooterComponent } from '../shared/components/footer/footer.component';
import { AuthService } from '../auth/services/auth-service';

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
  imageFile: File | null = null; // Store selected image file
  imageError: string | null = null; // Store image validation error

  constructor(
    private fb: FormBuilder,
    private serviceService: ServiceService,
    private userService: UserService,
    private categoryService: CategoryService,
    private router: Router,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    // Initialize the form
    this.createServiceForm = this.fb.group({
      title: ['', [Validators.required]],
      description: ['', [Validators.required]],
      price: ['', [Validators.required, Validators.min(1)]],
      categoryId: ['', [Validators.required]],
    });

    // Fetch categories from the backend
    this.categoryService.getCategories().subscribe(categories => {
      this.categories = categories;
    });

    // Fetch providers from the backend
    this.userService.getProviders().subscribe(
      (data) => {
        this.providers = data.filter(user => user.isServiceProvider); // Filter out non-service providers
      },
      (error) => {
        console.error('Error fetching providers:', error);
      }
    );
  }
  // Handle image selection
  onImageChange(event: any): void {
    const file = event.target.files[0];
    if (file) {

      
      // Validate the image file type (e.g., only accept images)
      const validImageTypes = ['image/jpeg', 'image/png', 'image/gif'];
      if (validImageTypes.includes(file.type)) {
        this.imageFile = file;

        this.imageError = null; 
      } else {
        this.imageError = 'Invalid image type. Only JPG, PNG, and GIF are allowed.';
        this.imageFile = null;
      }
    }
  }
  onSubmit(): void {
    if (this.createServiceForm.invalid || !this.imageFile) {
      console.error('Form is invalid or no image selected');
      return; // Do nothing if the form is invalid or no image is selected
    }

    const formData = new FormData();
    formData.append('title', this.createServiceForm.get('title')?.value);
    formData.append('description', this.createServiceForm.get('description')?.value);
    formData.append('price', this.createServiceForm.get('price')?.value);
    formData.append('categoryId', this.createServiceForm.get('categoryId')?.value);
    formData.append('imageUrl', this.imageFile); // Ensure this line is correct

   
    // Call the service to create the new service
    this.serviceService.createService(formData).subscribe(
      (response) => {
       
        this.router.navigate(['/all-services']); // Navigate to all services page on success
      },
      (error) => {
        console.error('Error creating service:', error); // Log any errors
      }
    );
  }
}
