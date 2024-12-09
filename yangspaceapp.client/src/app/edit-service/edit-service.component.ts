import { Component, Input, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ServiceService } from '../models/service.service';
import { CategoryService } from '../create-service/category-service';
import { CommonModule } from '@angular/common';
import { Category } from '../create-service/category.module';
import { NavbarComponent } from '../shared/components/navbar/navbar.component';
import { FooterComponent } from '../shared/components/footer/footer.component';
import { Service } from '../create-service/service.model';
import { environment } from '../../environments/environment';
import { DialogComponent } from '../dialog/dialog.component';


@Component({
  selector: 'app-edit-service',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, DialogComponent, NavbarComponent, FooterComponent],
  templateUrl: './edit-service.component.html',
  styleUrls: ['./edit-service.component.css']
})
export class EditServiceComponent implements OnInit {
  @Input() service!: Service
  editServiceForm!: FormGroup;
  categories: Category[] = [];
  existingServiceImage: string | null | undefined;
  serviceId!: number;
  imageFile: File | null = null; // Store selected image file
  imageError: string | null = null; // Store image validation error
  dialogMessage: string = '';
  dialogTitle: string = '';
  isDialogVisible: boolean = false;
  errorMessage: string = '';
  fileInput: any = '';
  constructor(
    private fb: FormBuilder,
    private serviceService: ServiceService,
    private route: ActivatedRoute,
    private categoryService: CategoryService,
    private router: Router,
  ) { }

  get imageUrl(): string {
    if (this.service && this.service.imageUrl) {
      return `${environment.imageUrl}/${this.service.imageUrl}`;
    }
    return '';
  }
  ngOnInit(): void {
    this.serviceId = Number(this.route.snapshot.paramMap.get('id'));
    this.editServiceForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', [Validators.required, Validators.minLength(10)]],
      price: [0, [Validators.required, Validators.min(1)]],
      categoryId: ['', Validators.required],
      imageUrl: [null],
    });

    this.categoryService.getCategories().subscribe({
      next: (data) => (this.categories = data),
      error: (err) => console.error('Failed to load categories:', err),
    });

    this.loadServiceData();
  }

  loadServiceData(): void {
    this.serviceService.getServiceById(this.serviceId).subscribe({
      next: (service) => {
        this.editServiceForm.patchValue({
          title: service.title,
          description: service.description,
          price: service.price,
          categoryId: service.categoryId,

        });
        this.existingServiceImage = service.imageUrl;

      },
      error: (err) => console.error('Failed to load service data:', err),
    });
  }

  // Handle image selection
  onImageChange(event: any): void {
    this.fileInput = event.target;
    const file = event.target.files[0];
    if (file) {
      // Validate the image file type (e.g., only accept images)
      const validImageTypes = ['image/jpeg', 'image/png', 'image/gif'];
      if (validImageTypes.includes(file.type)) {
        this.imageFile = file;
        this.imageError = null;
      } else {
        this.imageFile = null;
        this.imageError = 'Invalid image type. Only JPG, PNG, and GIF are allowed.';
        this.openDialog(this.imageError, "Failed")
      }
    }
  }
  openDialog(message: string, title: string): void {
    this.dialogMessage = message;
    this.dialogTitle = title;
    this.isDialogVisible = true;
  }

  closeDialog(): void {
    this.isDialogVisible = false;
    this.fileInput.value = '';
  }


  onSubmit(): void {
    // Ensure the form is valid and an image is selected
    if (this.editServiceForm.invalid || !this.imageFile) {
      this.openDialog('Form is invalid or no image selected', 'Failed')
      this.imageFile = null;
      return;
    }

    const formData = new FormData();

    // Append form data (other form fields)
    for (const key in this.editServiceForm.value) {
      if (this.editServiceForm.value[key]) {
        formData.append(key, this.editServiceForm.value[key]);
      }
    }
    // Append the file correctly
    if (this.imageFile) {
      formData.append('imageFile', this.imageFile);
    }

    this.serviceService.updateService(this.serviceId, formData).subscribe({
      next: (response) => {
        this.openDialog('Profile updated successfully.', 'Success');

        console.log('Service updated successfully:', response);
        this.router.navigate(['/all-services']); // Navigate to the service
      },
      error: (err) => console.error('Error updating service:', err),
    });
  }
}
