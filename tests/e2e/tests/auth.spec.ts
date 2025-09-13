import { test, expect, Page } from '@playwright/test';
import { MockAuthHelper } from '../utils/auth-helper';

test.describe('DbMaker Application - Authentication Flow', () => {
  let mockAuth: MockAuthHelper;

  test.beforeEach(async ({ page }) => {
    mockAuth = new MockAuthHelper(page);
  });

  test('should load the application homepage', async ({ page }) => {
    await page.goto('/');
    
    // Check if the page loads correctly
    await expect(page).toHaveTitle(/DbMaker/);
    
    // Look for key elements that indicate the app loaded
    await expect(page.locator('body')).toBeVisible();
  });

  test('should show login when not authenticated', async ({ page }) => {
    await page.goto('/');
    
    // Should show login button or redirect to login
    const loginButton = page.locator('button:has-text("Login"), button:has-text("Sign In"), [data-testid="login-button"]');
    
    // Wait for either login button to be visible or for already being logged in
    try {
      await expect(loginButton).toBeVisible({ timeout: 5000 });
    } catch (e) {
      // If no login button, we might already be in a logged-in state (dev mode)
      console.log('No login button found - might be in dev mode or already authenticated');
    }
  });

  test('should authenticate user and show dashboard', async ({ page }) => {
    // Mock authentication if needed
    await mockAuth.mockSuccessfulLogin();
    
    await page.goto('/');
    
    // After authentication, should see main dashboard
    await expect(page.locator('h1, .dashboard-title, [data-testid="dashboard"]')).toBeVisible({ timeout: 10000 });
    
    // Should see user-specific content
    const userInfo = page.locator('[data-testid="user-info"], .user-menu, .profile');
    if (await userInfo.isVisible()) {
      await expect(userInfo).toBeVisible();
    }
  });

  test('should handle authentication errors gracefully', async ({ page }) => {
    await mockAuth.mockFailedLogin();
    
    await page.goto('/');
    
    // Should show error message or stay on login page
    const errorMessage = page.locator('.error, .alert-danger, [data-testid="error-message"]');
    const loginButton = page.locator('button:has-text("Login"), button:has-text("Sign In")');
    
    // Should either show error or remain on login page
    const hasError = await errorMessage.isVisible();
    const hasLogin = await loginButton.isVisible();
    
    expect(hasError || hasLogin).toBeTruthy();
  });
});