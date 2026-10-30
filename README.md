# Kwarta

A personal budget tracker built with .NET MAUI (Android, iOS, macOS, Windows).

## Features

- **Overview** – monthly balance, total income and expenses, spending per category against its budget, recent transactions, one-tap quick adds, upcoming bills, and a reminder when you haven't backed up in 30 days.
- **Transactions** – browse by month (or all months at once), grouped by day; search by category, account or note; filter to income, expenses or transfers; tap to edit, swipe to delete.
- **Accounts** – cash, e-wallets and banks with their balances; transfers between them; **Adjust** sets an account to its real balance with a correcting entry that isn't counted as income or spending.
- **Budgets** – a monthly limit per expense category (green → amber at 80% → red when over), optionally **rolling over** what's left into the next month.
- **More**
  - **Reports** – income vs. spending for the last 6 months, change from last month, average spending, and a breakdown by category.
  - **Recurring & bills** – weekly, monthly or yearly transactions. Each is either added automatically or, for bills whose amount changes, waits on the Overview to be recorded or skipped. Optional reminder notifications (Android, iOS, macOS).
  - **Savings goals** – a target amount, tracked by a linked account's balance or updated by hand.
  - **Utang** – money lent or borrowed, with payments; kept apart from account balances.
  - **Quick add** – saved entries such as "Jeep fare ₱15 · Cash", added for today with one tap.
  - **Backup & export** – export everything to a JSON file and import it back (replaces all data), export transactions to CSV for spreadsheets, and a daily or weekly automatic backup to a folder (Android, Windows) or the app's Documents folder (iOS, macOS).
  - **App lock** – a 4–6 digit PIN, with fingerprint, face or Windows Hello, asked for at launch and after 30 seconds away.

All data is stored locally in SQLite (`kwarta.db3` in the app data folder). Amounts are shown in Philippine pesos (₱).

## Project layout

```
Kwarta/
  Models/       SQLite tables: Category, Account, Transaction, RecurringTransaction, TransactionTemplate,
                SavingsGoal, Debt
  Services/     BudgetDatabase (data access), BackupFile/BackupService (JSON + CSV, auto-backup),
                RecurringService + Recurrence (schedules), Reminders (notifications), AppLock (PIN),
                BudgetMath (rollover), Money (₱ formatting/parsing)
  ViewModels/   MVVM view models (CommunityToolkit.Mvvm)
  Views/        XAML pages plus shared controls (MonthHeader, TransactionRow, BudgetRow, PillGroupView, MeterBar)
  Platforms/    Platform code: Android reminder alarms and folder picker result, Windows window handle
Kwarta.Tests/   xUnit tests for the platform-independent logic (schedules, rollover, backup format, money)
```

## Run

```
dotnet build Kwarta/Kwarta.csproj -f net10.0-windows10.0.19041.0 -t:Run
```

Or open `Kwarta.slnx` in Visual Studio and pick a target (Windows Machine or an Android emulator).

## Test

```
dotnet test Kwarta.Tests
```
