# 🚀 DbMaker Local Development Guide

## Prerequisites

- **Node.js 18+** (for Angular frontend)
- **.NET 8 SDK** (for backend API)
- **Docker Desktop** (for container management)
- **PowerShell/Terminal** (for running commands)

## 🔧 Step-by-Step Local Setup

### 1. Clone and Navigate

```bash
git clone <repository>
cd DbMaker
```

### 2. Backend Setup (.NET API)

```bash
# Navigate to backend
cd src/backend

# Restore packages for all projects
dotnet restore

# Setup database
cd DbMaker.API
dotnet ef database update

# Run the API
dotnet run
```

**Backend will be available at: `http://localhost:5000`**

### 3. Frontend Setup (Angular)

Open a **new terminal window**:

```bash
# Navigate to frontend
cd src/frontend/dbmaker-frontend

# Install dependencies
npm install

# Start development server
npm start
```

**Frontend will be available at: `http://localhost:4200`**

### 4. Environment Configuration

#### Azure B2C Configuration (Already Applied)
Your B2C settings are now configured in `src/frontend/dbmaker-frontend/src/app/auth-config.ts`:

- **Client ID**: `c83c5908-2b64-4304-8c53-b964ace5a1ea`
- **Authority**: `https://jsraauth.b2clogin.com/jsraauth.onmicrosoft.com/B2C_1_SIGNUP_SIGNIN/v2.0`
- **Known Authorities**: `jsraauth.b2clogin.com`

## 🐳 Alternative: Docker Development

For containerized development:

```bash
# Start backend services only
docker-compose -f docker-compose.dev.yml up -d

# Frontend runs on host for hot reload
cd src/frontend/dbmaker-frontend
npm start
```

## 🌐 Access Points

- **Frontend**: http://localhost:4200
- **API**: http://localhost:5000
- **API Health**: http://localhost:5000/health
- **Swagger**: http://localhost:5000/swagger (if enabled)

## 🔍 Development Workflow

### Starting Development Session

1. **Terminal 1** - Backend API:
   ```bash
   cd src/backend/DbMaker.API
   dotnet run
   ```

2. **Terminal 2** - Frontend:
   ```bash
   cd src/frontend/dbmaker-frontend
   npm start
   ```

3. **Terminal 3** - Workers (optional):
   ```bash
   cd src/backend/DbMaker.Workers
   dotnet run
   ```

### Hot Reload

- **Backend**: Automatically reloads on code changes
- **Frontend**: Live reload on save (Angular CLI)
- **Database**: Changes require `dotnet ef database update`

## 🛠️ Development Tools

### Visual Studio Code Extensions
- **C# Dev Kit** - .NET development
- **Angular Language Service** - Angular support
- **Docker** - Container management
- **REST Client** - API testing

### Useful Commands

```bash
# Backend
dotnet watch run                    # Hot reload API
dotnet ef migrations add <name>     # Create migration
dotnet ef database update           # Apply migrations

# Frontend
ng generate component <name>        # Generate component
ng build                           # Build for production
ng test                            # Run tests

# Docker
docker ps                          # List running containers
docker logs <container-name>       # View container logs
```

## 🔧 Troubleshooting

### Common Issues

1. **Port Conflicts**
   - API: Change port in `launchSettings.json`
   - Frontend: `ng serve --port 4201`

2. **CORS Issues**
   - Ensure API CORS allows `http://localhost:4200`
   - Check `Program.cs` CORS configuration

3. **Authentication Errors**
   - Verify B2C tenant settings
   - Check redirect URIs in Azure portal
   - Clear browser cache/localStorage

4. **Database Issues**
   ```bash
   # Reset database
   cd src/backend/DbMaker.API
   rm dbmaker.db
   dotnet ef database update
   ```

### Health Checks

```bash
# Check API health
curl http://localhost:5000/health

# Check if ports are open
netstat -an | findstr :5000
netstat -an | findstr :4200
```

## 📝 Testing

### Unit Tests
```bash
# Backend tests
cd src/backend
dotnet test

# Frontend tests
cd src/frontend/dbmaker-frontend
npm test
```

### Manual Testing
1. Navigate to http://localhost:4200
2. Should redirect to B2C login
3. After login, access dashboard
4. Create a test container
5. Monitor container status

## 🚀 Production Deployment

When ready for production:

```bash
# Full production deployment
./deploy.sh prod

# Or Windows
deploy.bat prod
```

## 📋 Next Development Tasks

See [TODOS.md](TODOS.md) for remaining development tasks and improvements.
