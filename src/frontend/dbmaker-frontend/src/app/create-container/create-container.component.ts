import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ContainerService } from '../services/container.service';
import { NameGeneratorService } from '../services/name-generator.service';
import { MsalService } from '@azure/msal-angular';
import { protectedResources } from '../auth-config';
import { type CreateContainerRequest } from '../../../api/consolidated';
import { environment } from '../../environments/environment';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-create-container',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './create-container.html',
  styleUrls: ['./create-container.scss']
})
export class CreateContainerComponent implements OnInit, OnDestroy {
  containerForm!: FormGroup;
  isCreating = false;
  errorMessage = '';
  successMessage = '';

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private containerService: ContainerService,
    private nameGenerator: NameGeneratorService,
    private msal: MsalService
  ) {}

  ngOnInit(): void {
    this.isCreating = false;

    this.containerForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      type: ['redis', Validators.required],
      description: ['']
    });

    // Auto-generate a friendly name when the component loads
    this.generateRandomName();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }  clearMessages(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }

  generateRandomName(): void {
    const randomName = this.nameGenerator.generateFriendlyName();
    this.containerForm.patchValue({ name: randomName });
  }

  onGenerateNewName(): void {
    this.generateRandomName();
  }

  onSubmit(): void {
    if (this.containerForm.valid && !this.isCreating) {
      this.clearMessages();
      this.isCreating = true;

      const containerData = {
        name: this.containerForm.value.name,
        databaseType: this.containerForm.value.type,
        configuration: {
          description: this.containerForm.value.description || '',
          ...(this.containerForm.value.type === 'postgresql' && {
            'POSTGRES_DB': this.containerForm.value.name,
            'POSTGRES_USER': 'admin',
            'POSTGRES_PASSWORD': 'password123'
          }),
          ...(this.containerForm.value.type === 'redis' && {
            'REDIS_PASSWORD': 'password123'
          })
        }
      };

      // Use HttpClient via API client so MSAL interceptor attaches Bearer token
      this.containerService.createContainer(containerData as any).subscribe({
        next: () => {
          this.isCreating = false;
          this.successMessage = `Container "${containerData.name}" created successfully!`;
          setTimeout(() => {
            this.router.navigate(['/containers']);
          }, 1200);
        },
        error: (error) => {
          this.isCreating = false;
          const msg = error?.error?.message || error?.message || 'Unknown error';
          this.errorMessage = `Failed to create container: ${msg}`;
        }
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/containers']);
  }
}
