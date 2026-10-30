# Kwarta

A personal budget tracker built with .NET MAUI (Android, iOS, macOS, Windows).

## Features

- **Overview** – monthly balance, total income and expenses, spending per category against its budget, and recent transactions.
- **Transactions** – browse by month, grouped by day; search by category or note; filter to income or expenses; tap to edit, swipe to delete.
- **Budgets** – set a monthly limit per expense category (green → amber at 80% → red when over), and add, rename, or delete income and expense categories.

All data is stored locally in SQLite (`kwarta.db3` in the app data folder). Amounts are shown in Philippine pesos (₱).

## Project layout

```
Kwarta/
  Models/       Category, Transaction, TransactionType (SQLite tables)
  Services/     BudgetDatabase (data access + default categories), Money (₱ formatting/parsing)
  ViewModels/   MVVM view models (CommunityToolkit.Mvvm)
  Views/        XAML pages plus shared MonthHeader and TransactionRow controls
  Converters/   XAML value converters
```

## Run

```
dotnet build Kwarta/Kwarta.csproj -f net10.0-windows10.0.19041.0 -t:Run
```

Or open `Kwarta.slnx` in Visual Studio and pick a target (Windows Machine or an Android emulator).
