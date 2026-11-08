using System;
using System.Collections.Generic;
using System.Data;
using EclatEtSoin.Data;
using EclatEtSoin.Helpers;
using EclatEtSoin.Models;

namespace EclatEtSoin.Services
{
    public static class AuthService
    {
        public static User Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Veuillez saisir le nom d'utilisateur et le mot de passe.");
            var user = UserRepository.GetByUsername(username.Trim());
            if (user == null || !user.IsActive || !PasswordHasher.Verify(password, user.PasswordHash))
                throw new InvalidOperationException("Identifiants incorrects.");
            return user;
        }
    }

    public static class ProductService
    {
        public static void Validate(Produit p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.Designation))
                throw new InvalidOperationException("Veuillez saisir la désignation du produit.");
            if (p.SalePrice < 0) throw new InvalidOperationException("Le prix de vente doit être valide.");
            if (p.PurchasePrice < 0) throw new InvalidOperationException("Le prix d'achat doit être valide.");
            if (p.Quantity < 0) throw new InvalidOperationException("La quantité doit être valide.");
        }

        public static int Save(Produit p)
        {
            Validate(p);
            if (p.Id == 0) return ProductRepository.Insert(p);
            ProductRepository.Update(p);
            return p.Id;
        }

        public static void Delete(Produit p)
        {
            if (p == null) return;
            if (ProductRepository.IsUsedInSales(p.Id))
            {
                ProductRepository.Deactivate(p.Id);
                return;
            }
            if (!ProductRepository.DeleteIfUnused(p.Id))
                ProductRepository.Deactivate(p.Id);
        }
    }

    public static class ReportService
    {
        public static DataTable DashboardKpis()
        {
            return Database.Query(@"
SELECT
 (SELECT COUNT(*) FROM Products WHERE IsActive=1) AS Produits,
 (SELECT COUNT(*) FROM Clients) AS Clients,
 (SELECT COUNT(*) FROM Suppliers) AS Fournisseurs,
 (SELECT COUNT(*) FROM Sales WHERE CAST(SaleDate AS DATE)=CAST(GETDATE() AS DATE) AND Status=N'Finalisee') AS VentesJour,
 (SELECT ISNULL(SUM(Total),0) FROM Sales WHERE CAST(SaleDate AS DATE)=CAST(GETDATE() AS DATE) AND Status=N'Finalisee') AS CaJour,
 (SELECT COUNT(*) FROM Products WHERE IsActive=1 AND Quantity>0 AND Quantity<=MinStock) AS StockFaible,
 (SELECT COUNT(*) FROM Products WHERE IsActive=1 AND Quantity<=0) AS Rupture,
 (SELECT COUNT(*) FROM Products WHERE IsActive=1 AND ExpirationDate IS NOT NULL AND ExpirationDate <= DATEADD(DAY,60,GETDATE())) AS Expiration");
        }

        public static DataTable SalesByDay(int days)
        {
            return Database.Query(@"
SELECT CAST(SaleDate AS DATE) AS Jour, SUM(Total) AS Montant
FROM Sales WHERE Status=N'Finalisee' AND SaleDate>=DATEADD(DAY,@d,GETDATE())
GROUP BY CAST(SaleDate AS DATE) ORDER BY Jour",
                Database.P("@d", -days));
        }

        public static DataTable TopProducts()
        {
            return Database.Query(@"
SELECT TOP 8 p.Designation, SUM(d.Quantity) AS Qte
FROM SaleDetails d
INNER JOIN Products p ON p.Id=d.ProductId
INNER JOIN Sales s ON s.Id=d.SaleId AND s.Status=N'Finalisee'
GROUP BY p.Designation ORDER BY Qte DESC");
        }

        public static DataTable LatestSales()
        {
            return Database.Query(@"
SELECT TOP 10 s.Number, s.SaleDate, ISNULL(c.FullName,N'Comptoir') AS Client, s.Total, s.PaymentMode, s.Status
FROM Sales s LEFT JOIN Clients c ON c.Id=s.ClientId
ORDER BY s.SaleDate DESC");
        }

        public static DataTable SalesPeriod(DateTime from, DateTime to)
        {
            return Database.Query(@"
SELECT s.Number, s.SaleDate, ISNULL(c.FullName,N'Comptoir') AS Client, u.FullName AS Vendeur, s.Total, s.PaymentMode, s.Status
FROM Sales s
LEFT JOIN Clients c ON c.Id=s.ClientId
LEFT JOIN Users u ON u.Id=s.UserId
WHERE s.SaleDate>=@f AND s.SaleDate<DATEADD(DAY,1,@t)
ORDER BY s.SaleDate DESC",
                Database.P("@f", from.Date), Database.P("@t", to.Date));
        }

        public static DataTable StockState()
        {
            return Database.Query(@"
SELECT Designation, Barcode, Quantity, MinStock, SalePrice, ExpirationDate,
 CASE WHEN Quantity<=0 THEN N'Rupture' WHEN Quantity<=MinStock THEN N'Stock faible' ELSE N'OK' END AS Etat
FROM Products WHERE IsActive=1 ORDER BY Designation");
        }
    }
}
