#!/bin/bash

# DbMaker Deployment Script
# Supports both development and production deployments

set -e

ENVIRONMENT=${1:-dev}
COMPOSE_FILE=""

case $ENVIRONMENT in
  "dev"|"development")
    echo "🚀 Starting DbMaker in DEVELOPMENT mode..."
    COMPOSE_FILE="docker-compose.dev.yml"
    ;;
  "prod"|"production")
    echo "🚀 Starting DbMaker in PRODUCTION mode..."
    COMPOSE_FILE="docker-compose.prod.yml"
    ;;
  *)
    echo "❌ Invalid environment. Use 'dev' or 'prod'"
    echo "Usage: ./deploy.sh [dev|prod]"
    exit 1
    ;;
esac

echo "📋 Using compose file: $COMPOSE_FILE"

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker first."
    exit 1
fi

# Build and start services
echo "🔨 Building and starting services..."
docker-compose -f $COMPOSE_FILE up --build -d

echo "⏳ Waiting for services to be ready..."
sleep 10

# Health checks
echo "🔍 Checking service health..."

if [ "$ENVIRONMENT" = "prod" ] || [ "$ENVIRONMENT" = "production" ]; then
    # Production health checks
    echo "Checking nginx health..."
    curl -f http://localhost:8080/health || echo "⚠️  nginx health check failed"
    
    echo "Checking API health..."
    curl -f http://localhost:5000/health || echo "⚠️  API health check failed"
    
    echo "Checking frontend..."
    curl -f http://localhost:4200 || echo "⚠️  Frontend health check failed"
else
    # Development health checks
    echo "Checking API health..."
    curl -f http://localhost:5000/health || echo "⚠️  API health check failed"
    
    echo "ℹ️  Frontend should be started manually with 'npm start' in development mode"
fi

echo "✅ DbMaker deployment complete!"
echo ""
echo "📊 Service Status:"
docker-compose -f $COMPOSE_FILE ps

echo ""
echo "🌐 Access URLs:"
if [ "$ENVIRONMENT" = "prod" ] || [ "$ENVIRONMENT" = "production" ]; then
    echo "  Frontend: http://console.mydomain.com (or http://localhost:4200)"
    echo "  API: http://localhost:5000"
    echo "  nginx Health: http://localhost:8080/health"
else
    echo "  API: http://localhost:5000"
    echo "  Frontend: Start with 'cd src/frontend/dbmaker-frontend && npm start'"
fi

echo ""
echo "📱 Next Steps:"
echo "  1. Configure your MSAL settings in the frontend"
echo "  2. Set up your domain DNS (*.mydomain.com → your server)"
echo "  3. Create your first database container through the UI"

echo ""
echo "🔧 Management Commands:"
echo "  View logs: docker-compose -f $COMPOSE_FILE logs -f [service]"
echo "  Stop: docker-compose -f $COMPOSE_FILE down"
echo "  Restart: docker-compose -f $COMPOSE_FILE restart [service]"
