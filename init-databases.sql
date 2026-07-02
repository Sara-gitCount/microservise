-- Initialize separate databases for each microservice
-- This script runs when the SQL Server container starts

-- Create AuthService database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'Mechira-AuthService')
BEGIN
    CREATE DATABASE [Mechira-AuthService];
    PRINT 'Created database: Mechira-AuthService';
END
ELSE
BEGIN
    PRINT 'Database Mechira-AuthService already exists';
END

-- Create CatalogService database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'Mechira-CatalogService')
BEGIN
    CREATE DATABASE [Mechira-CatalogService];
    PRINT 'Created database: Mechira-CatalogService';
END
ELSE
BEGIN
    PRINT 'Database Mechira-CatalogService already exists';
END

-- Create OrderService database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'Mechira-OrderService')
BEGIN
    CREATE DATABASE [Mechira-OrderService];
    PRINT 'Created database: Mechira-OrderService';
END
ELSE
BEGIN
    PRINT 'Database Mechira-OrderService already exists';
END

-- Create LotteryService database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'Mechira-LotteryService')
BEGIN
    CREATE DATABASE [Mechira-LotteryService];
    PRINT 'Created database: Mechira-LotteryService';
END
ELSE
BEGIN
    PRINT 'Database Mechira-LotteryService already exists';
END

-- Create NotificationService database (optional, if needed for event log storage)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'Mechira-NotificationService')
BEGIN
    CREATE DATABASE [Mechira-NotificationService];
    PRINT 'Created database: Mechira-NotificationService';
END
ELSE
BEGIN
    PRINT 'Database Mechira-NotificationService already exists';
END
