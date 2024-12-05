import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Category } from '../create-service/category.module';
import { ServiceService } from '../models/service.service';
import { CategoryService } from '../create-service/category-service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-edit-service',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './edit-service.component.html',
  styleUrls: ['./edit-service.component.css']
})
export class EditServiceComponent implements OnInit {
  editServiceForm!: FormGroup;
  categories: Category[] = [];
  existingServiceImage: string | null | undefined;
  serviceId!: number;

  constructor(
    private fb: FormBuilder,
    private serviceService: ServiceService,
    private route: ActivatedRoute,   private categoryService: CategoryService  ) { }

  ngOnInit(): void {
    this.serviceId = Number(this.route.snapshot.paramMap.get('id'));

    this.editServiceForm = this.fb.group({
      title: ['', [Validators.required]],
      description: ['', [Validators.required]],
      price: ['', [Validators.required]],
      categoryId: ['', [Validators.required]],
      image: [null]
    });

    // Fetch categories from backend
    this.categoryService.getCategories().subscribe(data => {
      this.categories = data;
    });

    this.loadServiceData();
  }

  loadServiceData(): void {
    this.serviceService.getServiceById(this.serviceId).subscribe(service => {
      this.editServiceForm.patchValue({
        title: service.title,
        description: service.description,
        price: service.price,
        categoryId: service.categoryId
      });
      this.existingServiceImage = service.imageUrl; // Adjust based on backend
    });
  }

  onImageChange(event: any): void {
    const file = event.target.files[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = () => {
        this.existingServiceImage = reader.result as string;
        this.editServiceForm.patchValue({
          image: this.existingServiceImage
        });
      };
      reader.readAsDataURL(file);
    }
  }

  onSubmit(): void {
    if (this.editServiceForm.invalid) {
      return;
    }
    this.serviceService.updateService(this.serviceId, this.editServiceForm.value).subscribe(response => {
      console.log('Service updated successfully:', response);
    });
  }
}
