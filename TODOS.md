# 📋 DbMaker - Remaining TODOs and Development Tasks

## 🔴 High Priority - Required for MVP

### 1. Missing Component Templates
- [ ] **Dashboard HTML Template** - `src/frontend/dbmaker-frontend/src/app/dashboard/dashboard.html`
- [ ] **Container List HTML Template** - `src/frontend/dbmaker-frontend/src/app/container-list/container-list.html`
- [ ] **Create Container HTML Template** - `src/frontend/dbmaker-frontend/src/app/create-container/create-container.html`

### 2. Service Implementation Issues
- [ ] **UserService.getUserStats()** - Method referenced in dashboard but may not exist
- [ ] **Error Handling** - Add proper error handling to all services
- [ ] **Loading States** - Add loading indicators for async operations

### 3. Backend Configuration
- [ ] **Health Endpoint** - Add `/health` endpoint for monitoring
- [ ] **CORS Configuration** - Ensure proper CORS setup for B2C domains
- [ ] **B2C Token Validation** - Configure JWT validation for B2C tokens

### 4. Database Initialization
- [ ] **Database Migrations** - Run initial EF migrations
- [ ] **Seed Data** - Add default templates (Redis, PostgreSQL)
- [ ] **Connection String** - Verify SQLite database path

## 🟡 Medium Priority - Core Features

### 5. Container Management
- [ ] **Template CRUD** - Complete template management endpoints
- [ ] **Port Allocation Logic** - Verify port management works correctly
- [ ] **Container Lifecycle** - Test create/start/stop/delete flows
- [ ] **Subdomain Generation** - Implement dynamic subdomain routing

### 6. Monitoring & Events
- [ ] **SSE Implementation** - Verify Server-Sent Events work
- [ ] **Container Health Checks** - Implement proper health monitoring
- [ ] **Event Broadcasting** - Ensure events reach all connected clients

### 7. Authentication Integration
- [ ] **B2C Scope Configuration** - Verify API scope permissions
- [ ] **Token Refresh** - Handle token expiration gracefully
- [ ] **User Profile Creation** - Auto-create user profiles on first login

## 🟢 Low Priority - Nice to Have

### 8. UI/UX Improvements
- [ ] **Responsive Design** - Ensure mobile compatibility
- [ ] **Dark Mode** - Add theme switching
- [ ] **Loading Animations** - Improve user experience
- [ ] **Error Messages** - User-friendly error displays

### 9. Security Enhancements
- [ ] **Input Validation** - Sanitize all user inputs
- [ ] **Rate Limiting** - Prevent API abuse
- [ ] **Audit Logging** - Log important actions
- [ ] **HTTPS Enforcement** - Production SSL configuration

### 10. Performance Optimizations
- [ ] **Connection Pooling** - Optimize database connections
- [ ] **Caching Strategy** - Cache frequently accessed data
- [ ] **Bundle Optimization** - Minimize frontend bundle size

## 🔧 Development Environment TODOs

### 11. Missing Files/Components
```bash
# These files need to be created:
src/frontend/dbmaker-frontend/src/app/dashboard/dashboard.html
src/frontend/dbmaker-frontend/src/app/dashboard/dashboard.scss
src/frontend/dbmaker-frontend/src/app/create-container/create-container.ts
src/frontend/dbmaker-frontend/src/app/create-container/create-container.html
src/frontend/dbmaker-frontend/src/app/create-container/create-container.scss
src/frontend/dbmaker-frontend/src/app/models/container.models.ts
```

### 12. Package Dependencies
- [ ] **Verify Angular Dependencies** - Check for missing packages
- [ ] **Backend NuGet Packages** - Ensure all packages are installed
- [ ] **Docker Dependencies** - Verify Docker.DotNet package

## 🚀 Immediate Action Items

### To Run Locally Right Now:

1. **Create Missing Component Templates**:
   ```bash
   # Navigate to frontend
   cd src/frontend/dbmaker-frontend/src/app
   
   # Create missing HTML templates (see specific templates below)
   ```

2. **Run Backend**:
   ```bash
   cd src/backend/DbMaker.API
   dotnet restore
   dotnet ef database update
   dotnet run
   ```

3. **Run Frontend**:
   ```bash
   cd src/frontend/dbmaker-frontend
   npm install
   npm start
   ```

## 📋 Quick Fix Templates

### Dashboard Template (Minimal)
```html
<div class="dashboard">
  <h1>DbMaker Dashboard</h1>
  <div class="stats">
    <p>Containers: {{containers.length}}</p>
  </div>
  <button (click)="logout()">Logout</button>
</div>
```

### Container List Template (Minimal)
```html
<div class="container-list">
  <h2>My Containers</h2>
  <div *ngFor="let container of containers">
    <p>{{container.name}} - {{container.status}}</p>
  </div>
</div>
```

### Create Container Template (Minimal)
```html
<div class="create-container">
  <h2>Create New Container</h2>
  <form>
    <select>
      <option>Redis</option>
      <option>PostgreSQL</option>
    </select>
    <button type="submit">Create</button>
  </form>
</div>
```

## 🎯 Success Criteria

- [ ] User can login with B2C
- [ ] Dashboard loads without errors
- [ ] Can create a test container
- [ ] Container appears in list
- [ ] Can access container via subdomain
- [ ] Monitoring events work
- [ ] Can delete container

## ⏰ Time Estimates

- **Missing Templates**: 2-4 hours
- **Service Fixes**: 2-3 hours
- **Backend Health Checks**: 1-2 hours
- **Database Setup**: 1 hour
- **Full Testing**: 2-3 hours

**Total Estimated Time to Working MVP: 8-13 hours**
