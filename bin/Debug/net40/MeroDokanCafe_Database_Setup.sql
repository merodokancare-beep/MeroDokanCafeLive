-- ====================================================================================
-- MERO DOKAN CAFE - FULL DATABASE SETUP SCRIPT FOR CLIENT MACHINE
-- Compatible with: Microsoft SQL Server 2008 / 2012 / 2014 / 2016 / 2019 / 2022 / Express
-- Default Database: MeroDokanCafeDB (Change to 'Cafe' if your dbconfig.txt uses 'Cafe')
-- ====================================================================================

USE master;
GO

-- 1. CREATE DATABASE IF NOT EXISTS
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'MeroDokanCafeDB')
BEGIN
    CREATE DATABASE [MeroDokanCafeDB];
    PRINT 'Database [MeroDokanCafeDB] created successfully.';
END
ELSE
BEGIN
    PRINT 'Database [MeroDokanCafeDB] already exists.';
END
GO

USE [MeroDokanCafeDB];
GO

-- ====================================================================================
-- 2. CREATE SCHEMAS & TABLES
-- ====================================================================================

-- Users Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Username NVARCHAR(50) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        Role NVARCHAR(20) NOT NULL,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
    PRINT 'Table [Users] created.';
END
GO

-- Customers Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
BEGIN
    CREATE TABLE Customers (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(100) NOT NULL,
        Phone NVARCHAR(20) NULL,
        Email NVARCHAR(100) NULL,
        Address NVARCHAR(200) NULL,
        GSTIN NVARCHAR(50) NULL,
        StateName NVARCHAR(100) NOT NULL DEFAULT 'Delhi',
        StateCode NVARCHAR(10) NOT NULL DEFAULT '07',
        CreatedAt DATETIME DEFAULT GETDATE()
    );
    PRINT 'Table [Customers] created.';
END
GO

-- Suppliers Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Suppliers')
BEGIN
    CREATE TABLE Suppliers (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(100) NOT NULL,
        ContactPerson NVARCHAR(100) NULL,
        Phone NVARCHAR(20) NULL,
        Email NVARCHAR(100) NULL,
        Address NVARCHAR(200) NULL,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
    PRINT 'Table [Suppliers] created.';
END
GO

-- Categories Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
BEGIN
    CREATE TABLE Categories (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(100) UNIQUE NOT NULL,
        Type NVARCHAR(20) NOT NULL DEFAULT 'Product',
        HsnSacCode NVARCHAR(50) NULL DEFAULT '996331',
        GSTRate DECIMAL(5,2) NOT NULL DEFAULT 5.00
    );
    PRINT 'Table [Categories] created.';
END
GO

-- Products Table (Cafe Dishes & Menu Items)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
BEGIN
    CREATE TABLE Products (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Code NVARCHAR(50) NOT NULL UNIQUE,
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        Category NVARCHAR(100) NULL,
        PurchasePrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        SalesPrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        Stock INT NOT NULL DEFAULT 0,
        MinStockLevel INT NOT NULL DEFAULT 5,
        HSNCode NVARCHAR(50) NOT NULL DEFAULT '2106',
        GSTRate DECIMAL(5,2) NOT NULL DEFAULT 5.00,
        Barcode NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
    PRINT 'Table [Products] created.';
END
GO

-- Services Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Services')
BEGIN
    CREATE TABLE Services (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Code NVARCHAR(50) NOT NULL UNIQUE,
        Name NVARCHAR(150) NOT NULL,
        Category NVARCHAR(100) NULL,
        Price DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        DurationMinutes INT NOT NULL DEFAULT 30,
        Description NVARCHAR(500) NULL,
        SACCode NVARCHAR(50) NOT NULL DEFAULT '996331',
        GSTRate DECIMAL(18,2) NOT NULL DEFAULT 5.00,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
    PRINT 'Table [Services] created.';
END
GO

-- StylistRoles Table (Cafe Roles & Designations)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StylistRoles')
BEGIN
    CREATE TABLE StylistRoles (
        Id INT PRIMARY KEY IDENTITY(1,1),
        RoleName NVARCHAR(100) NOT NULL UNIQUE,
        Description NVARCHAR(500) NULL,
        DefaultCommissionRate DECIMAL(5,2) NOT NULL DEFAULT 0.00,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
    PRINT 'Table [StylistRoles] created.';
END
GO

-- Staff Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Staff')
BEGIN
    CREATE TABLE Staff (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(100) NOT NULL,
        Phone NVARCHAR(50) NULL,
        Email NVARCHAR(100) NULL,
        Role NVARCHAR(50) NOT NULL DEFAULT 'Steward',
        CommissionRate DECIMAL(5,2) NOT NULL DEFAULT 0.00,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
    PRINT 'Table [Staff] created.';
END
GO

-- Appointments Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Appointments')
BEGIN
    CREATE TABLE Appointments (
        Id INT PRIMARY KEY IDENTITY(1,1),
        AppointmentNumber NVARCHAR(50) NOT NULL UNIQUE,
        CustomerId INT NULL FOREIGN KEY REFERENCES Customers(Id) ON DELETE SET NULL,
        StaffId INT NULL FOREIGN KEY REFERENCES Staff(Id) ON DELETE SET NULL,
        ServiceId INT NULL FOREIGN KEY REFERENCES Services(Id) ON DELETE SET NULL,
        ServiceIds NVARCHAR(500) NULL,
        ServiceNames NVARCHAR(1000) NULL,
        AppointmentDate DATE NOT NULL,
        AppointmentTime NVARCHAR(100) NOT NULL,
        Status NVARCHAR(30) NOT NULL DEFAULT 'Booked',
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
    PRINT 'Table [Appointments] created.';
END
GO

-- Purchases Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Purchases')
BEGIN
    CREATE TABLE Purchases (
        Id INT PRIMARY KEY IDENTITY(1,1),
        PurchaseNumber NVARCHAR(50) NOT NULL UNIQUE,
        SupplierId INT NULL FOREIGN KEY REFERENCES Suppliers(Id) ON DELETE SET NULL,
        PurchaseDate DATETIME NOT NULL DEFAULT GETDATE(),
        TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
    );
    PRINT 'Table [Purchases] created.';
END
GO

-- PurchaseDetails Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseDetails')
BEGIN
    CREATE TABLE PurchaseDetails (
        Id INT PRIMARY KEY IDENTITY(1,1),
        PurchaseId INT FOREIGN KEY REFERENCES Purchases(Id) ON DELETE CASCADE,
        ProductId INT FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
        Quantity INT NOT NULL,
        PurchasePrice DECIMAL(18,2) NOT NULL
    );
    PRINT 'Table [PurchaseDetails] created.';
END
GO

-- Sales Table (Billing Orders)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sales')
BEGIN
    CREATE TABLE Sales (
        Id INT PRIMARY KEY IDENTITY(1,1),
        InvoiceNumber NVARCHAR(50) NOT NULL UNIQUE,
        CustomerId INT NULL FOREIGN KEY REFERENCES Customers(Id) ON DELETE SET NULL,
        SaleDate DATETIME NOT NULL DEFAULT GETDATE(),
        SubTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        Discount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        Tax DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        GrandTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        AmountPaid DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        DueAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'Cash',
        CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
        IsGSTBill BIT NOT NULL DEFAULT 1,
        TaxableAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        CGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        SGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        IGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        CustomerGSTIN NVARCHAR(50) NULL,
        PlaceOfSupply NVARCHAR(100) NULL,
        IsInterState BIT NOT NULL DEFAULT 0,
        CashAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        OnlineAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        OrderType NVARCHAR(50) NOT NULL DEFAULT 'DINING',
        TableNumber NVARCHAR(50) NULL,
        KotNumbers NVARCHAR(200) NULL,
        PackingCharges DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        StewardName NVARCHAR(100) NULL,
        RoundOff DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        AppointmentId INT NULL
    );
    PRINT 'Table [Sales] created.';
END
GO

-- SaleDetails Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SaleDetails')
BEGIN
    CREATE TABLE SaleDetails (
        Id INT PRIMARY KEY IDENTITY(1,1),
        SaleId INT FOREIGN KEY REFERENCES Sales(Id) ON DELETE CASCADE,
        ItemType NVARCHAR(20) NOT NULL DEFAULT 'Product',
        ProductId INT NULL FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
        ServiceId INT NULL FOREIGN KEY REFERENCES Services(Id) ON DELETE SET NULL,
        StaffId INT NULL FOREIGN KEY REFERENCES Staff(Id) ON DELETE SET NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        Total DECIMAL(18,2) NOT NULL,
        PurchaseCostAtSale DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        HSNSAC NVARCHAR(50) NULL,
        GSTRate DECIMAL(18,2) NOT NULL DEFAULT 5.00,
        TaxableAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        CGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        SGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        IGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00
    );
    PRINT 'Table [SaleDetails] created.';
END
GO

-- ProductPriceHistory Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductPriceHistory')
BEGIN
    CREATE TABLE ProductPriceHistory (
        Id INT PRIMARY KEY IDENTITY(1,1),
        ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
        OldPurchasePrice DECIMAL(18,2) NOT NULL,
        NewPurchasePrice DECIMAL(18,2) NOT NULL,
        OldSalesPrice DECIMAL(18,2) NOT NULL,
        NewSalesPrice DECIMAL(18,2) NOT NULL,
        ChangeDate DATETIME NOT NULL DEFAULT GETDATE(),
        ChangedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
        Source NVARCHAR(100) NOT NULL
    );
    PRINT 'Table [ProductPriceHistory] created.';
END
GO

-- SalesReturns Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SalesReturns')
BEGIN
    CREATE TABLE SalesReturns (
        Id INT PRIMARY KEY IDENTITY(1,1),
        ReturnNumber NVARCHAR(50) UNIQUE NOT NULL,
        SaleId INT NOT NULL FOREIGN KEY REFERENCES Sales(Id) ON DELETE CASCADE,
        ReturnDate DATETIME NOT NULL DEFAULT GETDATE(),
        TotalRefund DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        CashRefund DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
    );
    PRINT 'Table [SalesReturns] created.';
END
GO

-- SalesReturnDetails Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SalesReturnDetails')
BEGIN
    CREATE TABLE SalesReturnDetails (
        Id INT PRIMARY KEY IDENTITY(1,1),
        ReturnId INT NOT NULL FOREIGN KEY REFERENCES SalesReturns(Id) ON DELETE CASCADE,
        ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(Id),
        Quantity INT NOT NULL,
        RefundPrice DECIMAL(18,2) NOT NULL,
        Total DECIMAL(18,2) NOT NULL,
        ItemCondition NVARCHAR(50) NOT NULL
    );
    PRINT 'Table [SalesReturnDetails] created.';
END
GO

-- CustomerPayments Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomerPayments')
BEGIN
    CREATE TABLE CustomerPayments (
        Id INT PRIMARY KEY IDENTITY(1,1),
        CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(Id) ON DELETE CASCADE,
        PaymentDate DATETIME NOT NULL DEFAULT GETDATE(),
        Amount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'Cash',
        Remarks NVARCHAR(200) NULL,
        CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
        SaleId INT NULL FOREIGN KEY REFERENCES Sales(Id) ON DELETE SET NULL
    );
    PRINT 'Table [CustomerPayments] created.';
END
GO

-- DailySettlements Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DailySettlements')
BEGIN
    CREATE TABLE DailySettlements (
        Id INT PRIMARY KEY IDENTITY(1,1),
        SettlementDate DATETIME NOT NULL DEFAULT GETDATE(),
        OpeningCash DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        CashSales DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        DueCollections DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        CardQRSales DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        CardSales DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        QRSales DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        DuesCreated DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        ExpectedCash DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        ActualCash DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        Variance DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        SettlementBy INT NULL FOREIGN KEY REFERENCES Users(Id),
        Remarks NVARCHAR(500) NULL,
        Refunds DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        VoidAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00
    );
    PRINT 'Table [DailySettlements] created.';
END
GO

-- AppProfile Configuration Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppProfile')
BEGIN
    CREATE TABLE AppProfile (
        Id INT PRIMARY KEY IDENTITY(1,1),
        OwnerName NVARCHAR(100) NOT NULL DEFAULT 'Cafe Manager',
        ShopName NVARCHAR(150) NOT NULL DEFAULT 'The Local Cafe',
        Phone NVARCHAR(50) NOT NULL DEFAULT '9971592652',
        Email NVARCHAR(100) NOT NULL DEFAULT 'contact@thelocalcafe.com',
        Address NVARCHAR(200) NOT NULL DEFAULT 'vajra world Mall Balwa khani, Gangtok Sikkim 737101',
        LogoPath NVARCHAR(500) NULL,
        ProfilePicPath NVARCHAR(500) NULL,
        ThemePreset NVARCHAR(50) NOT NULL DEFAULT 'Emerald Mint',
        FontSizePreset NVARCHAR(50) NOT NULL DEFAULT 'Medium',
        BackupFolderPath NVARCHAR(500) NOT NULL DEFAULT 'D:\MeroDokanCafe\DailyDatabaseBackup',
        GoogleDriveAddress NVARCHAR(500) NOT NULL DEFAULT 'https://script.google.com/macros/s/AKfycbwm3WKMbeToLZt10WTPGrHwL4XsA8JgVO_H4MAaraDpssgTfUNs1x_ECblU4cKkRMAx/exec',
        GSTIN NVARCHAR(50) NULL,
        StateName NVARCHAR(100) NOT NULL DEFAULT 'Delhi',
        StateCode NVARCHAR(10) NOT NULL DEFAULT '07',
        IsTaxInclusive BIT NOT NULL DEFAULT 1,
        DefaultBillType NVARCHAR(50) NOT NULL DEFAULT 'GST',
        DefaultGSTRate DECIMAL(18,2) NOT NULL DEFAULT 5.00,
        ReceiptFooterText NVARCHAR(500) NOT NULL DEFAULT 'Tashi Delek! Thukje Che!',
        DefaultPackingCharge DECIMAL(18,2) NOT NULL DEFAULT 40.00,
        KitchenPrinterName NVARCHAR(200) NULL,
        BillingPrinterName NVARCHAR(200) NULL,
        UPIId NVARCHAR(100) NULL,
        UPIName NVARCHAR(100) NULL,
        AutoShowQROnUPI BIT NOT NULL DEFAULT 1,
        PrintQROnReceipt BIT NOT NULL DEFAULT 1
    );
    PRINT 'Table [AppProfile] created.';
END
GO

-- HsnSacMaster Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HsnSacMaster')
BEGIN
    CREATE TABLE HsnSacMaster (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Code NVARCHAR(50) NOT NULL UNIQUE,
        Type NVARCHAR(20) NOT NULL DEFAULT 'HSN',
        Description NVARCHAR(500) NOT NULL,
        GSTRate DECIMAL(5,2) NOT NULL DEFAULT 18.00,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
    PRINT 'Table [HsnSacMaster] created.';
END
GO

-- RawMaterials Table (Inventory & Ingredients)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RawMaterials')
BEGIN
    CREATE TABLE RawMaterials (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Code NVARCHAR(50) NOT NULL UNIQUE,
        Name NVARCHAR(150) NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        Unit NVARCHAR(30) NOT NULL DEFAULT 'Kg',
        CurrentStock DECIMAL(18,3) NOT NULL DEFAULT 0.000,
        MinStockLevel DECIMAL(18,3) NOT NULL DEFAULT 5.000,
        UnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
    PRINT 'Table [RawMaterials] created.';
END
GO

-- StockMovements Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StockMovements')
BEGIN
    CREATE TABLE StockMovements (
        Id INT PRIMARY KEY IDENTITY(1,1),
        MaterialId INT NOT NULL FOREIGN KEY REFERENCES RawMaterials(Id) ON DELETE CASCADE,
        TransactionType NVARCHAR(30) NOT NULL,
        Quantity DECIMAL(18,3) NOT NULL,
        UnitCost DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        TotalCost DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        Department NVARCHAR(50) NULL DEFAULT 'Kitchen',
        SupplierId INT NULL FOREIGN KEY REFERENCES Suppliers(Id) ON DELETE SET NULL,
        ReferenceNo NVARCHAR(100) NULL,
        Remarks NVARCHAR(500) NULL,
        TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy NVARCHAR(100) NULL
    );
    PRINT 'Table [StockMovements] created.';
END
GO

-- CafeTables Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CafeTables')
BEGIN
    CREATE TABLE CafeTables (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TableNumber NVARCHAR(50) NOT NULL UNIQUE,
        TableName NVARCHAR(100) NOT NULL,
        Section NVARCHAR(50) NOT NULL DEFAULT 'Main Dining',
        Capacity INT NOT NULL DEFAULT 4,
        Status NVARCHAR(30) NOT NULL DEFAULT 'Available',
        CurrentBillAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        OrderStartTime DATETIME NULL,
        BilledTime DATETIME NULL,
        ActiveKotNumbers NVARCHAR(200) NULL,
        ActiveSaleId INT NULL,
        CurrentSteward NVARCHAR(100) NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
    PRINT 'Table [CafeTables] created.';
END
GO

-- KOTMaster Table (Kitchen Order Tickets)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KOTMaster')
BEGIN
    CREATE TABLE KOTMaster (
        Id INT PRIMARY KEY IDENTITY(1,1),
        KOTNumber INT NOT NULL,
        TableNumber NVARCHAR(50) NOT NULL,
        OrderType NVARCHAR(50) NOT NULL DEFAULT 'DINING',
        Steward NVARCHAR(100) NULL,
        Status NVARCHAR(30) NOT NULL DEFAULT 'Active',
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        KotComment NVARCHAR(500) NULL,
        IsVoided BIT NOT NULL DEFAULT 0,
        VoidReason NVARCHAR(500) NULL,
        VoidedAt DATETIME NULL,
        SaleId INT NULL
    );
    PRINT 'Table [KOTMaster] created.';
END
GO

-- KOTDetails Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KOTDetails')
BEGIN
    CREATE TABLE KOTDetails (
        Id INT PRIMARY KEY IDENTITY(1,1),
        KOTId INT NOT NULL FOREIGN KEY REFERENCES KOTMaster(Id) ON DELETE CASCADE,
        ProductId INT NULL,
        ItemName NVARCHAR(150) NOT NULL,
        Quantity INT NOT NULL DEFAULT 1,
        Rate DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        Amount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        Instructions NVARCHAR(200) NULL,
        IsVoided BIT NOT NULL DEFAULT 0,
        VoidReason NVARCHAR(300) NULL,
        VoidedAt DATETIME NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Table [KOTDetails] created.';
END
GO

-- ====================================================================================
-- 3. SEED DEFAULT DATA
-- ====================================================================================

-- 3.1 Default Administrator Account (Username: admin | Password: admin)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, PasswordHash, FullName, Role, CreatedAt)
    VALUES ('admin', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 'System Administrator', 'Admin', GETDATE());
    PRINT 'Default Admin user seeded (admin / admin).';
END
GO

-- 3.2 Default Customers
IF NOT EXISTS (SELECT 1 FROM Customers WHERE Name = 'Walk-in Guest')
BEGIN
    INSERT INTO Customers (Name, Phone, Email, Address) VALUES 
    ('Walk-in Guest', '0000000000', 'guest@thelocalcafe.com', 'Local'),
    ('Tenzing Norbu', '9971511223', 'tenzing@gmail.com', 'Gangtok'),
    ('Doma Bhutia', '9971522334', 'doma@yahoo.com', 'Balwakhani');
    PRINT 'Default Customers seeded.';
END
GO

-- 3.3 Default Suppliers
IF NOT EXISTS (SELECT 1 FROM Suppliers WHERE Name = 'Himalayan Fresh Dairy')
BEGIN
    INSERT INTO Suppliers (Name, ContactPerson, Phone, Email, Address) VALUES 
    ('Himalayan Fresh Dairy', 'Tashi Wangyal', '9971501122', 'dairy@himalayanfresh.com', 'Gangtok, Sikkim'),
    ('Sikkim Organic Grocery Suppliers', 'Pemba Sherpa', '9971502233', 'orders@sikkimorganics.com', 'Tadong, Gangtok'),
    ('Sunrise Bakery & Cafe Packaging', 'Karma Lepcha', '9971503344', 'packaging@sunrisebakery.com', 'Deorali, Gangtok');
    PRINT 'Default Suppliers seeded.';
END
GO

-- 3.4 Cafe Categories
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Name = 'Coffee')
BEGIN
    INSERT INTO Categories (Name, Type, HsnSacCode, GSTRate) VALUES 
    ('Coffee', 'Product', '0901', 5.00),
    ('Shakes', 'Product', '2202', 5.00),
    ('Black Hot', 'Product', '0902', 5.00),
    ('Milk Hot', 'Product', '0902', 5.00),
    ('Refreshers', 'Product', '2202', 5.00),
    ('Pizza', 'Product', '1905', 5.00),
    ('Sandwich', 'Product', '1905', 5.00),
    ('Burger', 'Product', '1905', 5.00),
    ('Korean', 'Product', '2106', 5.00),
    ('Breakfast', 'Product', '1905', 5.00),
    ('Small Bites', 'Product', '2106', 5.00),
    ('Soups', 'Product', '2104', 5.00),
    ('Wraps', 'Product', '1905', 5.00),
    ('Pasta & Noodles', 'Product', '1902', 5.00),
    ('Laphing', 'Product', '2104', 5.00),
    ('Salads', 'Product', '2106', 5.00),
    ('AddOn', 'Product', '2106', 5.00);
    PRINT 'Default Categories seeded.';
END
GO

-- 3.5 Cafe Staff Roles
IF NOT EXISTS (SELECT 1 FROM StylistRoles WHERE RoleName = 'Steward')
BEGIN
    INSERT INTO StylistRoles (RoleName, Description, DefaultCommissionRate, IsActive) VALUES
    ('Head Chef', 'Executive chef overseeing kitchen preparation and quality', 0.00, 1),
    ('Sous Chef / Cook', 'Cooking, food preparation, continental & oriental dishes', 0.00, 1),
    ('Senior Steward / Captain', 'Table management, guest greeting and order oversight', 0.00, 1),
    ('Steward', 'Order taking, table service, KOT serving and customer care', 0.00, 1),
    ('Barista & Beverage Master', 'Coffee brewing, shakes, mocktails and iced teas', 0.00, 1),
    ('Pastry & Bakery Chef', 'Tibetan breads, bakery items and desserts', 0.00, 1),
    ('Cashier & Front Desk', 'Billing counter settlement and customer reception', 0.00, 1),
    ('Kitchen Helper / Busser', 'Kitchen assistance and table clearance', 0.00, 1);
    PRINT 'Default Cafe Roles seeded.';
END
GO

-- 3.6 Default Cafe Stewards / Staff
IF NOT EXISTS (SELECT 1 FROM Staff WHERE Name = 'Tashi')
BEGIN
    INSERT INTO Staff (Name, Phone, Email, Role, CommissionRate, IsActive) VALUES 
    ('Tashi', '9971500001', 'tashi@thelocalcafe.com', 'Steward', 0.00, 1),
    ('Pemba', '9971500002', 'pemba@thelocalcafe.com', 'Steward', 0.00, 1),
    ('Karma', '9971500003', 'karma@thelocalcafe.com', 'Captain', 0.00, 1),
    ('Dawa', '9971500004', 'dawa@thelocalcafe.com', 'Steward', 0.00, 1),
    ('Passang', '9971500005', 'passang@thelocalcafe.com', 'Chef', 0.00, 1),
    ('Choden', '9971500006', 'choden@thelocalcafe.com', 'Barista', 0.00, 1);
    PRINT 'Default Staff seeded.';
END
GO

-- 3.7 Cafe Tables & Tokens
IF NOT EXISTS (SELECT 1 FROM CafeTables WHERE TableNumber = '1')
BEGIN
    INSERT INTO CafeTables (TableNumber, TableName, Section, Capacity, Status) VALUES
    ('1', 'Table 1', 'Main Dining', 2, 'Available'),
    ('2', 'Table 2', 'Main Dining', 4, 'Available'),
    ('3', 'Table 3', 'Main Dining', 4, 'Available'),
    ('4', 'Table 4', 'Main Dining', 4, 'Available'),
    ('5', 'Table 5', 'Main Dining', 6, 'Available'),
    ('6', 'Table 6', 'Main Dining', 2, 'Available'),
    ('7', 'Table 7', 'Main Dining', 4, 'Available'),
    ('8', 'Table 8', 'Main Dining', 4, 'Available'),
    ('9', 'Table 9', 'Main Dining', 6, 'Available'),
    ('10', 'Table 10', 'Main Dining', 8, 'Available'),
    ('Waiting 1', 'Waiting Token 1', 'Takeaway & Waiting', 1, 'Available'),
    ('Waiting 2', 'Waiting Token 2', 'Takeaway & Waiting', 1, 'Available'),
    ('Waiting 3', 'Waiting Token 3', 'Takeaway & Waiting', 1, 'Available'),
    ('Waiting 4', 'Waiting Token 4', 'Takeaway & Waiting', 1, 'Available'),
    ('Waiting 5', 'Waiting Token 5', 'Takeaway & Waiting', 1, 'Available');
    PRINT 'Default Cafe Tables seeded.';
END
GO

-- 3.8 Cafe Menu Dishes & Products
IF NOT EXISTS (SELECT 1 FROM Products WHERE Code = 'COF-001')
BEGIN
    INSERT INTO Products (Code, Name, Description, Category, PurchasePrice, SalesPrice, Stock, MinStockLevel, HSNCode, GSTRate) VALUES 
    ('DRK-001', 'Detox Tea', 'Detox Tea', 'Black Hot', 0.00, 120.00, 100, 5, '0902', 5.00),
    ('DRK-002', 'Ginger Honey Lemon Tea', 'Ginger Honey Lemon Tea', 'Black Hot', 0.00, 130.00, 100, 5, '0902', 5.00),
    ('DRK-003', 'Tibetan Tea', 'Tibetan Tea', 'Milk Hot', 0.00, 120.00, 100, 5, '0902', 5.00),
    ('DRK-004', 'Hot Chocolate', 'Hot Chocolate', 'Milk Hot', 0.00, 200.00, 100, 5, '1806', 5.00),
    ('REF-001', 'Lemon Iced Tea', 'Lemon Iced Tea', 'Refreshers', 0.00, 220.00, 100, 5, '2202', 5.00),
    ('REF-002', 'Peach Iced Tea', 'Peach Iced Tea', 'Refreshers', 0.00, 220.00, 100, 5, '2202', 5.00),
    ('REF-003', 'Virgin Mojito', 'Virgin Mojito', 'Refreshers', 0.00, 220.00, 100, 5, '2202', 5.00),
    ('REF-004', 'Mint Mojito', 'Mint Mojito', 'Refreshers', 0.00, 220.00, 100, 5, '2202', 5.00),
    ('REF-005', 'Watermelon Mojito', 'Watermelon Mojito', 'Refreshers', 0.00, 220.00, 100, 5, '2202', 5.00),
    ('REF-006', 'Lemon Soda', 'Lemon Soda', 'Refreshers', 0.00, 120.00, 100, 5, '2202', 5.00),
    ('PIZ-001', 'Veg Pizza', 'Veg Pizza', 'Pizza', 0.00, 450.00, 100, 5, '1905', 5.00),
    ('PIZ-002', 'Margarita Pizza', 'Margarita Pizza', 'Pizza', 0.00, 460.00, 100, 5, '1905', 5.00),
    ('PIZ-003', 'Pizza Fungi', 'Pizza Fungi', 'Pizza', 0.00, 490.00, 100, 5, '1905', 5.00),
    ('PIZ-004', 'Grilled Chicken Pizza', 'Grilled Chicken Pizza', 'Pizza', 0.00, 520.00, 100, 5, '1905', 5.00),
    ('PIZ-005', 'Tuna Pizza', 'Tuna Pizza', 'Pizza', 0.00, 500.00, 100, 5, '1905', 5.00),
    ('PIZ-006', 'Peri Peri Grilled Chicken Pizza', 'Peri Peri Grilled Chicken Pizza', 'Pizza', 0.00, 530.00, 100, 5, '1905', 5.00),
    ('PIZ-007', 'Peri Peri Chicken Sausage Pizza', 'Peri Peri Chicken Sausage Pizza', 'Pizza', 0.00, 550.00, 100, 5, '1905', 5.00),
    ('PIZ-008', 'Bacon Tomato Pizza', 'Bacon Tomato Pizza', 'Pizza', 0.00, 580.00, 100, 5, '1905', 5.00),
    ('PIZ-009', 'Pepperoni Pizza', 'Pepperoni Pizza', 'Pizza', 0.00, 580.00, 100, 5, '1905', 5.00),
    ('PIZ-010', 'Gorkha Spicy Pizza (Veg)', 'Gorkha Spicy Pizza (Veg)', 'Pizza', 0.00, 490.00, 100, 5, '1905', 5.00),
    ('PIZ-011', 'Gorkha Spicy Pizza (Chicken)', 'Gorkha Spicy Pizza (Chicken)', 'Pizza', 0.00, 550.00, 100, 5, '1905', 5.00),
    ('SND-001', 'Classic Grilled Cheese (Veg)', 'Classic Grilled Cheese (Veg)', 'Sandwich', 0.00, 250.00, 100, 5, '1905', 5.00),
    ('SND-002', 'Classic Grilled Cheese (Non Veg)', 'Classic Grilled Cheese (Non Veg)', 'Sandwich', 0.00, 280.00, 100, 5, '1905', 5.00),
    ('SND-003', 'Club Sandwich (Veg)', 'Club Sandwich (Veg)', 'Sandwich', 0.00, 330.00, 100, 5, '1905', 5.00),
    ('SND-004', 'Club Sandwich (Non Veg)', 'Club Sandwich (Non Veg)', 'Sandwich', 0.00, 380.00, 100, 5, '1905', 5.00),
    ('SND-005', 'Bacon Sandwich', 'Bacon Sandwich', 'Sandwich', 0.00, 380.00, 100, 5, '1905', 5.00),
    ('SND-006', 'Tuna Sandwich', 'Tuna Sandwich', 'Sandwich', 0.00, 370.00, 100, 5, '1905', 5.00),
    ('SND-007', 'Tibetan Bread Sandwich (Veg)', 'Tibetan Bread Sandwich (Veg)', 'Sandwich', 0.00, 290.00, 100, 5, '1905', 5.00),
    ('SND-008', 'Tibetan Bread Sandwich (Non Veg)', 'Tibetan Bread Sandwich (Non Veg)', 'Sandwich', 0.00, 320.00, 100, 5, '1905', 5.00),
    ('BGR-001', 'TLC Special Burger (Non Veg)', 'TLC Special Burger (Non Veg)', 'Burger', 0.00, 370.00, 100, 5, '1905', 5.00),
    ('KOR-001', 'Kimbap (Veg)', 'Kimbap (Veg)', 'Korean', 0.00, 350.00, 100, 5, '2106', 5.00),
    ('KOR-002', 'Kimbap (Chicken)', 'Kimbap (Chicken)', 'Korean', 0.00, 380.00, 100, 5, '2106', 5.00),
    ('KOR-003', 'Tuna Kimbap', 'Tuna Kimbap', 'Korean', 0.00, 400.00, 100, 5, '2106', 5.00),
    ('KOR-004', 'Bacon Kimbap', 'Bacon Kimbap', 'Korean', 0.00, 450.00, 100, 5, '2106', 5.00),
    ('KOR-005', 'Nude Kimbap (Veg)', 'Nude Kimbap (Veg)', 'Korean', 0.00, 300.00, 100, 5, '2106', 5.00),
    ('KOR-006', 'Nude Kimbap (Chicken)', 'Nude Kimbap (Chicken)', 'Korean', 0.00, 350.00, 100, 5, '2106', 5.00),
    ('KOR-007', 'Nude Kimbap (Bacon)', 'Nude Kimbap (Bacon)', 'Korean', 0.00, 400.00, 100, 5, '2106', 5.00),
    ('KOR-008', 'Ramen (Veg)', 'Ramen (Veg)', 'Korean', 0.00, 300.00, 100, 5, '2106', 5.00),
    ('KOR-009', 'Ramen (Chicken)', 'Ramen (Chicken)', 'Korean', 0.00, 350.00, 100, 5, '2106', 5.00),
    ('KOR-010', 'Bacon Ramen', 'Bacon Ramen', 'Korean', 0.00, 400.00, 100, 5, '2106', 5.00),
    ('KOR-011', 'Tteokbokki (Veg)', 'Tteokbokki (Veg)', 'Korean', 0.00, 350.00, 100, 5, '2106', 5.00),
    ('KOR-012', 'Tteokbokki (Chicken)', 'Tteokbokki (Chicken)', 'Korean', 0.00, 420.00, 100, 5, '2106', 5.00),
    ('KOR-013', 'Tteokbokki (Pork)', 'Tteokbokki (Pork)', 'Korean', 0.00, 450.00, 100, 5, '2106', 5.00),
    ('KOR-014', 'Dakgangjeong (Chicken Wings)', 'Dakgangjeong (Chicken Wings)', 'Korean', 0.00, 400.00, 100, 5, '2106', 5.00),
    ('KOR-015', 'Korean Corn Dog (Cheese)', 'Korean Corn Dog (Cheese)', 'Korean', 0.00, 300.00, 100, 5, '2106', 5.00),
    ('KOR-016', 'Korean Corn Dog (Chicken Sausage)', 'Korean Corn Dog (Chicken Sausage)', 'Korean', 0.00, 350.00, 100, 5, '2106', 5.00),
    ('KOR-017', 'Jjamppong (Veg)', 'Jjamppong (Veg)', 'Korean', 0.00, 320.00, 100, 5, '2106', 5.00),
    ('KOR-018', 'Jjamppong (Non Veg)', 'Jjamppong (Non Veg)', 'Korean', 0.00, 360.00, 100, 5, '2106', 5.00),
    ('KOR-019', 'Jangi Guksu (Veg)', 'Jangi Guksu (Veg)', 'Korean', 0.00, 320.00, 100, 5, '2106', 5.00),
    ('KOR-020', 'Jangi Guksu (Non Veg)', 'Jangi Guksu (Non Veg)', 'Korean', 0.00, 360.00, 100, 5, '2106', 5.00),
    ('KOR-021', 'Japchae (Veg)', 'Japchae (Veg)', 'Korean', 0.00, 350.00, 100, 5, '2106', 5.00),
    ('KOR-022', 'Japchae (Chicken)', 'Japchae (Chicken)', 'Korean', 0.00, 400.00, 100, 5, '2106', 5.00),
    ('KOR-023', 'Japchae (Pork)', 'Japchae (Pork)', 'Korean', 0.00, 450.00, 100, 5, '2106', 5.00),
    ('KOR-024', 'Korean Chicken Wings', 'Korean Chicken Wings', 'Korean', 0.00, 400.00, 100, 5, '2106', 5.00),
    ('KOR-025', 'Korean Hot and Crispy Chicken', 'Korean Hot and Crispy Chicken', 'Korean', 0.00, 400.00, 100, 5, '2106', 5.00),
    ('COF-001', 'Espresso (Hot)', 'Espresso (Hot)', 'Coffee', 0.00, 130.00, 100, 5, '0901', 5.00),
    ('COF-002', 'Americano (Hot)', 'Americano (Hot)', 'Coffee', 0.00, 130.00, 100, 5, '0901', 5.00),
    ('COF-003', 'Yak Butter Americano', 'Yak Butter Americano', 'Coffee', 0.00, 160.00, 100, 5, '0901', 5.00),
    ('COF-004', 'Iced Espresso', 'Iced Espresso', 'Coffee', 0.00, 150.00, 100, 5, '0901', 5.00),
    ('COF-005', 'Iced Americano', 'Iced Americano', 'Coffee', 0.00, 150.00, 100, 5, '0901', 5.00),
    ('COF-006', 'Affogato', 'Affogato', 'Coffee', 0.00, 250.00, 100, 5, '0901', 5.00),
    ('COF-007', 'Cappuccino', 'Cappuccino', 'Coffee', 0.00, 160.00, 100, 5, '0901', 5.00),
    ('COF-008', 'Yak Butter Cappuccino', 'Yak Butter Cappuccino', 'Coffee', 0.00, 200.00, 100, 5, '0901', 5.00),
    ('COF-009', 'Flat White', 'Flat White', 'Coffee', 0.00, 160.00, 100, 5, '0901', 5.00),
    ('COF-010', 'Cafe Latte', 'Cafe Latte', 'Coffee', 0.00, 160.00, 100, 5, '0901', 5.00),
    ('COF-011', 'Mocha', 'Mocha', 'Coffee', 0.00, 180.00, 100, 5, '0901', 5.00),
    ('COF-012', 'Macchiato', 'Macchiato', 'Coffee', 0.00, 130.00, 100, 5, '0901', 5.00),
    ('COF-013', 'Iced Latte', 'Iced Latte', 'Coffee', 0.00, 160.00, 100, 5, '0901', 5.00),
    ('COF-014', 'Iced Cappuccino', 'Iced Cappuccino', 'Coffee', 0.00, 160.00, 100, 5, '0901', 5.00),
    ('COF-015', 'Iced Mocha', 'Iced Mocha', 'Coffee', 0.00, 180.00, 100, 5, '0901', 5.00),
    ('COF-016', 'Cold Coffee', 'Cold Coffee', 'Coffee', 0.00, 180.00, 100, 5, '0901', 5.00),
    ('COF-017', 'Vietnamese Iced Coffee', 'Vietnamese Iced Coffee', 'Coffee', 0.00, 230.00, 100, 5, '0901', 5.00),
    ('COF-018', 'Orange Americano', 'Orange Americano', 'Coffee', 0.00, 240.00, 100, 5, '0901', 5.00),
    ('COF-019', 'Passion Fruit Americano', 'Passion Fruit Americano', 'Coffee', 0.00, 240.00, 100, 5, '0901', 5.00),
    ('COF-020', 'Coconut Espresso', 'Coconut Espresso', 'Coffee', 0.00, 240.00, 100, 5, '0901', 5.00),
    ('COF-021', 'French Press', 'French Press', 'Coffee', 0.00, 160.00, 100, 5, '0901', 5.00),
    ('COF-022', 'Himaliano Drip', 'Himaliano Drip', 'Coffee', 0.00, 160.00, 100, 5, '0901', 5.00),
    ('COF-023', 'Chemex', 'Chemex', 'Coffee', 0.00, 180.00, 100, 5, '0901', 5.00),
    ('COF-024', 'Cold Brew Black', 'Cold Brew Black', 'Coffee', 0.00, 230.00, 100, 5, '0901', 5.00),
    ('SHK-001', 'Vanilla Shake', 'Vanilla Shake', 'Shakes', 0.00, 240.00, 100, 5, '2202', 5.00),
    ('SHK-002', 'Banana Shake', 'Banana Shake', 'Shakes', 0.00, 240.00, 100, 5, '2202', 5.00),
    ('SHK-003', 'Oreo Shake', 'Oreo Shake', 'Shakes', 0.00, 240.00, 100, 5, '2202', 5.00),
    ('SHK-004', 'Chocolate Shake', 'Chocolate Shake', 'Shakes', 0.00, 240.00, 100, 5, '2202', 5.00),
    ('ADD-001', 'Vanilla Syrup Topping', 'Vanilla Syrup Topping', 'AddOn', 0.00, 40.00, 100, 5, '2106', 5.00),
    ('ADD-002', 'Hazelnut Syrup Topping', 'Hazelnut Syrup Topping', 'AddOn', 0.00, 40.00, 100, 5, '2106', 5.00),
    ('ADD-003', 'Caramel Syrup Topping', 'Caramel Syrup Topping', 'AddOn', 0.00, 40.00, 100, 5, '2106', 5.00),
    ('BRK-001', 'Himalayan Breakfast', 'Himalayan Breakfast', 'Breakfast', 0.00, 400.00, 100, 5, '1905', 5.00),
    ('BRK-002', 'English Breakfast', 'English Breakfast', 'Breakfast', 0.00, 450.00, 100, 5, '1905', 5.00),
    ('ADD-004', 'Extra Chicken Sausage / Bacon', 'Extra Chicken Sausage / Bacon', 'AddOn', 0.00, 60.00, 100, 5, '2106', 5.00),
    ('ADD-005', 'Extra Pork Sausage / Bacon', 'Extra Pork Sausage / Bacon', 'AddOn', 0.00, 90.00, 100, 5, '2106', 5.00),
    ('SML-005', 'Peri Peri Fries', 'Peri Peri Fries', 'Small Bites', 0.00, 210.00, 100, 5, '2106', 5.00),
    ('SML-006', 'French Fries', 'French Fries', 'Small Bites', 0.00, 180.00, 100, 5, '2106', 5.00),
    ('SML-007', 'Chilli Cheese Garlic Fries', 'Chilli Cheese Garlic Fries', 'Small Bites', 0.00, 220.00, 100, 5, '2106', 5.00),
    ('SML-008', 'Bacon & Egg with Garlic Bread', 'Bacon & Egg with Garlic Bread', 'Small Bites', 0.00, 250.00, 100, 5, '1905', 5.00),
    ('SML-009', 'Chilli Cheese Garlic Bread', 'Chilli Cheese Garlic Bread', 'Small Bites', 0.00, 270.00, 100, 5, '1905', 5.00),
    ('WRP-001', 'Hummus with Pita Pocket Wrap (Veg)', 'Hummus with Pita Pocket Wrap (Veg)', 'Wraps', 0.00, 300.00, 100, 5, '1905', 5.00),
    ('WRP-002', 'Hummus with Pita Pocket Wrap (Non Veg)', 'Hummus with Pita Pocket Wrap (Non Veg)', 'Wraps', 0.00, 350.00, 100, 5, '1905', 5.00),
    ('WRP-003', 'Jamaican Wrap (Non Veg)', 'Jamaican Wrap (Non Veg)', 'Wraps', 0.00, 330.00, 100, 5, '1905', 5.00),
    ('WRP-004', 'Mexican Wrap (Veg)', 'Mexican Wrap (Veg)', 'Wraps', 0.00, 270.00, 100, 5, '1905', 5.00),
    ('WRP-005', 'Mexican Wrap (Non Veg)', 'Mexican Wrap (Non Veg)', 'Wraps', 0.00, 330.00, 100, 5, '1905', 5.00),
    ('PAS-006', 'Pasta Al Fungi (Veg)', 'Pasta Al Fungi (Veg)', 'Pasta & Noodles', 0.00, 350.00, 100, 5, '1902', 5.00),
    ('PAS-007', 'Pasta Al Fungi (Non Veg)', 'Pasta Al Fungi (Non Veg)', 'Pasta & Noodles', 0.00, 390.00, 100, 5, '1902', 5.00),
    ('PAS-008', 'Spaghetti Pasta (Veg)', 'Spaghetti Pasta (Veg)', 'Pasta & Noodles', 0.00, 350.00, 100, 5, '1902', 5.00),
    ('PAS-009', 'Spaghetti Pasta (Non Veg)', 'Spaghetti Pasta (Non Veg)', 'Pasta & Noodles', 0.00, 390.00, 100, 5, '1902', 5.00),
    ('SLD-001', 'Waldorf Salad (Veg)', 'Waldorf Salad (Veg)', 'Salads', 0.00, 300.00, 100, 5, '2106', 5.00),
    ('SLD-002', 'Chicken Waldorf Salad (Non Veg)', 'Chicken Waldorf Salad (Non Veg)', 'Salads', 0.00, 350.00, 100, 5, '2106', 5.00),
    ('SLD-003', 'Chef Special Salad', 'Chef Special Salad', 'Salads', 0.00, 350.00, 100, 5, '2106', 5.00);
    PRINT 'Default Cafe Menu Dishes seeded.';
END
GO

-- 3.9 Default Raw Materials & Kitchen Stock
IF NOT EXISTS (SELECT 1 FROM RawMaterials WHERE Code = 'RAW-001')
BEGIN
    INSERT INTO RawMaterials (Code, Name, Category, Unit, CurrentStock, MinStockLevel, UnitPrice) VALUES
    ('RAW-001', 'Amul Taaza Fresh Milk 1L', 'Dairy & Milk', 'Litre', 50.000, 10.000, 64.00),
    ('RAW-002', 'Arabica Dark Roast Coffee Beans', 'Coffee & Tea', 'Kg', 15.000, 3.000, 950.00),
    ('RAW-003', 'Darjeeling Special Tea Leaves', 'Coffee & Tea', 'Kg', 8.000, 2.000, 480.00),
    ('RAW-004', 'Mozzarella Diced Pizza Cheese', 'Dairy & Milk', 'Kg', 20.000, 5.000, 450.00),
    ('RAW-005', 'Amul Salted Butter Block', 'Dairy & Milk', 'Kg', 12.000, 3.000, 520.00),
    ('RAW-006', 'Fresh Chicken Boneless Breast', 'Meats & Non-Veg', 'Kg', 25.000, 5.000, 280.00),
    ('RAW-007', 'Refined Wheat Flour (Maida 00)', 'Pantry & Grains', 'Kg', 40.000, 10.000, 45.00),
    ('RAW-008', 'Granulated White Sugar', 'Pantry & Grains', 'Kg', 30.000, 5.000, 44.00),
    ('RAW-009', 'Durum Wheat Penne Pasta', 'Pantry & Grains', 'Kg', 18.000, 4.000, 140.00),
    ('RAW-010', 'San Marzano Tomato Pizza Sauce', 'Pantry & Grains', 'Kg', 15.000, 3.000, 180.00),
    ('RAW-011', 'Vanilla & Caramel Flavor Syrups', 'Beverages & Syrups', 'Litre', 8.000, 2.000, 380.00),
    ('RAW-012', 'Refined Sunflower Cooking Oil', 'Pantry & Grains', 'Litre', 30.000, 5.000, 135.00),
    ('RAW-013', 'Kraft Takeaway Paper Meal Boxes', 'Packaging & Disposables', 'Pcs', 200.000, 50.000, 6.50),
    ('RAW-014', 'Hot Beverage Paper Cups 250ml', 'Packaging & Disposables', 'Pcs', 300.000, 50.000, 4.00);
    PRINT 'Default Raw Materials seeded.';
END
GO

-- 3.10 HSN & SAC Masters (Cafe GST)
IF NOT EXISTS (SELECT 1 FROM HsnSacMaster WHERE Code = '996331')
BEGIN
    INSERT INTO HsnSacMaster (Code, Type, Description, GSTRate, IsActive) VALUES 
    ('996331', 'SAC', 'Restaurant, cafe and local dining food serving services (Air-conditioned & indoor seating)', 5.00, 1),
    ('996332', 'SAC', 'Takeaway, packaging counter and home delivery food / beverage services', 5.00, 1),
    ('996333', 'SAC', 'Outdoor cafe catering and private event beverage food serving services', 5.00, 1),
    ('996339', 'SAC', 'Other food and beverage preparation, barista brews and hospitality dining services', 5.00, 1),
    ('0901', 'HSN', 'Coffee beans, roasted coffee, ground espresso blends, filter coffee and beans', 5.00, 1),
    ('0902', 'HSN', 'Tea leaves, green tea, Darjeeling brew, organic herbal infusions and specialty teas', 5.00, 1),
    ('1902', 'HSN', 'Pasta, spaghetti, macaroni, noodles, chowmein and soupy laphing preparations', 12.00, 1),
    ('1905', 'HSN', 'Bakery products, cakes, pastries, croissants, toasted bread, cookies, Tibetan bread', 18.00, 1),
    ('2106', 'HSN', 'Ready food preparations, momos, pizzas, sandwiches, snacks, sauces and cafe dishes', 5.00, 1),
    ('2202', 'HSN', 'Non-alcoholic beverages, mocktails, iced teas, fruit drinks, craft coolers and sodas', 18.00, 1),
    ('0401', 'HSN', 'Fresh milk, dairy cream and milk beverages for coffee & shakes', 5.00, 1),
    ('0406', 'HSN', 'Cheese (mozzarella, cheddar, parmesan) for pizzas, sandwiches and pasta', 12.00, 1),
    ('2009', 'HSN', 'Fresh fruit juices, vegetable smoothies and cold-pressed drinks', 12.00, 1),
    ('4819', 'HSN', 'Food packaging containers, takeaway boxes, beverage cups, paper bags', 18.00, 1);
    PRINT 'Default HSN/SAC master seeded.';
END
GO

-- 3.11 Default AppProfile (The Local Cafe Brand)
IF NOT EXISTS (SELECT 1 FROM AppProfile)
BEGIN
    INSERT INTO AppProfile (
        OwnerName, ShopName, Phone, Email, Address, ThemePreset, FontSizePreset,
        BackupFolderPath, GoogleDriveAddress, GSTIN, StateName, StateCode,
        IsTaxInclusive, DefaultBillType, DefaultGSTRate, ReceiptFooterText,
        DefaultPackingCharge, LogoPath, AutoShowQROnUPI, PrintQROnReceipt
    ) VALUES (
        'Cafe Manager',
        'The Local Cafe',
        '9971592652',
        'contact@thelocalcafe.com',
        'vajra world Mall Balwa khani, Gangtok Sikkim 737101',
        'Emerald Mint',
        'Medium',
        'D:\MeroDokanCafe\DailyDatabaseBackup',
        'https://script.google.com/macros/s/AKfycbwm3WKMbeToLZt10WTPGrHwL4XsA8JgVO_H4MAaraDpssgTfUNs1x_ECblU4cKkRMAx/exec',
        '11BIDPB3498K1ZD',
        'Sikkim',
        '11',
        1,
        'GST',
        5.00,
        'Tashi Delek! Thukje Che!',
        40.00,
        'Assets\logo.jpg',
        1,
        1
    );
    PRINT 'Default AppProfile seeded.';
END
GO

PRINT '=======================================================';
PRINT '   MERO DOKAN CAFE DATABASE SETUP COMPLETE!';
PRINT '=======================================================';
