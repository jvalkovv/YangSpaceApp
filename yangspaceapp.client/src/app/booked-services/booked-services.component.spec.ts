import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BookedServicesComponent } from './booked-services.component';

describe('BookedServicesComponent', () => {
  let component: BookedServicesComponent;
  let fixture: ComponentFixture<BookedServicesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BookedServicesComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BookedServicesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
