# TODO

## Observability

- [x] Add observability stack and instrumentation for metrics, tracing, and alerting.
- [x] Add OpenTelemetry for traces, metrics, and correlation across database, HTTP, and cache operations.

## User Workflows

- [x] Add user registration workflow.
- [x] Add user lifecycle workflows such as activation, deactivation, and role management.

## Tenant Management

- [ ] Add tenant creation workflow.
- [ ] Add tenant removal workflow.

## Product Data

- [x] Add workflow for attaching `ProductData` records to products.
- [x] Support many-to-many relationship where a single product can have multiple `ProductData` entries.

## Notifications

- [ ] Add email notification for user registration.
- [ ] Add email notification for tenant invitation workflow.
- [ ] Add email notification for password reset workflow.
- [ ] Add email notification for user role changes.

## Contracts

- [ ] Extract request/response DTOs and shared contract models into a separate NuGet package.

## Search

- [x] Add full-text search for products and categories.
- [x] Add faceted filtering for search results.

## Background Jobs

- [x] Add cleanup jobs for expired or orphaned data.
- [x] Add reindex jobs for search data.
- [x] Add retry jobs for failed notifications.
- [x] Add periodic synchronization tasks for external integrations.
- [x] Cursor-based pagination for orphaned ProductData cleanup to bound memory usage at scale.
- [x] Distributed locking (`SELECT ... FOR UPDATE SKIP LOCKED` or claim column) for email retry to prevent duplicate sends in multi-instance deployments.
- [x] Migrate from `PeriodicTimer` to Quartz.NET (or TickerQ) for CRON scheduling, persistent job state, and distributed locking.

## Permissions

- [ ] Add a finer-grained permissions model beyond roles.
- [ ] Add policy-based access control per action and resource.

## File and Media Handling

- [ ] Add file upload support for `ProductData`.
- [ ] Add storage abstraction for local and S3-compatible backends.
- [ ] Add cleanup workflow for orphaned files.


## Soft delete and Data Retention
- [x] Hard delete for soft-deleted products after a configurable retention period.
- [x] Add workflow for permanently deleting soft-deleted products after retention period.
- [ ] MassTransit Outbox, Wolverine, CAP, or other reliable messaging for eventual consistency in data deletion across related entities.
