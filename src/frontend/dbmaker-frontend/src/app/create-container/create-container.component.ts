import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ContainerService } from '../services/container.service';
import { MsalService } from '@azure/msal-angular';
import { protectedResources } from '../auth-config';
import { CreateContainerRequest } from '../models/container.models';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

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
    MatProgressSpinnerModule
  ],
  templateUrl: './create-container.html',
  styleUrls: ['./create-container.scss']
})
export class CreateContainerComponent implements OnInit {
  containerForm!: FormGroup;
  isCreating = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private containerService: ContainerService,
    private msal: MsalService
  ) {}

  ngOnInit(): void {
    this.containerForm = this.fb.group({
  name: ['', [Validators.required, Validators.minLength(3)]],
  type: ['redis', Validators.required],
      description: ['']
    });
  }

  clearMessages(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }

  onSubmit(): void {
    if (this.containerForm.valid) {
      // Ensure authenticated; if not, redirect and come back to /create
      const hasAccount = this.msal.instance.getAllAccounts().length > 0;
      if (!hasAccount) {
        this.msal.instance.initialize().then(() =>
          this.msal.loginRedirect({ scopes: protectedResources.scopes, redirectStartPage: `${window.location.origin}/create` })
        );
        return;
      }
      this.isCreating = true;
      this.errorMessage = '';
      this.successMessage = '';

  const containerData: CreateContainerRequest = {
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

      this.containerService.createContainer(containerData).subscribe({
        next: () => {
          this.isCreating = false;
          this.successMessage = `Container "${containerData.name}" created successfully!`;
          setTimeout(() => {
            this.router.navigate(['/containers']);
          }, 2000);
        },
        error: (error) => {
          this.isCreating = false;
          this.errorMessage = error.error?.message || 'Failed to create container. Please try again.';
          console.error('Create container error:', error);
        }
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/containers']);
  }
}
