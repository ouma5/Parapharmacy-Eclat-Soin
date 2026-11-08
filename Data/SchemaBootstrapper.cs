using System;
using System.Data.SqlClient;
using EclatEtSoin.Helpers;

namespace EclatEtSoin.Data
{
    public static class SchemaBootstrapper
    {
        public static void EnsureDatabase()
        {
            using (var cn = new SqlConnection(Database.MasterConnectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"
IF DB_ID(N'PharmacyoumS') IS NOT NULL AND DB_ID(N'PharmacyDB') IS NULL
BEGIN
    -- La base historique existe : on crée PharmacyDB séparément sans y copier les données.
    -- Vérifiez PharmacyoumS dans SSMS avant toute migration.
    CREATE DATABASE [PharmacyDB];
END
ELSE IF DB_ID(N'PharmacyDB') IS NULL
BEGIN
    CREATE DATABASE [PharmacyDB];
END", cn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            var statements = new[]
            {
@"IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(80) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(200) NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Role NVARCHAR(30) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);",
@"IF OBJECT_ID(N'dbo.Clients', N'U') IS NULL
CREATE TABLE dbo.Clients (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(40) NULL,
    Address NVARCHAR(250) NULL,
    Email NVARCHAR(150) NULL,
    IsCounter BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);",
@"IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
CREATE TABLE dbo.Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL UNIQUE,
    Description NVARCHAR(250) NULL
);",
@"IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NULL
CREATE TABLE dbo.Suppliers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Company NVARCHAR(150) NULL,
    Phone NVARCHAR(40) NULL,
    Address NVARCHAR(250) NULL,
    Email NVARCHAR(150) NULL,
    Notes NVARCHAR(400) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);",
@"IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
CREATE TABLE dbo.Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Barcode NVARCHAR(60) NULL,
    Reference NVARCHAR(60) NULL,
    Designation NVARCHAR(200) NOT NULL,
    CategoryId INT NULL,
    Brand NVARCHAR(120) NULL,
    ImagePath NVARCHAR(400) NULL,
    PurchasePrice DECIMAL(18,2) NOT NULL DEFAULT 0,
    SalePrice DECIMAL(18,2) NOT NULL DEFAULT 0,
    Pph DECIMAL(18,2) NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Quantity INT NOT NULL DEFAULT 0,
    MinStock INT NOT NULL DEFAULT 5,
    ExpirationDate DATE NULL,
    SupplierId INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);",
@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Products_Barcode' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE UNIQUE INDEX UX_Products_Barcode ON dbo.Products(Barcode) WHERE Barcode IS NOT NULL AND Barcode <> N'';",
@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_Designation' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_Designation ON dbo.Products(Designation);",
@"IF OBJECT_ID(N'dbo.Purchases', N'U') IS NULL
CREATE TABLE dbo.Purchases (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Number NVARCHAR(40) NOT NULL UNIQUE,
    SupplierId INT NOT NULL,
    PurchaseDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Total DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(20) NOT NULL DEFAULT N'Brouillon',
    ReceivedAt DATETIME2 NULL,
    CreatedBy INT NULL
);",
@"IF OBJECT_ID(N'dbo.PurchaseDetails', N'U') IS NULL
CREATE TABLE dbo.PurchaseDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PurchaseId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    LineTotal DECIMAL(18,2) NOT NULL
);",
@"IF OBJECT_ID(N'dbo.Sales', N'U') IS NULL
CREATE TABLE dbo.Sales (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Number NVARCHAR(40) NOT NULL UNIQUE,
    SaleDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ClientId INT NULL,
    UserId INT NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Total DECIMAL(18,2) NOT NULL DEFAULT 0,
    AmountReceived DECIMAL(18,2) NOT NULL DEFAULT 0,
    ChangeDue DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaymentMode NVARCHAR(40) NOT NULL DEFAULT N'Espèces',
    Status NVARCHAR(20) NOT NULL DEFAULT N'Finalisee',
    CancelReason NVARCHAR(250) NULL
);",
@"IF OBJECT_ID(N'dbo.SaleDetails', N'U') IS NULL
CREATE TABLE dbo.SaleDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SaleId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    LineTotal DECIMAL(18,2) NOT NULL
);",
@"IF OBJECT_ID(N'dbo.StockMovements', N'U') IS NULL
CREATE TABLE dbo.StockMovements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    MovementDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Type NVARCHAR(20) NOT NULL,
    Quantity INT NOT NULL,
    Reason NVARCHAR(250) NULL,
    UserId INT NULL,
    RelatedDocument NVARCHAR(60) NULL
);",
@"IF OBJECT_ID(N'dbo.Quotes', N'U') IS NULL
CREATE TABLE dbo.Quotes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Number NVARCHAR(40) NOT NULL UNIQUE,
    QuoteDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ClientId INT NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Total DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(20) NOT NULL DEFAULT N'Ouvert',
    ConvertedSaleId INT NULL
);",
@"IF OBJECT_ID(N'dbo.QuoteDetails', N'U') IS NULL
CREATE TABLE dbo.QuoteDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuoteId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    LineTotal DECIMAL(18,2) NOT NULL
);",
@"IF OBJECT_ID(N'dbo.DeliveryNotes', N'U') IS NULL
CREATE TABLE dbo.DeliveryNotes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Number NVARCHAR(40) NOT NULL UNIQUE,
    DeliveryDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ClientId INT NULL,
    SaleId INT NULL,
    Total DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(20) NOT NULL DEFAULT N'Livre',
    StockApplied BIT NOT NULL DEFAULT 0
);",
@"IF OBJECT_ID(N'dbo.DeliveryNoteDetails', N'U') IS NULL
CREATE TABLE dbo.DeliveryNoteDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DeliveryNoteId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL
);",
@"IF OBJECT_ID(N'dbo.Settings', N'U') IS NULL
CREATE TABLE dbo.Settings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    [Key] NVARCHAR(80) NOT NULL UNIQUE,
    [Value] NVARCHAR(500) NOT NULL
);"
            };

            using (var cn = Database.CreateConnection())
            {
                cn.Open();
                foreach (var sql in statements)
                {
                    using (var cmd = new SqlCommand(sql, cn))
                        cmd.ExecuteNonQuery();
                }
            }

            Seed();
        }

        private static void Seed()
        {
            var count = Convert.ToInt32(Database.Scalar("SELECT COUNT(*) FROM Users") ?? 0);
            if (count == 0)
            {
                Database.Execute(
                    "INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive) VALUES (@u,@p,@n,@r,1)",
                    Database.P("@u", "admin"),
                    Database.P("@p", PasswordHasher.Hash("Admin@123")),
                    Database.P("@n", "Administrateur"),
                    Database.P("@r", "Administrateur"));
                Database.Execute(
                    "INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive) VALUES (@u,@p,@n,@r,1)",
                    Database.P("@u", "vendeur"),
                    Database.P("@p", PasswordHasher.Hash("Vendeur@123")),
                    Database.P("@n", "Caisse"),
                    Database.P("@r", "Vendeur"));
            }

            if (Convert.ToInt32(Database.Scalar("SELECT COUNT(*) FROM Clients") ?? 0) == 0)
            {
                Database.Execute(
                    "INSERT INTO Clients (FullName, IsCounter) VALUES (N'Client comptoir', 1)");
            }

            if (Convert.ToInt32(Database.Scalar("SELECT COUNT(*) FROM Categories") ?? 0) == 0)
            {
                Database.Execute("INSERT INTO Categories (Name) VALUES (N'Hygiène bucco-dentaire'), (N'Soins du visage'), (N'Compléments')");
            }

            if (Convert.ToInt32(Database.Scalar("SELECT COUNT(*) FROM Settings") ?? 0) == 0)
            {
                UpsertSetting("TradeName", "Parapharmacie Èclat & Soin");
                UpsertSetting("Address", "Casablanca, Sidi Othmane, Lbaladia, Rue 46, N°49");
                UpsertSetting("Phone", "");
                UpsertSetting("DefaultPrinter", "");
                UpsertSetting("DefaultMinStock", "5");
            }
        }

        public static void UpsertSetting(string key, string value)
        {
            Database.Execute(@"
IF EXISTS (SELECT 1 FROM Settings WHERE [Key]=@k) UPDATE Settings SET [Value]=@v WHERE [Key]=@k;
ELSE INSERT INTO Settings ([Key],[Value]) VALUES (@k,@v);",
                Database.P("@k", key), Database.P("@v", value ?? ""));
        }

        public static string GetSetting(string key, string fallback = "")
        {
            var v = Database.Scalar("SELECT [Value] FROM Settings WHERE [Key]=@k", Database.P("@k", key));
            return v == null ? fallback : v.ToString();
        }
    }
}
