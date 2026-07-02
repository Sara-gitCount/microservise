# Phase 12: Database Verification

## 🗄️ Objective

Verify that SQL Server database is properly initialized with all schemas, tables, relationships, and data.

## 📋 Prerequisites

- SQL Server 2022 (local or Docker)
- SQL Server Management Studio (SSMS)
- All services running (F5)
- ~10 minutes

## 🔍 Step-by-Step Verification

### Step 1: Connect to Database

```
Open SQL Server Management Studio

Server name: localhost,1433
Authentication: Windows Authentication or SQL Authentication
Connect
```

### Step 2: Verify Database Exists

```sql
-- In Query window, run:
SELECT name FROM sys.databases 
WHERE name IN ('AuthDb', 'CatalogDb', 'OrderDb', 'LotteryDb');

-- Expected result: 4 rows returned
-- AuthDb
-- CatalogDb
-- OrderDb
-- LotteryDb
```

### Step 3: Verify Auth Schema

```sql
-- Switch to AuthDb
USE AuthDb;
GO

-- Check schemas
SELECT schema_name FROM information_schema.schemata
WHERE schema_name = 'Auth';

-- Expected: 1 row
```

### Step 4: Verify Auth Tables

```sql
USE AuthDb;
GO

-- List all tables in Auth schema
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'Auth'
ORDER BY TABLE_NAME;

-- Expected tables:
-- AspNetRoles
-- AspNetRoleClaims
-- AspNetUserClaims
-- AspNetUserLogins
-- AspNetUserRoles
-- AspNetUserTokens
-- AspNetUsers
```

### Step 5: Verify Auth Users Table Structure

```sql
USE AuthDb;
GO

-- Check Users table
EXEC sp_columns 'AspNetUsers';

-- Expected columns:
-- Id
-- UserName
-- NormalizedUserName
-- Email
-- NormalizedEmail
-- EmailConfirmed
-- PasswordHash
-- SecurityStamp
-- ConcurrencyStamp
-- PhoneNumber
-- PhoneNumberConfirmed
-- TwoFactorEnabled
-- LockoutEnd
-- LockoutEnabled
-- AccessFailedCount
-- FullName (custom)
-- CreatedAt (custom)
-- UpdatedAt (custom)
```

### Step 6: Verify Catalog Schema

```sql
-- Switch to CatalogDb
USE CatalogDb;
GO

-- Check schemas
SELECT schema_name FROM information_schema.schemata
WHERE schema_name = 'Catalog';

-- Expected: 1 row
```

### Step 7: Verify Catalog Tables

```sql
USE CatalogDb;
GO

-- List all tables
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'Catalog'
ORDER BY TABLE_NAME;

-- Expected tables:
-- Gifts
-- Categories (if implemented)
```

### Step 8: Verify Gifts Table Structure

```sql
USE CatalogDb;
GO

-- Check Gifts table
EXEC sp_columns 'Gifts';

-- Expected columns:
-- GiftId
-- Name
-- Description
-- Price
-- Stock
-- Category
-- ImageUrl
-- CreatedAt
-- UpdatedAt
```

### Step 9: Verify Orders Schema

```sql
USE OrderDb;
GO

-- Check schemas
SELECT schema_name FROM information_schema.schemata
WHERE schema_name = 'Orders';

-- Expected: 1 row
```

### Step 10: Verify Orders Tables

```sql
USE OrderDb;
GO

-- List all tables
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'Orders'
ORDER BY TABLE_NAME;

-- Expected tables:
-- Orders
-- OrderItems (if implemented)
```

### Step 11: Verify Orders Table Structure

```sql
USE OrderDb;
GO

-- Check Orders table
EXEC sp_columns 'Orders';

-- Expected columns:
-- OrderId
-- UserId
-- GiftId
-- Quantity
-- TotalPrice
-- Status (pending, completed, cancelled)
-- CreatedAt
-- UpdatedAt
```

### Step 12: Verify Lottery Schema

```sql
USE LotteryDb;
GO

-- Check schemas
SELECT schema_name FROM information_schema.schemata
WHERE schema_name = 'Lottery';

-- Expected: 1 row
```

### Step 13: Verify Lottery Tables

```sql
USE LotteryDb;
GO

-- List all tables
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'Lottery'
ORDER BY TABLE_NAME;

-- Expected tables:
-- LotteryDraws
-- LotteryTickets (if implemented)
```

### Step 14: Verify Data Integrity

```sql
-- Check for null values in critical fields
USE AuthDb;
SELECT COUNT(*) as users FROM AspNetUsers;

USE CatalogDb;
SELECT COUNT(*) as gifts FROM Gifts;

USE OrderDb;
SELECT COUNT(*) as orders FROM Orders;

USE LotteryDb;
SELECT COUNT(*) as draws FROM LotteryDraws;
```

## ✅ Full Verification Script

Save this as `verify-database.sql` and run in SSMS:

```sql
-- Phase 12: Complete Database Verification Script
-- Run this to verify all databases are properly initialized

-- 1. Check databases exist
PRINT '=== CHECKING DATABASES ==='
SELECT 'Database: ' + name as DatabaseName, state_desc as State, user_access_desc as Access
FROM sys.databases 
WHERE name IN ('AuthDb', 'CatalogDb', 'OrderDb', 'LotteryDb')
ORDER BY name;

-- 2. Verify Auth database
PRINT ''
PRINT '=== VERIFYING AUTH DATABASE ==='
USE AuthDb;
PRINT 'Auth Schema tables:'
SELECT '  - ' + TABLE_NAME as Table_
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'Auth'
ORDER BY TABLE_NAME;

PRINT ''
PRINT 'Auth Users:'
SELECT '  Users count: ' + CAST(COUNT(*) as VARCHAR) as count_ FROM AspNetUsers;

-- 3. Verify Catalog database
PRINT ''
PRINT '=== VERIFYING CATALOG DATABASE ==='
USE CatalogDb;
PRINT 'Catalog Schema tables:'
SELECT '  - ' + TABLE_NAME as Table_
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'Catalog'
ORDER BY TABLE_NAME;

PRINT ''
PRINT 'Catalog Gifts:'
SELECT '  Gifts count: ' + CAST(COUNT(*) as VARCHAR) as count_ FROM Gifts;

-- 4. Verify Orders database
PRINT ''
PRINT '=== VERIFYING ORDERS DATABASE ==='
USE OrderDb;
PRINT 'Orders Schema tables:'
SELECT '  - ' + TABLE_NAME as Table_
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'Orders'
ORDER BY TABLE_NAME;

PRINT ''
PRINT 'Orders Data:'
SELECT '  Orders count: ' + CAST(COUNT(*) as VARCHAR) as count_ FROM Orders;

-- 5. Verify Lottery database
PRINT ''
PRINT '=== VERIFYING LOTTERY DATABASE ==='
USE LotteryDb;
PRINT 'Lottery Schema tables:'
SELECT '  - ' + TABLE_NAME as Table_
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'Lottery'
ORDER BY TABLE_NAME;

PRINT ''
PRINT 'Lottery Data:'
SELECT '  Draws count: ' + CAST(COUNT(*) as VARCHAR) as count_ FROM LotteryDraws;

PRINT ''
PRINT '=== VERIFICATION COMPLETE ==='
```

## 🔍 Advanced Verification Queries

### Check Foreign Keys

```sql
-- Check all foreign keys
USE AuthDb;
SELECT 
    CONSTRAINT_NAME,
    TABLE_NAME,
    COLUMN_NAME,
    REFERENCED_TABLE_NAME = 'N/A' -- Auth DB has identity relationships
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE CONSTRAINT_TYPE = 'FOREIGN KEY'
ORDER BY TABLE_NAME;
```

### Check Indexes

```sql
USE AuthDb;
SELECT 
    t.name as TableName,
    i.name as IndexName,
    i.type_desc as IndexType
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
ORDER BY t.name, i.name;
```

### Check Table Sizes

```sql
USE AuthDb;
EXEC sp_spaceused 'AspNetUsers';

USE CatalogDb;
EXEC sp_spaceused 'Gifts';

USE OrderDb;
EXEC sp_spaceused 'Orders';

USE LotteryDb;
EXEC sp_spaceused 'LotteryDraws';
```

## 📊 Expected Database Structure

### AuthDb.[Auth] Schema
```
AspNetUsers
  - Id (PK, GUID)
  - UserName (string, unique)
  - Email (string, unique)
  - PasswordHash (string)
  - FullName (string)
  - CreatedAt (datetime)
  - UpdatedAt (datetime)
  - ... (other AspNetIdentity fields)
```

### CatalogDb.[Catalog] Schema
```
Gifts
  - GiftId (PK, int)
  - Name (string)
  - Description (string)
  - Price (decimal)
  - Stock (int)
  - Category (string)
  - ImageUrl (string)
  - CreatedAt (datetime)
  - UpdatedAt (datetime)
```

### OrderDb.[Orders] Schema
```
Orders
  - OrderId (PK, int)
  - UserId (FK to AuthDb)
  - GiftId (FK to CatalogDb)
  - Quantity (int)
  - TotalPrice (decimal)
  - Status (string: pending/completed/cancelled)
  - CreatedAt (datetime)
  - UpdatedAt (datetime)
```

### LotteryDb.[Lottery] Schema
```
LotteryDraws
  - DrawId (PK, int)
  - Name (string)
  - DrawDate (datetime)
  - WinnerId (FK to AuthDb, nullable)
  - Prize (decimal)
  - Status (string: scheduled/completed/cancelled)
  - CreatedAt (datetime)
  - UpdatedAt (datetime)
```

## ✅ Verification Checklist

### Databases
- [ ] AuthDb exists
- [ ] CatalogDb exists
- [ ] OrderDb exists
- [ ] LotteryDb exists

### Schemas
- [ ] [Auth] schema exists in AuthDb
- [ ] [Catalog] schema exists in CatalogDb
- [ ] [Orders] schema exists in OrderDb
- [ ] [Lottery] schema exists in LotteryDb

### Tables - Auth
- [ ] AspNetUsers table exists
- [ ] AspNetRoles table exists
- [ ] AspNetUserRoles table exists
- [ ] All columns present

### Tables - Catalog
- [ ] Gifts table exists
- [ ] All columns present

### Tables - Orders
- [ ] Orders table exists
- [ ] All columns present

### Tables - Lottery
- [ ] LotteryDraws table exists
- [ ] All columns present

### Data Integrity
- [ ] Users table is accessible
- [ ] Gifts table is accessible
- [ ] Orders table is accessible
- [ ] LotteryDraws table is accessible
- [ ] No constraint violations

### Connections
- [ ] AuthService can connect
- [ ] CatalogService can connect
- [ ] OrderService can connect
- [ ] LotteryService can connect

## 🚨 Troubleshooting

### Database Won't Connect
```powershell
# Check if SQL Server is running
Get-Service MSSQL* | Select-Object Name, Status

# If not running:
Start-Service MSSQL$SQLEXPRESS
```

### Schemas Not Created
```
Cause: Migrations not run
Solution: Ensure all services started with F5
         Check console for migration errors
         If errors, see PHASE12_TROUBLESHOOTING.md
```

### Tables Missing Columns
```
Cause: Outdated code or partial migration
Solution: 
  1. Stop all services
  2. Delete databases
  3. Restart services (they'll recreate)
  4. Verify again
```

### Can't Query Tables
```powershell
# Verify permissions
SELECT * FROM sys.user_permissions;

# Try as sa user (if available)
# Or check service account permissions
```

## 📞 Next Steps

✅ Database verified?
👉 Continue to [PHASE12_API_TESTING.md](PHASE12_API_TESTING.md)

❌ Database issues?
👉 See [PHASE12_TROUBLESHOOTING.md](PHASE12_TROUBLESHOOTING.md)

---

**Phase 12 Database Verification ensures your data layer is ready!**
