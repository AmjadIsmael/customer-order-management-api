# Customer Order Management API

ASP.NET Core Web API for managing customers, products, and orders.

The project uses a layered architecture with PostgreSQL as the database. EF Core is used for the main CRUD operations, while Dapper is used to execute two PostgreSQL stored procedures for order reporting and search.

Authentication is handled using JWT, with separate permissions for Admin/User staff accounts and
self-registered Customer accounts (see [Authentication](#authentication)).

## Project Structure

The solution contains four main projects:

| Project                               | Purpose                                                                        |
| ------------------------------------- | ------------------------------------------------------------------------------ |
| `CustomerOrderManagement.Domain`      | Entities, DTOs and enums                                                       |
| `CustomerOrderManagement.Business`    | Business logic, services, validation, mapping and authentication               |
| `CustomerOrderManagement.Persistence` | EF Core, repositories, Unit of Work, migrations and Dapper                     |
| `CustomerOrderManagement.API`         | Controllers, Swagger, JWT configuration, rate limiting and application startup |

The general flow is:

```text
Controller
   ↓
Business Service
   ↓
Repository
   ↓
EF Core / Dapper
   ↓
PostgreSQL
```

## Technologies Used

- ASP.NET Core / .NET 10
- Entity Framework Core
- PostgreSQL
- Npgsql
- Dapper
- JWT Authentication
- FluentValidation
- Serilog
- Swagger
- API Versioning
- ASP.NET Core Rate Limiting
- Health Checks
- xUnit
- Moq
- Quartz.NET

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/AmjadIsmael/customer-order-management-api.git
cd customer-order-management-api
dotnet restore
```

### 2. Configure the database and JWT key

The application requires:

```text
ConnectionStrings:DefaultConnection
Jwt:Key
```

For local development, these can be stored using .NET User Secrets.

Move to the API project:

```bash
cd src/CustomerOrderManagement.API
```

Set the PostgreSQL connection string:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=CustomerOrderManagement;Username=postgres;Password=<your-password>"
```

Set the JWT signing key:

```bash
dotnet user-secrets set "Jwt:Key" "<your-secret-key-at-least-32-characters>"
```

Do not commit real passwords or JWT keys to the repository.

Environment variables can also be used:

```text
ConnectionStrings__DefaultConnection
Jwt__Key
```

## Database Setup

Database schema changes are managed using Entity Framework Core migrations.

Migrations are located in:

```text
src/CustomerOrderManagement.Persistence/Migrations
```

In Development, migrations are applied automatically when the API starts.

They can also be applied manually from the repository root:

```bash
dotnet ef database update \
  --project src/CustomerOrderManagement.Persistence \
  --startup-project src/CustomerOrderManagement.API
```

If the EF Core CLI tool is not installed:

```bash
dotnet tool install --global dotnet-ef
```

To create a new migration after changing an entity:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/CustomerOrderManagement.Persistence \
  --startup-project src/CustomerOrderManagement.API
```

## Stored Procedures

The project contains two PostgreSQL stored procedures:

```text
GetCustomerOrderSummary
SearchOrders
```

They are called from the Persistence layer using Dapper.

The SQL script is located at:

```text
database/CreateOrderProcedures.sql
```

After the database tables have been created, run the script using pgAdmin Query Tool or `psql`:

```bash
psql "<your-connection-string>" -f database/CreateOrderProcedures.sql
```

The procedures are not managed by EF Core migrations, so the script should be executed again whenever it changes.

## Running the API

From the repository root:

```bash
dotnet run --project src/CustomerOrderManagement.API
```

Development URLs:

```text
https://localhost:7044
http://localhost:5231
```

Swagger UI is available at:

```text
https://localhost:7044/swagger
```

The health-check endpoint is:

```text
/health
```

## Authentication

The API uses JWT Bearer authentication. There are three roles:

| Role       | How the account is created                          | Access                                                                                          |
| ---------- | ----------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| `Admin`    | Seeded, or created by another Admin                    | Full read/write on customers, products and orders                                                |
| `User`     | Seeded, or created by an Admin                         | Read-only on customers, products and orders                                                      |
| `Customer` | Self-registered via `POST /api/v1/auth/register`       | Read/update **only their own** linked customer record; view **only their own** orders; no writes |

In Development, two staff test users are seeded automatically:

| Username | Password    | Role  |
| -------- | ----------- | ----- |
| `admin`  | `Admin@123` | Admin |
| `user`   | `User@123`  | User  |

To authenticate as staff:

1. Call `POST /api/v1/auth/login`.
2. Copy the returned access token.
3. Open Swagger.
4. Click **Authorize**.
5. Paste the token.
6. Call the protected endpoints.

To sign up as a customer instead, call `POST /api/v1/auth/register` with your account and profile
details — this creates the linked customer record and returns an access token in the same call, so
there's no separate login step needed afterwards.

## API Endpoints

All endpoints are versioned under:

```text
/api/v1/
```

### Authentication

```text
POST /api/v1/auth/login
POST /api/v1/auth/register
```

### Customers

```text
GET    /api/v1/customers
GET    /api/v1/customers/{id}
POST   /api/v1/customers
PUT    /api/v1/customers/{id}
DELETE /api/v1/customers/{id}
```

`GET /{id}` and `PUT /{id}` also allow the `Customer` role for its own linked record. `POST` and
`DELETE` require `Admin`. `GET /customers` (list) requires staff (`Admin`/`User`).

### Products

```text
GET    /api/v1/products
GET    /api/v1/products/{id}
POST   /api/v1/products
PUT    /api/v1/products/{id}
DELETE /api/v1/products/{id}
```

Writes require the `Admin` role.

### Orders

```text
GET    /api/v1/orders
GET    /api/v1/orders/{id}
GET    /api/v1/orders/search
GET    /api/v1/orders/customers/{customerId}/summary

POST   /api/v1/orders
PUT    /api/v1/orders/{id}
DELETE /api/v1/orders/{id}

POST   /api/v1/orders/{id}/items
DELETE /api/v1/orders/{id}/items/{itemId}
```

Writes require the `Admin` role. The `Customer` role can call every read endpoint above but is
always scoped to its own orders, regardless of the `customerId` passed to search.

More information about request models, response models and status codes is available directly in Swagger.

## Validation

Request validation is handled using FluentValidation.

Invalid requests return a `400 Bad Request` response before reaching the Business layer.

## Logging

Serilog is used for application logging and HTTP request logging.

## Rate Limiting

Rate limiting is configured in:

```text
src/CustomerOrderManagement.API/appsettings.json
```

The application uses:

```text
Global API limit: 100 requests per minute
Login limit:      5 requests per minute
```

The stricter login limit helps reduce repeated authentication attempts.

## Background Jobs

A Quartz.NET scheduled job automatically cancels orders that have stayed in the `Pending` status
too long and restores stock for their items. It's configured under:

```text
Jobs:ExpireStalePendingOrders:PendingHours     # default 24
Jobs:ExpireStalePendingOrders:CronExpression   # default "0 0 * * * ?" (hourly)
```

in `src/CustomerOrderManagement.API/appsettings.json` (and overridable per environment in
`appsettings.Development.json`). The job itself lives in
`src/CustomerOrderManagement.API/Jobs/`.

## Health Checks

The API provides a health-check endpoint:

```text
GET /health
```

It verifies that the application is running and that PostgreSQL is reachable.

## Testing

Unit tests focus on the Business layer only (services); controllers are not unit tested.

xUnit is used as the testing framework and Moq is used to mock repositories and the Unit of Work
so tests never touch a real database. Tests live in:

```text
tests/CustomerOrderManagement.Business.Tests
```

Run all tests from the repository root:

```bash
dotnet test
```

Run just this project, or filter to a class/method:

```bash
dotnet test tests/CustomerOrderManagement.Business.Tests
dotnet test tests/CustomerOrderManagement.Business.Tests --filter "FullyQualifiedName~OrderServiceTests"
```

The tests cover both successful and failure scenarios for every Business-layer service
(`AuthService`, `CustomerService`, `ProductService`, `OrderService`, `JwtTokenGenerator`),
following the Arrange-Act-Assert pattern.

## Postman Collection

A ready-to-import collection covering every endpoint (Auth, Customers, Products, Orders, Health)
is at:

```text
postman/CustomerOrderManagement.postman_collection.json
```

Import it into Postman, set the `baseUrl` collection variable if it differs from
`https://localhost:7044`, then run **Auth → Login** (or **Register**) first — its test script
stores the returned token in the `accessToken` collection variable automatically, and creating a
customer/product/order likewise auto-fills `customerId`/`productId`/`orderId` for the rest of the
requests.

## Main Features

- Customer management
- Product management
- Order and OrderItem management
- CRUD operations using Entity Framework Core
- One-to-many and one-to-one entity relationships
- Order stock management
- JWT authentication
- Role-based authorization, including per-record ownership checks for self-registered customers
- Customer self-registration (creates a linked login account and customer profile in one call)
- Automatic cancellation of stale pending orders via a Quartz.NET scheduled job
- FluentValidation
- Serilog logging
- PostgreSQL stored procedures
- Dapper integration
- API versioning
- Rate limiting
- Health checks
- Swagger documentation
- Unit testing with xUnit and Moq
- EF Core migrations
- Layered architecture

## Repository

GitHub:

https://github.com/AmjadIsmael/customer-order-management-api
