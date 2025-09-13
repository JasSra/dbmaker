import { chromium, FullConfig } from '@playwright/test';

async function globalSetup(config: FullConfig) {
  console.log('🚀 Starting global setup for DbMaker E2E tests...');
  
  // Launch a browser to perform setup tasks
  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();
  
  try {
    // Wait for the application to be ready
    const baseUrl = process.env.BASE_URL || 'http://localhost:4200';
    const apiUrl = process.env.API_URL || 'http://localhost:5021';
    
    console.log(`📊 Checking frontend health at ${baseUrl}`);
    await page.goto(baseUrl, { waitUntil: 'networkidle' });
    
    console.log(`🔍 Checking API health at ${apiUrl}`);
    const response = await page.request.get(`${apiUrl}/api/health`);
    
    if (!response.ok()) {
      throw new Error(`API health check failed: ${response.status()}`);
    }
    
    console.log('✅ Global setup completed successfully');
    
    // Store any global state needed for tests
    await context.storageState({ path: './test-state.json' });
    
  } catch (error) {
    console.error('❌ Global setup failed:', error);
    throw error;
  } finally {
    await context.close();
    await browser.close();
  }
}

export default globalSetup;