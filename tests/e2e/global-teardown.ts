import { FullConfig } from '@playwright/test';
import fs from 'fs';

async function globalTeardown(config: FullConfig) {
  console.log('🧹 Starting global teardown for DbMaker E2E tests...');
  
  try {
    // Clean up any test state files
    if (fs.existsSync('./test-state.json')) {
      fs.unlinkSync('./test-state.json');
      console.log('🗑️ Cleaned up test state file');
    }
    
    // Perform any additional cleanup tasks here
    // For example, clean up test containers or test data
    
    console.log('✅ Global teardown completed successfully');
    
  } catch (error) {
    console.error('❌ Global teardown failed:', error);
    // Don't throw here to avoid masking test failures
  }
}

export default globalTeardown;