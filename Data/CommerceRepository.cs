using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EclatEtSoin.Helpers;
using EclatEtSoin.Models;

namespace EclatEtSoin.Data
{
    public static class CommerceRepository
    {
        public static int CreateSale(int? clientId, int userId, decimal discount, decimal total, decimal received, decimal change, string payment, List<LigneDocument> lines)
        {
            return Database.InTransaction((cn, tx) =>
            {
                var number = DocumentNumbers.Next(cn, tx, "Sales", "VTE");
                foreach (var line in lines)
                {
                    var qty = Convert.ToInt32(Database.Scalar(cn, tx, "SELECT Quantity FROM Products WHERE Id=@id", Database.P("@id", line.ProductId)));
                    if (qty < line.Quantity)
                        throw new InvalidOperationException("Stock insuffisant pour cette quantité.");
                }

                var saleId = Convert.ToInt32(Database.Scalar(cn, tx, @"
INSERT INTO Sales (Number, ClientId, UserId, Discount, Total, AmountReceived, ChangeDue, PaymentMode, Status)
VALUES (@n,@c,@u,@d,@t,@r,@ch,@p,N'Finalisee'); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    Database.P("@n", number), Database.P("@c", (object)clientId ?? DBNull.Value),
                    Database.P("@u", userId), Database.P("@d", discount), Database.P("@t", total),
                    Database.P("@r", received), Database.P("@ch", change), Database.P("@p", payment)));

                foreach (var line in lines)
                {
                    Database.Execute(cn, tx, @"
INSERT INTO SaleDetails (SaleId, ProductId, Quantity, UnitPrice, Discount, LineTotal)
VALUES (@s,@p,@q,@u,@d,@l)",
                        Database.P("@s", saleId), Database.P("@p", line.ProductId), Database.P("@q", line.Quantity),
                        Database.P("@u", line.UnitPrice), Database.P("@d", line.Discount), Database.P("@l", line.LineTotal));
                    Database.Execute(cn, tx, "UPDATE Products SET Quantity = Quantity - @q WHERE Id=@id",
                        Database.P("@q", line.Quantity), Database.P("@id", line.ProductId));
                    Database.Execute(cn, tx, @"
INSERT INTO StockMovements (ProductId, Type, Quantity, Reason, UserId, RelatedDocument)
VALUES (@p,N'Sortie',@q,N'Vente',@u,@n)",
                        Database.P("@p", line.ProductId), Database.P("@q", -line.Quantity),
                        Database.P("@u", userId), Database.P("@n", number));
                }
                return saleId;
            });
        }

        public static void CancelSale(int saleId, string reason, int userId)
        {
            Database.InTransaction((cn, tx) =>
            {
                var status = Convert.ToString(Database.Scalar(cn, tx, "SELECT Status FROM Sales WHERE Id=@id", Database.P("@id", saleId)));
                if (status != "Finalisee") throw new InvalidOperationException("Cette vente n'est pas annulable.");
                var number = Convert.ToString(Database.Scalar(cn, tx, "SELECT Number FROM Sales WHERE Id=@id", Database.P("@id", saleId)));
                var details = new DataTable();
                using (var cmd = new SqlCommand("SELECT ProductId, Quantity FROM SaleDetails WHERE SaleId=@id", cn, tx))
                {
                    Database.AddParameters(cmd, Database.P("@id", saleId));
                    using (var da = new SqlDataAdapter(cmd)) da.Fill(details);
                }
                foreach (DataRow r in details.Rows)
                {
                    var pid = Convert.ToInt32(r["ProductId"]);
                    var q = Convert.ToInt32(r["Quantity"]);
                    Database.Execute(cn, tx, "UPDATE Products SET Quantity = Quantity + @q WHERE Id=@id", Database.P("@q", q), Database.P("@id", pid));
                    Database.Execute(cn, tx, @"
INSERT INTO StockMovements (ProductId, Type, Quantity, Reason, UserId, RelatedDocument)
VALUES (@p,N'Entrée',@q,@r,@u,@n)",
                        Database.P("@p", pid), Database.P("@q", q), Database.P("@r", "Annulation vente: " + reason),
                        Database.P("@u", userId), Database.P("@n", number));
                }
                Database.Execute(cn, tx, "UPDATE Sales SET Status=N'Annulee', CancelReason=@r WHERE Id=@id",
                    Database.P("@r", reason), Database.P("@id", saleId));
                return 0;
            });
        }

        public static int CreatePurchase(int supplierId, decimal discount, List<LigneDocument> lines, int userId)
        {
            return Database.InTransaction((cn, tx) =>
            {
                decimal total = 0;
                foreach (var l in lines) total += l.LineTotal;
                total = Math.Round(total - discount, 2);
                var number = DocumentNumbers.Next(cn, tx, "Purchases", "ACH");
                var id = Convert.ToInt32(Database.Scalar(cn, tx, @"
INSERT INTO Purchases (Number, SupplierId, Discount, Total, Status, CreatedBy)
VALUES (@n,@s,@d,@t,N'Brouillon',@u); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    Database.P("@n", number), Database.P("@s", supplierId), Database.P("@d", discount),
                    Database.P("@t", total), Database.P("@u", userId)));
                foreach (var l in lines)
                {
                    Database.Execute(cn, tx, @"
INSERT INTO PurchaseDetails (PurchaseId, ProductId, Quantity, UnitPrice, Discount, LineTotal)
VALUES (@id,@p,@q,@u,@d,@l)",
                        Database.P("@id", id), Database.P("@p", l.ProductId), Database.P("@q", l.Quantity),
                        Database.P("@u", l.UnitPrice), Database.P("@d", l.Discount), Database.P("@l", l.LineTotal));
                }
                return id;
            });
        }

        public static void ReceivePurchase(int purchaseId, int userId)
        {
            Database.InTransaction((cn, tx) =>
            {
                var status = Convert.ToString(Database.Scalar(cn, tx, "SELECT Status FROM Purchases WHERE Id=@id", Database.P("@id", purchaseId)));
                if (status == "Recu") throw new InvalidOperationException("Cet achat a déjà été réceptionné.");
                var number = Convert.ToString(Database.Scalar(cn, tx, "SELECT Number FROM Purchases WHERE Id=@id", Database.P("@id", purchaseId)));
                var details = new DataTable();
                using (var cmd = new SqlCommand("SELECT ProductId, Quantity FROM PurchaseDetails WHERE PurchaseId=@id", cn, tx))
                {
                    Database.AddParameters(cmd, Database.P("@id", purchaseId));
                    using (var da = new SqlDataAdapter(cmd)) da.Fill(details);
                }
                foreach (DataRow r in details.Rows)
                {
                    var pid = Convert.ToInt32(r["ProductId"]);
                    var q = Convert.ToInt32(r["Quantity"]);
                    Database.Execute(cn, tx, "UPDATE Products SET Quantity = Quantity + @q WHERE Id=@id", Database.P("@q", q), Database.P("@id", pid));
                    Database.Execute(cn, tx, @"
INSERT INTO StockMovements (ProductId, Type, Quantity, Reason, UserId, RelatedDocument)
VALUES (@p,N'Entrée',@q,N'Réception achat',@u,@n)",
                        Database.P("@p", pid), Database.P("@q", q), Database.P("@u", userId), Database.P("@n", number));
                }
                Database.Execute(cn, tx, "UPDATE Purchases SET Status=N'Recu', ReceivedAt=SYSUTCDATETIME() WHERE Id=@id", Database.P("@id", purchaseId));
                return 0;
            });
        }

        public static void AdjustStock(int productId, int delta, string reason, int userId)
        {
            Database.InTransaction((cn, tx) =>
            {
                var qty = Convert.ToInt32(Database.Scalar(cn, tx, "SELECT Quantity FROM Products WHERE Id=@id", Database.P("@id", productId)));
                var next = qty + delta;
                if (next < 0) throw new InvalidOperationException("Le stock ne peut pas devenir négatif.");
                Database.Execute(cn, tx, "UPDATE Products SET Quantity=@q WHERE Id=@id", Database.P("@q", next), Database.P("@id", productId));
                Database.Execute(cn, tx, @"
INSERT INTO StockMovements (ProductId, Type, Quantity, Reason, UserId, RelatedDocument)
VALUES (@p,@t,@q,@r,@u,N'Ajustement')",
                    Database.P("@p", productId),
                    Database.P("@t", delta >= 0 ? "Entrée" : "Sortie"),
                    Database.P("@q", delta),
                    Database.P("@r", reason),
                    Database.P("@u", userId));
                return 0;
            });
        }

        public static int CreateQuote(int? clientId, decimal discount, List<LigneDocument> lines)
        {
            return Database.InTransaction((cn, tx) =>
            {
                decimal total = 0;
                foreach (var l in lines) total += l.LineTotal;
                total = Math.Round(total - discount, 2);
                var number = DocumentNumbers.Next(cn, tx, "Quotes", "DEV");
                var id = Convert.ToInt32(Database.Scalar(cn, tx, @"
INSERT INTO Quotes (Number, ClientId, Discount, Total, Status)
VALUES (@n,@c,@d,@t,N'Ouvert'); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    Database.P("@n", number), Database.P("@c", (object)clientId ?? DBNull.Value),
                    Database.P("@d", discount), Database.P("@t", total)));
                foreach (var l in lines)
                    Database.Execute(cn, tx, @"
INSERT INTO QuoteDetails (QuoteId, ProductId, Quantity, UnitPrice, Discount, LineTotal)
VALUES (@id,@p,@q,@u,@d,@l)",
                        Database.P("@id", id), Database.P("@p", l.ProductId), Database.P("@q", l.Quantity),
                        Database.P("@u", l.UnitPrice), Database.P("@d", l.Discount), Database.P("@l", l.LineTotal));
                return id;
            });
        }

        public static int ConvertQuoteToSale(int quoteId, int userId, string payment)
        {
            var lines = GetQuoteLines(quoteId);
            var quote = Database.Query("SELECT * FROM Quotes WHERE Id=@id", Database.P("@id", quoteId)).Rows[0];
            if (quote["Status"].ToString() == "Converti")
                throw new InvalidOperationException("Ce devis a déjà été converti.");
            int? clientId = quote["ClientId"] == DBNull.Value ? (int?)null : Convert.ToInt32(quote["ClientId"]);
            var discount = Convert.ToDecimal(quote["Discount"]);
            decimal total = 0;
            foreach (var l in lines) total += l.LineTotal;
            total = Math.Round(total - discount, 2);
            var saleId = CreateSale(clientId, userId, discount, total, total, 0, payment, lines);
            Database.Execute("UPDATE Quotes SET Status=N'Converti', ConvertedSaleId=@s WHERE Id=@id",
                Database.P("@s", saleId), Database.P("@id", quoteId));
            return saleId;
        }

        public static int CreateDeliveryFromSale(int saleId)
        {
            return Database.InTransaction((cn, tx) =>
            {
                var exists = Convert.ToInt32(Database.Scalar(cn, tx, "SELECT COUNT(*) FROM DeliveryNotes WHERE SaleId=@id", Database.P("@id", saleId)));
                if (exists > 0) throw new InvalidOperationException("Un bon de livraison existe déjà pour cette vente. Le stock a déjà été déduit à la vente.");
                var sale = new DataTable();
                using (var cmd = new SqlCommand("SELECT * FROM Sales WHERE Id=@id", cn, tx))
                {
                    Database.AddParameters(cmd, Database.P("@id", saleId));
                    using (var da = new SqlDataAdapter(cmd)) da.Fill(sale);
                }
                if (sale.Rows.Count == 0) throw new InvalidOperationException("Vente introuvable.");
                var number = DocumentNumbers.Next(cn, tx, "DeliveryNotes", "FAC");
                var clientId = sale.Rows[0]["ClientId"];
                var total = sale.Rows[0]["Total"];
                var blId = Convert.ToInt32(Database.Scalar(cn, tx, @"
INSERT INTO DeliveryNotes (Number, ClientId, SaleId, Total, Status, StockApplied)
VALUES (@n,@c,@s,@t,N'Livre',0); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    Database.P("@n", number), Database.P("@c", clientId), Database.P("@s", saleId), Database.P("@t", total)));
                var details = new DataTable();
                using (var cmd = new SqlCommand("SELECT * FROM SaleDetails WHERE SaleId=@id", cn, tx))
                {
                    Database.AddParameters(cmd, Database.P("@id", saleId));
                    using (var da = new SqlDataAdapter(cmd)) da.Fill(details);
                }
                foreach (DataRow r in details.Rows)
                {
                    Database.Execute(cn, tx, @"
INSERT INTO DeliveryNoteDetails (DeliveryNoteId, ProductId, Quantity, UnitPrice, LineTotal)
VALUES (@d,@p,@q,@u,@l)",
                        Database.P("@d", blId), Database.P("@p", r["ProductId"]), Database.P("@q", r["Quantity"]),
                        Database.P("@u", r["UnitPrice"]), Database.P("@l", r["LineTotal"]));
                }
                return blId;
            });
        }

        public static int CreateDirectDelivery(int? clientId, List<LigneDocument> lines, int userId)
        {
            return Database.InTransaction((cn, tx) =>
            {
                foreach (var line in lines)
                {
                    var qty = Convert.ToInt32(Database.Scalar(cn, tx, "SELECT Quantity FROM Products WHERE Id=@id", Database.P("@id", line.ProductId)));
                    if (qty < line.Quantity) throw new InvalidOperationException("Stock insuffisant pour cette quantité.");
                }
                decimal total = 0;
                foreach (var l in lines) total += l.LineTotal;
                var number = DocumentNumbers.Next(cn, tx, "DeliveryNotes", "FAC");
                var id = Convert.ToInt32(Database.Scalar(cn, tx, @"
INSERT INTO DeliveryNotes (Number, ClientId, Total, Status, StockApplied)
VALUES (@n,@c,@t,N'Livre',1); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    Database.P("@n", number), Database.P("@c", (object)clientId ?? DBNull.Value), Database.P("@t", total)));
                foreach (var l in lines)
                {
                    Database.Execute(cn, tx, @"
INSERT INTO DeliveryNoteDetails (DeliveryNoteId, ProductId, Quantity, UnitPrice, LineTotal)
VALUES (@id,@p,@q,@u,@l)",
                        Database.P("@id", id), Database.P("@p", l.ProductId), Database.P("@q", l.Quantity),
                        Database.P("@u", l.UnitPrice), Database.P("@l", l.LineTotal));
                    Database.Execute(cn, tx, "UPDATE Products SET Quantity = Quantity - @q WHERE Id=@id",
                        Database.P("@q", l.Quantity), Database.P("@id", l.ProductId));
                    Database.Execute(cn, tx, @"
INSERT INTO StockMovements (ProductId, Type, Quantity, Reason, UserId, RelatedDocument)
VALUES (@p,N'Sortie',@q,N'Facture',@u,@n)",
                        Database.P("@p", l.ProductId), Database.P("@q", -l.Quantity), Database.P("@u", userId), Database.P("@n", number));
                }
                return id;
            });
        }

        public static List<LigneDocument> GetSaleLines(int saleId) => GetLines("SaleDetails", "SaleId", saleId);
        public static List<LigneDocument> GetQuoteLines(int quoteId) => GetLines("QuoteDetails", "QuoteId", quoteId);
        public static List<LigneDocument> GetPurchaseLines(int id) => GetLines("PurchaseDetails", "PurchaseId", id);
        public static List<LigneDocument> GetDeliveryLines(int id) => GetLines("DeliveryNoteDetails", "DeliveryNoteId", id);

        private static List<LigneDocument> GetLines(string table, string fk, int id)
        {
            var sql = "SELECT d.*, p.Designation FROM " + table + " d INNER JOIN Products p ON p.Id=d.ProductId WHERE d." + fk + "=@id";
            var list = new List<LigneDocument>();
            foreach (DataRow r in Database.Query(sql, Database.P("@id", id)).Rows)
            {
                list.Add(new LigneDocument
                {
                    ProductId = Convert.ToInt32(r["ProductId"]),
                    Designation = r["Designation"].ToString(),
                    Quantity = Convert.ToInt32(r["Quantity"]),
                    UnitPrice = Convert.ToDecimal(r["UnitPrice"]),
                    Discount = r.Table.Columns.Contains("Discount") && r["Discount"] != DBNull.Value ? Convert.ToDecimal(r["Discount"]) : 0
                });
            }
            return list;
        }
    }
}
