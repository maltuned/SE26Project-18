# API Deployment

## Manual database initialization

The API never creates the database, calls `EnsureCreated`, applies migrations, or seeds business data in Development or Production. MariaDB 10.11 and the current migration must be provisioned before API startup.

Create the database and separate migration and runtime users externally as a MariaDB administrator. Replace the example passwords and host restrictions:

```sql
CREATE DATABASE se26proj_18
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;
CREATE USER 'se26proj_18_ddl'@'%' IDENTIFIED BY 'replace-ddl-password';
CREATE USER 'se26proj_18_app'@'%' IDENTIFIED BY 'replace-runtime-password';
GRANT ALL PRIVILEGES ON se26proj_18.* TO 'se26proj_18_ddl'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.embedding_sync_outbox TO 'se26proj_18_app'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.game_tags TO 'se26proj_18_app'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.games TO 'se26proj_18_app'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.recruitment_tags TO 'se26proj_18_app'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.user_tags TO 'se26proj_18_app'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.users TO 'se26proj_18_app'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.GameGameTag TO 'se26proj_18_app'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.recruitments TO 'se26proj_18_app'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.refresh_tokens TO 'se26proj_18_app'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.UserUserTag TO 'se26proj_18_app'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.chats TO 'se26proj_18_app'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.recruitment_views TO 'se26proj_18_app'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.RecruitmentRecruitmentTag TO 'se26proj_18_app'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.responses TO 'se26proj_18_app'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE ON se26proj_18.messages TO 'se26proj_18_app'@'%';
GRANT SELECT ON se26proj_18.__EFMigrationsHistory TO 'se26proj_18_app'@'%';
FLUSH PRIVILEGES;
```

From the repository root, use the DDL user's connection string with the initialization script. It verifies that the EF model is represented by committed migrations, displays migration status, and applies pending migrations:

```bash
export ConnectionStrings__Default='Server=localhost;Port=3306;Database=se26proj_18;User=se26proj_18_ddl;Password=replace-ddl-password;'
bash ./SE26Project-18.Api/scripts/init-db.sh
```

The script is safe to rerun: EF only applies migrations that are not recorded in `__EFMigrationsHistory`. It does not create the database or database users and does not seed games, tags, or sample records. Run `bash ./SE26Project-18.Api/scripts/init-db.sh --help` for usage details.

The equivalent manual commands are:

```bash
dotnet ef migrations has-pending-model-changes --project SE26Project-18.Api
dotnet ef migrations list --project SE26Project-18.Api
dotnet ef database update --project SE26Project-18.Api
```

`ConnectionStrings__Default` is mandatory for any command that connects to or changes a database. The design-time factory's deliberately invalid local fallback exists only so offline migration generation and model inspection can configure EF without external services; it is not a deployable credential.

To build and run a migration bundle instead:

```bash
mkdir -p artifacts
dotnet ef migrations bundle --project SE26Project-18.Api --output artifacts/se26project-18-migrate
ConnectionStrings__Default='Server=localhost;Port=3306;Database=se26proj_18;User=se26proj_18_ddl;Password=replace-ddl-password;' ./artifacts/se26project-18-migrate
```

Configure the API with the runtime user's connection string after migration. The runtime user needs DML access to application tables and only `SELECT` access to `__EFMigrationsHistory`, but no `CREATE`, `ALTER`, or `DROP` privileges. Startup performs read-only checks with bounded retries: it validates the actual server as MariaDB 10.11, requires at least one applied migration, rejects pending migrations, and rejects a schema containing migrations unknown to the running API binary. `DatabaseValidation__MaxRetryAttempts` and `DatabaseValidation__RetryDelaySeconds` only control that startup check and never change the schema.

If a database already contains an untracked schema, do not run `InitialCreate`: it will try to create existing objects. Back up the database, compare every table, key, index, collation, and column with the migration, then establish a migration-history baseline through a reviewed operational procedure. Never mark `InitialCreate` as applied merely to bypass startup validation.

The migration creates schema only. It does not insert games, tags, sample records, or other business seed data.

## First administrator

Public registration always creates a normal user. To create the first administrator, set these environment variables for one deployment:

- `AdminBootstrap__Enabled=true`
- `AdminBootstrap__Username=<3-50 character username>`
- `AdminBootstrap__Password=<8-100 character password>`

Apply all database migrations first, then start the API with these settings. Database validation runs before `AdminBootstrapper`; the API creates an administrator only after the schema is current and only when no administrator exists. Startup fails if the configured username already belongs to a normal user. The password is BCrypt-hashed and is never logged by the bootstrap process.

After the administrator has been created, remove `AdminBootstrap__Password` from the deployment secret store and unset or set `AdminBootstrap__Enabled=false`. Do not retain the bootstrap password in source-controlled configuration.

## Multi-instance constraints

WebSocket tickets and active socket tracking are held in each API instance's memory. Ticket creation and connection must therefore reach the same instance (for example, with sticky routing), and suspending a user immediately closes sockets connected to the instance processing the suspension. Other instances still reject that user's next message using current database status, but cross-instance socket closure requires a distributed connection-control mechanism.

Chat activity pagination uses a mutable activity timestamp as its cursor. Concurrent chat activity can move entries between pages, so clients should tolerate eventual consistency, duplicates, or omissions while paging an actively changing chat list.
