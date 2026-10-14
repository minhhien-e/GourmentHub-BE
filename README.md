# GourmetHub Platform — Backend API

A production-ready RESTful Web API for a food e-commerce platform, built with **ASP.NET Core 9 / .NET 9** following **Clean Architecture (Onion Architecture)** principles.

---

## Architecture Overview

```
┌────────────────────────────────────────────────────────────────┐
│                        Presentation Layer                      │
│         ECommerce.Web  ·  Ecommerce.Presentation               │
│           (ASP.NET Core Host · Controllers · Middlewares)      │
├────────────────────────────────────────────────────────────────┤
│                        Core Layer                              │
│   Ecommerce.Domain          Ecommerce.Abstraction              │
│   (Entities · Specs)        (Interfaces · DTOs)                │
│                                                                │
│   Ecommerce.Service         Ecommerce.Shared                   │
│   (Business Logic)          (DTOs · Error Models)              │
├────────────────────────────────────────────────────────────────┤
│                    Infrastructure Layer                        │
│   Ecommerce.Presistence     (EF Core · Repositories · UoW)    │
└────────────────────────────────────────────────────────────────┘
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 9 / C# 13 |
| Web Framework | ASP.NET Core 9 Web API |
| ORM | Entity Framework Core 9 |
| Database | SQL Server |
| Cache / Cart | Redis (StackExchange.Redis) |
| Identity | ASP.NET Core Identity |
| Auth | JWT Bearer + Google OAuth 2.0 |
| Payments | Stripe API (Payment Intent flow) |
| Mapping | AutoMapper |
| Docs | Swagger / OpenAPI |

---

## Features

- **Products** — CRUD with filtering, searching, pagination, and sorting via the Specification Pattern
- **Shopping Cart** — Redis-backed, schema-free cart management
- **Orders** — Full order lifecycle with multiple delivery methods
- **Payments** — Stripe Payment Intent creation and confirmation
- **Authentication** — JWT token-based auth with role-based access control (RBAC)
- **Google OAuth 2.0** — Social login flow
- **Response Caching** — Custom cache attribute backed by Redis
- **Global Error Handling** — Centralised exception middleware with consistent error envelopes

---

## Project Structure

```
GourmetHub-Platform/
├── Core/
│   ├── Ecommerce.Domain/          # Entities, value objects, Specification Pattern
│   ├── Ecommerce.Abstraction/     # Service & repository interfaces
│   ├── Ecommerce.Service/         # Business logic, AutoMapper profiles
│   └── Ecommerce.Shared/          # DTOs, common error models
├── Infrastructure/
│   ├── Ecommerce.Presistence/     # EF Core, migrations, repositories, UoW
│   └── Ecommerce.Presentation/   # Controllers, custom attributes
└── ECommerce.Web/                 # ASP.NET Core host, middleware, startup
```

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local or remote)
- Redis (local: `redis-server`, or Docker: `docker run -p 6379:6379 redis`)
- A [Stripe](https://stripe.com) test account
- *(Optional)* Google OAuth credentials

### 1. Clone the repository

```bash
git clone https://github.com/<your-github-username>/GourmetHub-Platform.git
cd GourmetHub-Platform
```

### 2. Configure secrets

Edit `ECommerce.Web/appsettings.Development.json` (never commit real secrets):

```json
{
  "ConnectionStrings": {
    "DefaultConnection":  "Server=.;Database=GourmetHubDb;Trusted_Connection=True;TrustServerCertificate=True;",
    "IdentityConnection": "Server=.;Database=GourmetHubDb;Trusted_Connection=True;TrustServerCertificate=True;",
    "RedisConnection":    "localhost:6379"
  },
  "JWTOptions": {
    "SecurityKey": "<your-256-bit-secret>",
    "Issuer":      "https://localhost:7109/",
    "Audience":    "https://localhost:7109/api"
  },
  "StripeSettings": {
    "secretKey": "sk_test_..."
  },
  "Authentication": {
    "Google": {
      "ClientId":     "<google-client-id>",
      "ClientSecret": "<google-client-secret>"
    }
  }
}
```

### 3. Apply migrations & seed data

```bash
dotnet ef database update --project Infrastructure/Ecommerce.Presistence --startup-project ECommerce.Web
```

Data is also seeded automatically on first run via `IdataSeed`.

### 4. Run the API

```bash
dotnet run --project ECommerce.Web
```

Swagger UI: `https://localhost:7109/swagger`

---

## API Endpoints

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/product` | — | List products (filter, search, page) |
| `GET` | `/api/product/{id}` | — | Get product by ID |
| `GET` | `/api/cart/{cartId}` | — | Get cart |
| `POST` | `/api/cart` | — | Create / update cart |
| `DELETE` | `/api/cart/{cartId}` | — | Delete cart |
| `POST` | `/api/order` | JWT | Create order |
| `GET` | `/api/order/my-orders` | JWT | Get current user's orders |
| `GET` | `/api/order/{id}` | JWT | Get order by ID |
| `GET` | `/api/order/delivery-methods` | JWT | List delivery methods |
| `POST` | `/api/payment/{cartId}` | JWT | Create / update Stripe Payment Intent |
| `POST` | `/api/authentication/login` | — | Login → returns JWT |
| `POST` | `/api/authentication/register` | — | Register new account |
| `GET` | `/api/authentication/me` | JWT | Get current user profile |
| `GET` | `/api/authentication/address` | JWT | Get user address |
| `PUT` | `/api/authentication/address` | JWT | Update user address |
| `GET` | `/api/authentication/google-login` | — | Initiate Google OAuth |
| `GET` | `/api/authentication/google-callback` | — | Google OAuth callback → returns JWT |

---

## Design Patterns

- **Clean Architecture** — strict dependency rule (outer layers depend on inner layers only)
- **Specification Pattern** — composable, reusable query specifications for products
- **Generic Repository + Unit of Work** — consistent data access abstraction
- **Lazy Initialisation via ServiceManager** — services are only instantiated when first used
- **Custom Cache Attribute** — Redis-backed response caching via `[Cache(seconds)]`

---

## License

This project is open-sourced for portfolio and learning purposes.
