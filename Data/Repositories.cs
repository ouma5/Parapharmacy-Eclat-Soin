using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EclatEtSoin.Helpers;
using EclatEtSoin.Models;

namespace EclatEtSoin.Data
{
    public static class UserRepository
    {
        public static User GetByUsername(string username)
        {
            var t = Database.Query("SELECT * FROM Users WHERE Username=@u", Database.P("@u", username));
            return t.Rows.Count == 0 ? null : Map(t.Rows[0]);
        }

        public static List<User> GetAll()
        {
            var list = new List<User>();
            foreach (DataRow r in Database.Query("SELECT * FROM Users ORDER BY Username").Rows)
                list.Add(Map(r));
            return list;
        }

        public static void Insert(User u)
        {
            Database.Execute(
                "INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive) VALUES (@u,@p,@n,@r,@a)",
                Database.P("@u", u.Username), Database.P("@p", u.PasswordHash),
                Database.P("@n", u.FullName), Database.P("@r", u.Role), Database.P("@a", u.IsActive));
        }

        public static void Update(User u)
        {
            Database.Execute(
                "UPDATE Users SET FullName=@n, Role=@r, IsActive=@a WHERE Id=@id",
                Database.P("@n", u.FullName), Database.P("@r", u.Role),
                Database.P("@a", u.IsActive), Database.P("@id", u.Id));
        }

        public static void UpdatePassword(int id, string hash)
        {
            Database.Execute("UPDATE Users SET PasswordHash=@p WHERE Id=@id", Database.P("@p", hash), Database.P("@id", id));
        }

        private static User Map(DataRow r)
        {
            return new User
            {
                Id = Convert.ToInt32(r["Id"]),
                Username = r["Username"].ToString(),
                PasswordHash = r["PasswordHash"].ToString(),
                FullName = r["FullName"].ToString(),
                Role = r["Role"].ToString(),
                IsActive = Convert.ToBoolean(r["IsActive"]),
                CreatedAt = Convert.ToDateTime(r["CreatedAt"])
            };
        }
    }

    public static class ClientRepository
    {
        public static List<Client> GetAll()
        {
            var list = new List<Client>();
            foreach (DataRow r in Database.Query("SELECT * FROM Clients ORDER BY FullName").Rows)
                list.Add(Map(r));
            return list;
        }

        public static Client GetById(int id)
        {
            var t = Database.Query("SELECT * FROM Clients WHERE Id=@id", Database.P("@id", id));
            return t.Rows.Count == 0 ? null : Map(t.Rows[0]);
        }

        public static List<Client> Search(string q)
        {
            var list = new List<Client>();
            foreach (DataRow r in Database.Query(
                "SELECT * FROM Clients WHERE FullName LIKE @q OR Phone LIKE @q ORDER BY FullName",
                Database.P("@q", "%" + q + "%")).Rows)
                list.Add(Map(r));
            return list;
        }

        public static void Insert(Client c)
        {
            Database.Execute(
                "INSERT INTO Clients (FullName, Phone, Address, Email, IsCounter) VALUES (@n,@p,@a,@e,@c)",
                Database.P("@n", c.FullName), Database.P("@p", c.Phone), Database.P("@a", c.Address),
                Database.P("@e", c.Email), Database.P("@c", c.IsCounter));
        }

        public static void Update(Client c)
        {
            Database.Execute(
                "UPDATE Clients SET FullName=@n, Phone=@p, Address=@a, Email=@e WHERE Id=@id",
                Database.P("@n", c.FullName), Database.P("@p", c.Phone), Database.P("@a", c.Address),
                Database.P("@e", c.Email), Database.P("@id", c.Id));
        }

        private static Client Map(DataRow r)
        {
            return new Client
            {
                Id = Convert.ToInt32(r["Id"]),
                FullName = r["FullName"].ToString(),
                Phone = r["Phone"] as string,
                Address = r["Address"] as string,
                Email = r["Email"] as string,
                IsCounter = Convert.ToBoolean(r["IsCounter"]),
                CreatedAt = Convert.ToDateTime(r["CreatedAt"])
            };
        }
    }

    public static class CategoryRepository
    {
        public static List<Categorie> GetAll()
        {
            var list = new List<Categorie>();
            foreach (DataRow r in Database.Query("SELECT * FROM Categories ORDER BY Name").Rows)
                list.Add(new Categorie { Id = Convert.ToInt32(r["Id"]), Name = r["Name"].ToString(), Description = r["Description"] as string });
            return list;
        }

        public static void Insert(Categorie c)
        {
            Database.Execute("INSERT INTO Categories (Name, Description) VALUES (@n,@d)", Database.P("@n", c.Name), Database.P("@d", c.Description));
        }

        public static void Update(Categorie c)
        {
            Database.Execute("UPDATE Categories SET Name=@n, Description=@d WHERE Id=@id",
                Database.P("@n", c.Name), Database.P("@d", c.Description), Database.P("@id", c.Id));
        }

        public static bool Delete(int id)
        {
            var used = Convert.ToInt32(Database.Scalar("SELECT COUNT(*) FROM Products WHERE CategoryId=@id", Database.P("@id", id)));
            if (used > 0) return false;
            Database.Execute("DELETE FROM Categories WHERE Id=@id", Database.P("@id", id));
            return true;
        }
    }

    public static class SupplierRepository
    {
        public static List<Fournisseur> GetAll()
        {
            var list = new List<Fournisseur>();
            foreach (DataRow r in Database.Query("SELECT * FROM Suppliers ORDER BY Name").Rows)
                list.Add(Map(r));
            return list;
        }

        public static Fournisseur GetById(int id)
        {
            var t = Database.Query("SELECT * FROM Suppliers WHERE Id=@id", Database.P("@id", id));
            return t.Rows.Count == 0 ? null : Map(t.Rows[0]);
        }

        public static List<Fournisseur> Search(string q)
        {
            var list = new List<Fournisseur>();
            foreach (DataRow r in Database.Query(
                "SELECT * FROM Suppliers WHERE Name LIKE @q OR Company LIKE @q OR Phone LIKE @q ORDER BY Name",
                Database.P("@q", "%" + q + "%")).Rows)
                list.Add(Map(r));
            return list;
        }

        public static void Insert(Fournisseur s)
        {
            Database.Execute(
                "INSERT INTO Suppliers (Name, Company, Phone, Address, Email, Notes) VALUES (@n,@c,@p,@a,@e,@o)",
                Database.P("@n", s.Name), Database.P("@c", s.Company), Database.P("@p", s.Phone),
                Database.P("@a", s.Address), Database.P("@e", s.Email), Database.P("@o", s.Notes));
        }

        public static void Update(Fournisseur s)
        {
            Database.Execute(
                "UPDATE Suppliers SET Name=@n, Company=@c, Phone=@p, Address=@a, Email=@e, Notes=@o WHERE Id=@id",
                Database.P("@n", s.Name), Database.P("@c", s.Company), Database.P("@p", s.Phone),
                Database.P("@a", s.Address), Database.P("@e", s.Email), Database.P("@o", s.Notes), Database.P("@id", s.Id));
        }

        private static Fournisseur Map(DataRow r)
        {
            return new Fournisseur
            {
                Id = Convert.ToInt32(r["Id"]),
                Name = r["Name"].ToString(),
                Company = r["Company"] as string,
                Phone = r["Phone"] as string,
                Address = r["Address"] as string,
                Email = r["Email"] as string,
                Notes = r["Notes"] as string,
                CreatedAt = Convert.ToDateTime(r["CreatedAt"])
            };
        }
    }

    public static class ProductRepository
    {
        private const string SelectSql = @"
SELECT p.*, c.Name AS CategoryName
FROM Products p
LEFT JOIN Categories c ON c.Id = p.CategoryId ";

        public static List<Produit> GetAll(bool activeOnly = false)
        {
            var sql = SelectSql + (activeOnly ? " WHERE p.IsActive=1 " : "") + " ORDER BY p.Designation";
            return MapList(Database.Query(sql));
        }

        public static Produit GetById(int id)
        {
            var t = Database.Query(SelectSql + " WHERE p.Id=@id", Database.P("@id", id));
            return t.Rows.Count == 0 ? null : Map(t.Rows[0]);
        }

        public static Produit GetByBarcode(string barcode)
        {
            var t = Database.Query(SelectSql + " WHERE p.Barcode=@b AND p.IsActive=1", Database.P("@b", barcode));
            return t.Rows.Count == 0 ? null : Map(t.Rows[0]);
        }

        public static List<Produit> Search(string q, int? categoryId)
        {
            var sql = SelectSql + @" WHERE (@q='' OR p.Designation LIKE @like OR p.Reference LIKE @like OR p.Barcode LIKE @like)
AND (@cid=0 OR p.CategoryId=@cid) ORDER BY p.Designation";
            return MapList(Database.Query(sql,
                Database.P("@q", q ?? ""),
                Database.P("@like", "%" + (q ?? "") + "%"),
                Database.P("@cid", categoryId ?? 0)));
        }

        public static int Insert(Produit p)
        {
            var id = Database.Scalar(@"
INSERT INTO Products (Barcode, Reference, Designation, CategoryId, Brand, ImagePath, PurchasePrice, SalePrice, Pph, Discount, Quantity, MinStock, ExpirationDate, SupplierId, IsActive)
VALUES (@b,@r,@d,@c,@br,@img,@pp,@sp,@pph,@disc,@q,@m,@exp,@s,1);
SELECT CAST(SCOPE_IDENTITY() AS INT);",
                Params(p));
            return Convert.ToInt32(id);
        }

        public static void Update(Produit p)
        {
            Database.Execute(@"
UPDATE Products SET Barcode=@b, Reference=@r, Designation=@d, CategoryId=@c, Brand=@br, ImagePath=@img,
PurchasePrice=@pp, SalePrice=@sp, Pph=@pph, Discount=@disc, MinStock=@m, ExpirationDate=@exp, SupplierId=@s
WHERE Id=@id", Params(p, true));
        }

        public static void Deactivate(int id)
        {
            Database.Execute("UPDATE Products SET IsActive=0 WHERE Id=@id", Database.P("@id", id));
        }

        public static bool IsUsedInSales(int id)
        {
            return Convert.ToInt32(Database.Scalar("SELECT COUNT(*) FROM SaleDetails WHERE ProductId=@id", Database.P("@id", id))) > 0;
        }

        public static bool DeleteIfUnused(int id)
        {
            if (IsUsedInSales(id) ||
                Convert.ToInt32(Database.Scalar("SELECT COUNT(*) FROM PurchaseDetails WHERE ProductId=@id", Database.P("@id", id))) > 0)
                return false;
            Database.Execute("DELETE FROM Products WHERE Id=@id", Database.P("@id", id));
            return true;
        }

        private static SqlParameter[] Params(Produit p, bool withId = false)
        {
            var list = new List<SqlParameter>
            {
                Database.P("@b", string.IsNullOrWhiteSpace(p.Barcode) ? null : p.Barcode),
                Database.P("@r", p.Reference),
                Database.P("@d", p.Designation),
                Database.P("@c", (object)p.CategoryId ?? DBNull.Value),
                Database.P("@br", p.Brand),
                Database.P("@img", p.ImagePath),
                Database.P("@pp", p.PurchasePrice),
                Database.P("@sp", p.SalePrice),
                Database.P("@pph", (object)p.Pph ?? DBNull.Value),
                Database.P("@disc", p.Discount),
                Database.P("@q", p.Quantity),
                Database.P("@m", p.MinStock),
                Database.P("@exp", (object)p.ExpirationDate ?? DBNull.Value),
                Database.P("@s", (object)p.SupplierId ?? DBNull.Value)
            };
            if (withId) list.Add(Database.P("@id", p.Id));
            return list.ToArray();
        }

        private static List<Produit> MapList(DataTable t)
        {
            var list = new List<Produit>();
            foreach (DataRow r in t.Rows) list.Add(Map(r));
            return list;
        }

        public static Produit Map(DataRow r)
        {
            return new Produit
            {
                Id = Convert.ToInt32(r["Id"]),
                Barcode = r["Barcode"] as string,
                Reference = r["Reference"] as string,
                Designation = r["Designation"].ToString(),
                CategoryId = r["CategoryId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["CategoryId"]),
                CategoryName = r.Table.Columns.Contains("CategoryName") ? r["CategoryName"] as string : null,
                Brand = r["Brand"] as string,
                ImagePath = r["ImagePath"] as string,
                PurchasePrice = Convert.ToDecimal(r["PurchasePrice"]),
                SalePrice = Convert.ToDecimal(r["SalePrice"]),
                Pph = r["Pph"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["Pph"]),
                Discount = Convert.ToDecimal(r["Discount"]),
                Quantity = Convert.ToInt32(r["Quantity"]),
                MinStock = Convert.ToInt32(r["MinStock"]),
                ExpirationDate = r["ExpirationDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["ExpirationDate"]),
                SupplierId = r["SupplierId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["SupplierId"]),
                IsActive = Convert.ToBoolean(r["IsActive"]),
                CreatedAt = Convert.ToDateTime(r["CreatedAt"])
            };
        }
    }
}
