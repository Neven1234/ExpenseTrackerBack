# Expense Tracker API

A monthly expense tracker built with ASP.NET Core (.NET 9) following Clean Architecture.

Each user sets a monthly allowance, logs expenses against categories, and whatever is left
over at the end of a month is automatically carried into the next one.

## Carry-over rule

Balances are never stored, they are derived from the months that came before:

```
CarriedOver(month)    = Remaining(previous month)
TotalAvailable(month) = CarriedOver(month) + Allowance(month)
Spent(month)          = sum of the month's expenses
Remaining(month)      = TotalAvailable(month) - Spent(month)
```

So a user who starts June with 20,000, spends 16,000, and receives another 20,000 in July
sees a July total of 24,000. Keeping this derived means a corrected expense in an old month
reflows through every later month with no reconciliation step.

The fold lives in `ExpenseTracker.Domain/Budgeting/BudgetLedger.cs`.

## Projects

| Project | Responsibility | Depends on |
| --- | --- | --- |
| `ExpenseTracker.Domain` | Entities, budget maths, domain exceptions | nothing |
| `ExpenseTracker.Application` | Use cases, DTOs, repository/security contracts | Domain |
| `ExpenseTracker.Infrastructure` | EF Core, repositories, JWT, password hashing | Application |
| `ExpenseTracker.Api` | Controllers, DI, middleware | Application, Infrastructure |

Dependencies point inwards only: the Domain knows nothing about EF Core or ASP.NET, and the
Application talks to storage through interfaces that Infrastructure implements.

## Running it

Requires the .NET 9 SDK and SQL Server LocalDB (ships with Visual Studio).

```bash
dotnet ef database update --project ExpenseTracker.Infrastructure --startup-project ExpenseTracker.Api
dotnet run --project ExpenseTracker.Api
```

Swagger UI is served at `/swagger` in Development.

The connection string lives in `ExpenseTracker.Api/appsettings.json` and points at
`(localdb)\MSSQLLocalDB`. Swap the provider in
`ExpenseTracker.Infrastructure/DependencyInjection.cs` to use a different database.

Before deploying anywhere real, replace `Jwt:SigningKey` with a long random secret held in
user secrets or environment variables rather than in source control.

## Endpoints

All routes except `/api/auth/*` require an `Authorization: Bearer <token>` header, and every
query is scoped to the signed-in user.

| Method | Route | Purpose |
| --- | --- | --- |
| POST | `/api/auth/register` | Create an account, returns a JWT |
| POST | `/api/auth/login` | Sign in, returns a JWT |
| GET | `/api/categories` | List categories |
| GET | `/api/categories/{id}` | Read one category |
| POST | `/api/categories` | Create a category |
| PUT | `/api/categories/{id}` | Rename a category |
| DELETE | `/api/categories/{id}` | Delete a category |
| GET | `/api/budgets` | Every month with its running balance |
| GET | `/api/budgets/{year}/{month}` | One month plus its category breakdown |
| POST | `/api/budgets` | Open a month with an allowance |
| PUT | `/api/budgets/{year}/{month}` | Change the allowance |
| DELETE | `/api/budgets/{year}/{month}` | Delete a month |
| GET | `/api/expenses?year=&month=&categoryId=` | List expenses, optionally filtered |
| GET | `/api/expenses/{id}` | Read one expense |
| POST | `/api/expenses` | Log an expense |
| PUT | `/api/expenses/{id}` | Update an expense |
| DELETE | `/api/expenses/{id}` | Delete an expense |

An expense is filed into a month by its `spentOn` date, so that month's budget must exist first.

## Design notes

- Passwords are hashed with PBKDF2-SHA256, 100,000 iterations and a per-user salt, written
  by hand against the BCL rather than pulled from a package.
- Mapping between entities and DTOs is done with small extension methods, so the shape of an
  API response is plain readable C# instead of runtime configuration.
- Domain exceptions carry the meaning (`NotFoundException`, `ConflictException`) and one
  middleware turns them into the right status code, keeping controllers free of error plumbing.
- A category or month that still has expenses against it cannot be deleted; the API answers
  409 rather than silently discarding history.
