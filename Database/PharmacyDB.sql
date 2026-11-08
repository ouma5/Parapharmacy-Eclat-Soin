/* PharmacyDB — Èclat & Soin
   Instance cible : .\SQLEXPRESS
   Exécuter dans SSMS si l'application ne peut pas créer la base automatiquement.
*/
IF DB_ID(N'PharmacyDB') IS NULL
BEGIN
    CREATE DATABASE [PharmacyDB];
END
GO
USE [PharmacyDB];
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(80) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(200) NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Role NVARCHAR(30) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

IF OBJECT_ID(N'dbo.Clients', N'U') IS NULL
CREATE TABLE dbo.Clients (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(40) NULL,
    Address NVARCHAR(250) NULL,
    Email NVARCHAR(150) NULL,
    IsCounter BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
CREATE TABLE dbo.Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL UNIQUE,
    Description NVARCHAR(250) NULL
);

IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NULL
CREATE TABLE dbo.Suppliers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Company NVARCHAR(150) NULL,
    Phone NVARCHAR(40) NULL,
    Address NVARCHAR(250) NULL,
    Email NVARCHAR(150) NULL,
    Notes NVARCHAR(400) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
CREATE TABLE dbo.Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Barcode NVARCHAR(60) NULL,
    Reference NVARCHAR(60) NULL,
    Designation NVARCHAR(200) NOT NULL,
    CategoryId INT NULL REFERENCES dbo.Categories(Id),
    Brand NVARCHAR(120) NULL,
    ImagePath NVARCHAR(400) NULL,
    PurchasePrice DECIMAL(18,2) NOT NULL DEFAULT 0,
    SalePrice DECIMAL(18,2) NOT NULL DEFAULT 0,
    Pph DECIMAL(18,2) NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Quantity INT NOT NULL DEFAULT 0,
    MinStock INT NOT NULL DEFAULT 5,
    ExpirationDate DATE NULL,
    SupplierId INT NULL REFERENCES dbo.Suppliers(Id),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Products_Barcode' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE UNIQUE INDEX UX_Products_Barcode ON dbo.Products(Barcode) WHERE Barcode IS NOT NULL AND Barcode <> N'';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_Designation' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_Designation ON dbo.Products(Designation);

IF OBJECT_ID(N'dbo.Purchases', N'U') IS NULL
CREATE TABLE dbo.Purchases (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Number NVARCHAR(40) NOT NULL UNIQUE,
    SupplierId INT NOT NULL REFERENCES dbo.Suppliers(Id),
    PurchaseDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Total DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(20) NOT NULL DEFAULT N'Brouillon',
    ReceivedAt DATETIME2 NULL,
    CreatedBy INT NULL REFERENCES dbo.Users(Id)
);

IF OBJECT_ID(N'dbo.PurchaseDetails', N'U') IS NULL
CREATE TABLE dbo.PurchaseDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PurchaseId INT NOT NULL REFERENCES dbo.Purchases(Id),
    ProductId INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    LineTotal DECIMAL(18,2) NOT NULL
);

IF OBJECT_ID(N'dbo.Sales', N'U') IS NULL
CREATE TABLE dbo.Sales (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Number NVARCHAR(40) NOT NULL UNIQUE,
    SaleDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ClientId INT NULL REFERENCES dbo.Clients(Id),
    UserId INT NULL REFERENCES dbo.Users(Id),
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Total DECIMAL(18,2) NOT NULL DEFAULT 0,
    AmountReceived DECIMAL(18,2) NOT NULL DEFAULT 0,
    ChangeDue DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaymentMode NVARCHAR(40) NOT NULL DEFAULT N'Espèces',
    Status NVARCHAR(20) NOT NULL DEFAULT N'Finalisee',
    CancelReason NVARCHAR(250) NULL
);

IF OBJECT_ID(N'dbo.SaleDetails', N'U') IS NULL
CREATE TABLE dbo.SaleDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SaleId INT NOT NULL REFERENCES dbo.Sales(Id),
    ProductId INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    LineTotal DECIMAL(18,2) NOT NULL
);

IF OBJECT_ID(N'dbo.StockMovements', N'U') IS NULL
CREATE TABLE dbo.StockMovements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL REFERENCES dbo.Products(Id),
    MovementDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Type NVARCHAR(20) NOT NULL,
    Quantity INT NOT NULL,
    Reason NVARCHAR(250) NULL,
    UserId INT NULL REFERENCES dbo.Users(Id),
    RelatedDocument NVARCHAR(60) NULL
);

IF OBJECT_ID(N'dbo.Quotes', N'U') IS NULL
CREATE TABLE dbo.Quotes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Number NVARCHAR(40) NOT NULL UNIQUE,
    QuoteDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ClientId INT NULL REFERENCES dbo.Clients(Id),
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Total DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(20) NOT NULL DEFAULT N'Ouvert',
    ConvertedSaleId INT NULL
);

IF OBJECT_ID(N'dbo.QuoteDetails', N'U') IS NULL
CREATE TABLE dbo.QuoteDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuoteId INT NOT NULL REFERENCES dbo.Quotes(Id),
    ProductId INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    LineTotal DECIMAL(18,2) NOT NULL
);

IF OBJECT_ID(N'dbo.DeliveryNotes', N'U') IS NULL
CREATE TABLE dbo.DeliveryNotes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Number NVARCHAR(40) NOT NULL UNIQUE,
    DeliveryDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ClientId INT NULL REFERENCES dbo.Clients(Id),
    SaleId INT NULL REFERENCES dbo.Sales(Id),
    Total DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(20) NOT NULL DEFAULT N'Livre',
    StockApplied BIT NOT NULL DEFAULT 0
);

IF OBJECT_ID(N'dbo.DeliveryNoteDetails', N'U') IS NULL
CREATE TABLE dbo.DeliveryNoteDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DeliveryNoteId INT NOT NULL REFERENCES dbo.DeliveryNotes(Id),
    ProductId INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL
);

IF OBJECT_ID(N'dbo.Settings', N'U') IS NULL
CREATE TABLE dbo.Settings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    [Key] NVARCHAR(80) NOT NULL UNIQUE,
    [Value] NVARCHAR(500) NOT NULL
);
GO
