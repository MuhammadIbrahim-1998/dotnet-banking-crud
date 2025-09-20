# ATM Console App (.NET + SQL Server)

A simple ATM console application built with .NET and ADO.NET that performs full CRUD on a SQL Server database:
- Create account
- Login
- List accounts
- Deposit
- Withdraw
- Transfer (transaction-safe)
- Update profile (name/password)
- Delete account

## Tech Stack

- .NET 8 console app
- Microsoft.Data.SqlClient (ADO.NET for SQL Server)
- SQL Server (local default instance)

## Prerequisites

- .NET 8 SDK
- SQL Server running locally (default instance)
- Optional: SQL Server Management Studio (SSMS)

## Database Setup

Create the database and table:

```sql
CREATE DATABASE Atmdatabase;
GO
USE Atmdatabase;
GO
CREATE TABLE Accounts
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Accountnumber NVARCHAR(50) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    Password NVARCHAR(100) NOT NULL,
    Balance DECIMAL(18,2) NOT NULL DEFAULT 0
);
