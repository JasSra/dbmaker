# DbMaker - Container Database Orchestration System

A comprehensive container orchestration system for creating and managing per-user database containers (Redis, PostgreSQL) with subdomain-based routing and authentication.

## System Architecture

- **Frontend**: Angular 17+ with MSAL.js authentication
- **Backend**: .NET 8 Web API with Entity Framework Core and SQLite
- **Container Management**: Docker.DotNet for container orchestration
- **Authentication**: Azure AD with MSAL integration
- **Reverse Proxy**: nginx with dynamic subdomain routing
- **Monitoring**: Server-Sent Events for real-time container status
- **Workers**: Background services for monitoring and cleanup

## Key Features

- **Template-driven Database Creation**: Supports Redis and PostgreSQL containers
- **Smart Port Management**: Handles hundreds of containers with port allocation
- **Subdomain Routing**: `console.mydomain.com` → Angular frontend, `user-container-type.mydomain.com` → database containers
- **Real-time Monitoring**: SSE events for container status updates
- **Automatic Cleanup**: Background workers for inactive container management
- **Per-user Isolation**: Each user can have multiple database instances

## Quick Start

### Prerequisites

- Docker and Docker Compose
- .NET 8 SDK (for development)
- Node.js 18+ (for frontend development)
- Domain with wildcard DNS pointing to your server

### Configuration

1. **Azure AD Configuration**: Set up your MSAL configuration in `src/frontend/dbmaker-frontend/src/app/msal-config.ts`

2. **Domain Configuration**: Update your DNS to point `*.mydomain.com` to your server

3. **Environment Variables**: Configure the following:
   ```bash
   # API Configuration
   ASPNETCORE_ENVIRONMENT=Production
   ConnectionStrings__DefaultConnection=Data Source=/app/data/dbmaker.db
   ```

### Deployment

1. **Clone and Build**:
   ```bash
   git clone <repository>
   cd DbMaker
   ```

2. **Start the System**:
   ```bash
   docker-compose up -d
   ```

3. **Access the Application**:
   - Frontend: `http://console.mydomain.com`
   - API Health: `http://localhost:5000/health`

## Container Port Allocation

- **System Ports**: 80 (nginx), 5000 (API), 4200 (frontend)
- **User Database Ports**: 10000-19999 (dynamically allocated)
- **Health Check Port**: 8080 (nginx health endpoint)

## Subdomain Routing

The nginx reverse proxy handles subdomain-based routing:

- `console.mydomain.com` → Angular frontend application
- `{username}-{containertype}-{id}.mydomain.com` → User database containers
- Dynamic upstream configuration for container access

## Development

### Backend Development

```bash
cd src/backend
dotnet restore
dotnet run --project DbMaker.API
```

### Frontend Development

```bash
cd src/frontend/dbmaker-frontend
npm install
npm start
```

### Database Migrations

```bash
cd src/backend/DbMaker.API
dotnet ef database update
```

## API Endpoints

### Authentication
- `POST /api/auth/login` - User authentication
- `GET /api/users/profile` - User profile information

### Container Management
- `GET /api/containers` - List user containers
- `POST /api/containers` - Create new container
- `DELETE /api/containers/{id}` - Remove container

### Templates
- `GET /api/templates` - Available database templates
- `POST /api/templates` - Create new template

### Monitoring
- `GET /events/containers` - SSE endpoint for container events

## Container Templates

### Redis Template
```json
{
  "name": "Redis",
  "type": "redis",
  "image": "redis:7-alpine",
  "defaultPort": 6379,
  "environmentVariables": {},
  "connectionStringTemplate": "redis://{subdomain}:{port}"
}
```

### PostgreSQL Template
```json
{
  "name": "PostgreSQL",
  "type": "postgresql",
  "image": "postgres:15-alpine",
  "defaultPort": 5432,
  "environmentVariables": {
    "POSTGRES_DB": "{username}_db",
    "POSTGRES_USER": "{username}",
    "POSTGRES_PASSWORD": "{generated_password}"
  },
  "connectionStringTemplate": "postgresql://{username}:{password}@{subdomain}:{port}/{database}"
}
```

## Monitoring and Cleanup

### Container Monitoring Worker
- Monitors container health every 30 seconds
- Sends SSE events to connected clients
- Updates container status in database

### Container Cleanup Worker
- Removes containers inactive for 24+ hours
- Cleans up database records
- Reclaims allocated ports

## Security Features

- **Azure AD Authentication**: Secure user authentication via MSAL
- **Container Isolation**: Each user's containers are isolated
- **Port Security**: Dynamic port allocation prevents conflicts
- **Network Isolation**: Docker network segmentation

## Scaling Considerations

- **Port Range**: 10,000 ports available for user containers
- **Horizontal Scaling**: Workers can be scaled independently
- **Database**: SQLite for simplicity, can be replaced with SQL Server/PostgreSQL
- **Load Balancing**: nginx can be configured for multiple API instances

## Troubleshooting

### Container Creation Issues
- Check Docker daemon connectivity
- Verify port availability
- Review container logs: `docker logs <container_name>`

### Authentication Issues
- Verify MSAL configuration
- Check Azure AD app registration
- Review network connectivity to login.microsoftonline.com

### Subdomain Routing Issues
- Verify DNS wildcard configuration
- Check nginx configuration reload
- Review nginx access logs

## File Structure

```
DbMaker/
├── src/
│   ├── backend/
│   │   ├── DbMaker.API/          # Main API project
│   │   ├── DbMaker.Shared/       # Shared models and services
│   │   └── DbMaker.Workers/      # Background workers
│   └── frontend/
│       └── dbmaker-frontend/     # Angular application
├── docker/
│   ├── redis/                    # Redis container template
│   └── postgresql/               # PostgreSQL container template
├── nginx/                        # Reverse proxy configuration
├── docker-compose.yml            # Complete system orchestration
└── README.md
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

MIT License - see LICENSE file for details - Container Database Orchestration System

A comprehensive system for managing per-user database containers with Angular frontend, .NET backend, and Docker orchestration.

## 🏗️ Architecture

- **Frontend**: Angular 17+ with MSAL.js authentication
- **Backend**: .NET 8 API with SQLite for user/container management
- **Containers**: Per-user Redis and PostgreSQL databases
- **Workers**: Container monitoring and cleanup services
- **Proxy**: Nginx for subdomain-based routing

## ✨ Features

- 🔐 User authentication via MSAL.js
- 🐳 Template-driven database container creation
- 🔌 Smart port management for container isolation
- 📊 Real-time container monitoring with Server-Sent Events
- 🧹 Automated cleanup and maintenance
- 🌐 Subdomain-driven connection routing

## 📁 Project Structure

```
src/
├── frontend/           # Angular application
├── backend/           # .NET API and workers
│   ├── API/          # Main API project
│   ├── Workers/      # Background services
│   └── Shared/       # Common libraries
docker/               # Container templates
nginx/                # Reverse proxy config
scripts/              # Deployment scripts
```

## 🚀 Quick Start

1. **Prerequisites**
   - Node.js 18+
   - .NET 8 SDK
   - Docker & Docker Compose
   - Nginx (for production)

2. **Development Setup**
   ```bash
   # Start the entire stack
   docker-compose up -d
   
   # Or run individually:
   cd src/backend && dotnet run
   cd src/frontend && ng serve
   ```

3. **Configuration**
   - Update MSAL configuration in `src/frontend/src/app/auth-config.ts`
   - Set database connection in `src/backend/API/appsettings.json`
   - Configure domains in `nginx/nginx.conf`

## 🔧 Configuration

### MSAL Authentication
Configure your Azure AD app registration and update the auth settings.

### Domain Routing
- `console.mydomain.com` → Angular frontend
- `{user-id}-redis.mydomain.com` → User's Redis instance
- `{user-id}-postgres.mydomain.com` → User's PostgreSQL instance

## 🐳 Container Management

The system automatically manages containers with:
- **Port allocation**: Smart port management for 100+ containers
- **Templates**: Extensible system for adding new database types
- **Monitoring**: Real-time status updates via SSE
- **Cleanup**: Automated container and data cleanup

## 📊 Monitoring

- Container health checks
- Resource usage monitoring  
- Automatic failover and restart
- User dashboard with real-time updates

## 🛠️ Development

### Adding New Database Types
1. Create template in `docker/templates/`
2. Add configuration in `src/backend/Shared/Models/DatabaseTemplate.cs`
3. Update frontend UI in `src/frontend/src/app/dashboard/`

### API Endpoints
- `/api/auth/` - Authentication endpoints
- `/api/containers/` - Container management
- `/api/users/` - User management
- `/api/monitoring/` - Real-time monitoring

## 🏭 Production Deployment

1. Configure nginx with SSL certificates
2. Set up monitoring and logging
3. Configure backup strategies
4. Set resource limits for containers

## 📝 License

MIT License - See LICENSE file for details
