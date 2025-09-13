// Production environment configuration
export const environment = {
  production: true,
  apiUrl: '/api',  // Use relative URL for production
  features: {
    enableMockData: false,
    enableDebugMode: false,
    enableRealTimeMonitoring: true
  }
};
