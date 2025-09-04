import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SetupService } from '../services/setup.service';
import { SetupStatus, ValidationResult, InitializeSystemRequest, InitializationResult } from '../models/setup.models';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatStepperModule } from '@angular/material/stepper';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-setup',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatStepperModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatDialogModule,
    MatTooltipModule
  ],
  templateUrl: './setup.component.html',
  styleUrls: ['./setup.component.scss']
})
export class SetupComponent implements OnInit {
  setupForm!: FormGroup;
  setupStatus: SetupStatus | null = null;
  validationResults: { [key: string]: ValidationResult } = {};
  isLoading = false;
  isInitializing = false;
  currentStep = 0;
  initializationResult: InitializationResult | null = null;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private setupService: SetupService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.checkSetupStatus();
  }

  private initForm(): void {
    this.setupForm = this.fb.group({
      adminEmail: ['', [Validators.required, Validators.email]],
      adminName: ['', [Validators.required, Validators.minLength(2)]],
      domain: ['localhost', [Validators.required]]
    });
  }

  async checkSetupStatus(): Promise<void> {
    this.isLoading = true;
    try {
      this.setupStatus = await this.setupService.getSetupStatus().toPromise() || null;

      if (this.setupStatus?.systemReady) {
        this.snackBar.open('System is already configured!', 'Close', { duration: 3000 });
        this.router.navigate(['/dashboard']);
        return;
      }

      // Check individual components
      await this.validateAllComponents();
    } catch (error) {
      console.error('Failed to check setup status:', error);
      this.snackBar.open('Failed to check system status', 'Close', { duration: 5000 });
    } finally {
      this.isLoading = false;
    }
  }

  async validateAllComponents(): Promise<void> {
    try {
      // Validate Docker
      this.validationResults['docker'] = await this.setupService.validateDocker().toPromise() ||
        { isValid: false, message: 'Failed to validate Docker', details: '' };

      // Validate MSAL
      this.validationResults['msal'] = await this.setupService.validateMsal().toPromise() ||
        { isValid: false, message: 'Failed to validate MSAL', details: '' };

    } catch (error) {
      console.error('Validation failed:', error);
    }
  }

  async validateDocker(): Promise<void> {
    try {
      this.isLoading = true;
      this.validationResults['docker'] = await this.setupService.validateDocker().toPromise() ||
        { isValid: false, message: 'Failed to validate Docker', details: '' };

      const result = this.validationResults['docker'];
      if (result.isValid) {
        this.snackBar.open('Docker validation successful!', 'Close', { duration: 3000 });
      } else {
        this.snackBar.open(`Docker validation failed: ${result.message}`, 'Close', { duration: 5000 });
      }
    } catch (error) {
      console.error('Docker validation failed:', error);
      this.snackBar.open('Docker validation failed', 'Close', { duration: 5000 });
    } finally {
      this.isLoading = false;
    }
  }

  async validateMsal(): Promise<void> {
    try {
      this.isLoading = true;
      this.validationResults['msal'] = await this.setupService.validateMsal().toPromise() ||
        { isValid: false, message: 'Failed to validate MSAL', details: '' };

      const result = this.validationResults['msal'];
      if (result.isValid) {
        this.snackBar.open('MSAL validation successful!', 'Close', { duration: 3000 });
      } else {
        this.snackBar.open(`MSAL validation failed: ${result.message}`, 'Close', { duration: 5000 });
      }
    } catch (error) {
      console.error('MSAL validation failed:', error);
      this.snackBar.open('MSAL validation failed', 'Close', { duration: 5000 });
    } finally {
      this.isLoading = false;
    }
  }

  canProceedToSetup(): boolean {
    return this.validationResults['docker']?.isValid &&
           this.validationResults['msal']?.isValid &&
           this.setupStatus?.databaseConfigured === true;
  }

  async initializeSystem(): Promise<void> {
    if (!this.setupForm.valid) {
      this.snackBar.open('Please fill in all required fields', 'Close', { duration: 3000 });
      return;
    }

    this.isInitializing = true;
    try {
      const request: InitializeSystemRequest = {
        adminEmail: this.setupForm.value.adminEmail,
        adminName: this.setupForm.value.adminName,
        domain: this.setupForm.value.domain
      };

      this.initializationResult = await this.setupService.initializeSystem(request).toPromise() || null;

      if (this.initializationResult?.success) {
        this.snackBar.open('System initialized successfully!', 'Close', { duration: 3000 });
        this.currentStep = 3; // Move to completion step
      } else {
        throw new Error(this.initializationResult?.message || 'Initialization failed');
      }
    } catch (error: any) {
      console.error('System initialization failed:', error);
      this.snackBar.open(`Initialization failed: ${error.message}`, 'Close', { duration: 5000 });
    } finally {
      this.isInitializing = false;
    }
  }

  copyBackupKey(): void {
    if (this.initializationResult?.backupKey) {
      navigator.clipboard.writeText(this.initializationResult.backupKey).then(() => {
        this.snackBar.open('Backup key copied to clipboard!', 'Close', { duration: 2000 });
      }).catch(() => {
        this.snackBar.open('Failed to copy backup key', 'Close', { duration: 3000 });
      });
    }
  }

  completePlainSetup(): void {
    this.snackBar.open('Setup completed! Redirecting to dashboard...', 'Close', { duration: 2000 });
    setTimeout(() => {
      this.router.navigate(['/dashboard']);
    }, 2000);
  }

  getStatusIcon(isValid: boolean | undefined): string {
    if (isValid === undefined) return 'help';
    return isValid ? 'check_circle' : 'error';
  }

  getStatusColor(isValid: boolean | undefined): string {
    if (isValid === undefined) return 'warn';
    return isValid ? 'primary' : 'warn';
  }
}
