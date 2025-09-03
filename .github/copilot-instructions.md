# DbMaker - Container Database Orchestration System

This is a comprehensive system for managing per-user database containers with Angular frontend, .NET backend, and Docker orchestration.

## Architecture
- **Frontend**: Angular with MSAL.js authentication
- **Backend**: .NET 8 API with SQLite for user/container management
- **Containers**: Per-user Redis and PostgreSQL databases
- **Workers**: Container monitoring and cleanup services
- **Proxy**: Nginx for subdomain-based routing

## Features
- User authentication via MSAL.js
- Template-driven database container creation
- Smart port management for container isolation
- Real-time container monitoring with SSE
- Automated cleanup and maintenance
- Subdomain-driven connection routing

## Project Structure
- `/src/frontend/` - Angular application
- `/src/backend/` - .NET API and workers
- `/docker/` - Container templates and configurations
- `/nginx/` - Reverse proxy configuration
- `/scripts/` - Deployment and utility scripts

## Development Status
✅ Project structure created
🔄 Setting up components...
