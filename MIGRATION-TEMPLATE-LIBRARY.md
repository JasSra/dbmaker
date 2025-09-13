# Migration Guide: Template Library (Backend → Client → Frontend)

This plan migrates DbMaker to a reusable, seedable Template Library. It covers backend schema and APIs, seeding, client generation, and frontend updates (with icon libraries).

## Goals & outcomes

- Support many templates with versions, categories, icons, ports, volumes, default env/config, healthchecks, and a connection string template.
- Replace hardcoded templates in the orchestrator with a repository/resolver.
- Provide read-only Template APIs first (CRUD later if needed).
- Seed from files with idempotent re-runs.
- Maintain backward compatibility with current DatabaseType during rollout.

## Project-wide order

1. Backend: schema, seeding, APIs, orchestrator refactor
2. Client: regenerate via NSwag
3. Frontend: catalog → detail → create

## Backend work

### EF Core schema

Add two entities and migrate with AddTemplateLibrary.

- Template fields: Id (GUID), Key (unique), DisplayName, Category, Icon, Description, IsEnabled, LatestVersion, CreatedAt, UpdatedAt, unique index on Key.
- TemplateVersion fields: Id (GUID), TemplateId (FK), Version (unique per Template), DockerImage, ConnectionStringTemplate, Ports (JSON), Volumes (JSON), DefaultEnvironment (JSON), DefaultConfiguration (JSON), Healthcheck (JSON), CreatedAt, unique index on (TemplateId, Version).
- Reuse existing JSON converters (as done for SystemSettings).
- Keep DatabaseTemplate for in-memory use; map from TemplateVersion when resolving.

### Seeding layout and behavior

Repo layout:

- src/backend/DbMaker.API/Seed/templates/index.json
- src/backend/DbMaker.API/Seed/templates/{key}/template.json
- src/backend/DbMaker.API/Seed/templates/{key}/versions/{version}.json
- Icons: src/backend/DbMaker.API/wwwroot/template-icons/{key}.svg

Seeder behavior:

- Upsert Template by Key from index.json and per-template metadata.
- Upsert each version by (TemplateId, Version).
- Set LatestVersion (from metadata or max version).
- Idempotent: safe to re-run at startup or via a setup endpoint.

Example version (PostgreSQL):

```json
{
  "version": "16-alpine",
  "dockerImage": "postgres:16-alpine",
  "connectionStringTemplate": "postgresql://{POSTGRES_USER}:{POSTGRES_PASSWORD}@localhost:{HOST_PORT}/{POSTGRES_DB}",
  "ports": [{ "containerPort": 5432, "protocol": "tcp" }],
  "volumes": [{ "containerPath": "/var/lib/postgresql/data" }],
  "defaultEnvironment": {
    "POSTGRES_DB": "userdb",
    "POSTGRES_USER": "dbuser",
    "POSTGRES_PASSWORD": "secure_password_123"
  },
  "defaultConfiguration": {},
  "healthcheck": {
    "test": ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"],
    "interval": "30s",
    "timeout": "10s",
    "retries": 3,
    "startPeriod": "10s"
  }
}
```

### Orchestrator refactor

- Add ITemplateRepository and ITemplateResolver (EF-backed).
- Update SafeContainerOrchestrator to resolve DatabaseTemplate by TemplateKey and optional TemplateVersion.
- Map legacy DatabaseType to Template.Key for compatibility.
- Merge overrides with defaults for env/config.
- Build port bindings and volumes from the resolved version.
- Create connection string via token replacement using {HOST_PORT}, {SUBDOMAIN}, and env values.

### API surface (read-only)

- GET /api/templates?category=&q= → list (key, name, category, icon, latestVersion, short description)
- GET /api/templates/{key} → template detail with versions list
- GET /api/templates/{key}/versions/{version} → full version spec
- GET /api/templates/{key}/preview?version=&overrides= → resolved preview including connection string
- (Later/admin) POST/PUT/DELETE as needed

### Backend quality gates

- Build and apply migration.
- Seeder logs and re-runs idempotently.
- Orchestrator creates Redis/Postgres via the new resolver.
- Unit tests: seed parsing, resolver mapping, token replacement, validation.

## Client generation (Angular)

- Ensure nswag.json includes new Template endpoints.
- Use the task: generate-api-client (depends on build-backend).
- Commit outputs to src/frontend/dbmaker-frontend/api.

Prompt:

- Regenerate the Angular API client using NSwag after adding the Template endpoints; place outputs in src/frontend/dbmaker-frontend/api.

## Frontend updates

### Sequence

1. Switch to new client types for templates (catalog/detail).
2. Add creation flow to send TemplateKey with optional TemplateVersion (keep DatabaseType fallback initially).

### Pages and components

- Template Catalog: grid/list with icon, name, category, description; filter by category and search query.
- Template Detail: version dropdown (default LatestVersion); show ports, default env/config, volumes; override controls; optional preview via /preview.
- Create Container: use selected TemplateKey/Version plus overrides; post to create endpoint.

### Icon library options

- Simple Icons (package: simple-icons, CC0). Import a small subset or serve static SVGs; map template.key to icon slug.
- Devicon (broad coverage). Verify licenses as needed.

Implementation notes:

- Add an IconService to resolve key to SVG (prefer local subset). Fallback to a generic DB icon.
- Tree-shake icon imports or copy selected SVGs to /assets/icons.

### Frontend quality gates

- Typecheck/build passes.
- Catalog shows templates with icons.
- Detail shows version info and defaults.
- Create flow works for Redis/PostgreSQL.

## Backward compatibility and rollout

- Phase 1: Ship new Template APIs. Orchestrator maps legacy DatabaseType to a default template.
- Phase 2: Frontend moves to Template-based create; keep DatabaseType fallback.
- Phase 3: Remove hardcoded templates and legacy endpoints.

## Copy/paste prompts (handoffs)

Backend:

- Add EF entities Template and TemplateVersion with JSON properties and create migration AddTemplateLibrary (unique on Template.Key and (TemplateId, Version)).
- Implement a seeder that reads JSON under Seed/templates and upserts templates and versions idempotently; set LatestVersion.
- Introduce ITemplateRepository and ITemplateResolver; refactor SafeContainerOrchestrator to resolve DatabaseTemplate from the Template library; support TemplateKey/TemplateVersion and legacy DatabaseType.
- Add read-only TemplatesController endpoints: list, detail, version, preview.

Client/Frontend:

- Regenerate Angular API client with NSwag to include Template endpoints; output to src/frontend/dbmaker-frontend/api.
- Build a Template Catalog page (list and filters) showing icon, name, category, description using Simple Icons.
- Build a Template Detail page with version selector, defaults (ports/env/config/volumes), and preview.
- Update Create flow to post TemplateKey (and optionally Version) with overrides; keep DatabaseType fallback.

## Definition of done

- DB tables exist; migration applied.
- Seed library loads Redis and PostgreSQL with icons.
- Template APIs return expected payloads.
- Orchestrator creates containers via resolved templates.
- NSwag client updated; frontend catalog/detail/create works with icons.

Last updated: 2025-09-13
