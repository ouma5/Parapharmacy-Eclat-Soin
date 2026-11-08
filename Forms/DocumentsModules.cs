using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EclatEtSoin.Data;
using EclatEtSoin.Helpers;
using EclatEtSoin.Models;
using EclatEtSoin.Services;
using EclatEtSoin.Theme;

namespace EclatEtSoin.Forms
{
    public class QuotesModule : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly ComboBox _client = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.BodyFont, Width = 200 };
        private readonly ComboBox _product = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.BodyFont, Width = 220 };
        private readonly NumericUpDown _qty = new NumericUpDown { Minimum = 1, Maximum = 100000, Value = 1 };
        private readonly List<LigneDocument> _lines = new List<LigneDocument>();
        private readonly DataGridView _cart = new DataGridView();

        public QuotesModule()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            AppTheme.ApplyGrid(_grid); AppTheme.ApplyGrid(_cart);
            var tabs = new TabControl { Dock = DockStyle.Fill };
            var create = new TabPage("Nouveau devis");
            var hist = new TabPage("Devis enregistrés");
            _cart.Dock = DockStyle.Fill;
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48 };
            var add = AppTheme.SecondaryButton("Ajouter");
            var save = AppTheme.PrimaryButton("Enregistrer");
            add.Click += (s, e) =>
            {
                var p = _product.SelectedItem as Produit;
                if (p == null) return;
                _lines.Add(new LigneDocument { ProductId = p.Id, Designation = p.Designation, Quantity = (int)_qty.Value, UnitPrice = p.SalePrice });
                _cart.DataSource = null;
                _cart.DataSource = _lines.Select(l => new { l.Designation, l.Quantity, l.UnitPrice, Total = l.LineTotal }).ToList();
            };
            save.Click += (s, e) => Save();
            top.Controls.AddRange(new Control[] { _client, _product, _qty, add, save });
            create.Controls.Add(_cart); create.Controls.Add(top);

            _grid.Dock = DockStyle.Fill;
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44 };
            var print = AppTheme.SecondaryButton("Imprimer");
            var conv = AppTheme.PrimaryButton("Convertir en vente");
            print.Click += (s, e) => Print();
            conv.Click += (s, e) => ConvertSale();
            bar.Controls.AddRange(new Control[] { print, conv });
            hist.Controls.Add(_grid); hist.Controls.Add(bar);
            tabs.TabPages.Add(create); tabs.TabPages.Add(hist);
            Controls.Add(tabs);
            Load += (s, e) => Fill();
        }

        private void Fill()
        {
            _client.Items.Clear();
            foreach (var c in ClientRepository.GetAll()) _client.Items.Add(c);
            _client.DisplayMember = "FullName";
            if (_client.Items.Count > 0) _client.SelectedIndex = 0;
            _product.Items.Clear();
            foreach (var p in ProductRepository.GetAll(true)) _product.Items.Add(p);
            _product.DisplayMember = "Designation";
            Bind();
        }

        private void Bind()
        {
            _grid.DataSource = Database.Query(@"
SELECT q.Id, q.Number, q.QuoteDate, ISNULL(c.FullName,N'Comptoir') AS Client, q.Total, q.Status
FROM Quotes q LEFT JOIN Clients c ON c.Id=q.ClientId ORDER BY q.QuoteDate DESC");
        }

        private void Save()
        {
            if (_lines.Count == 0) { UiMessages.Warn("Ajoutez des produits."); return; }
            var c = _client.SelectedItem as Client;
            try
            {
                CommerceRepository.CreateQuote(c == null ? (int?)null : c.Id, 0, _lines);
                UiMessages.Info("Devis enregistré. Le stock n'a pas été modifié.");
                _lines.Clear(); _cart.DataSource = null; Bind();
            }
            catch (Exception ex) { UiMessages.Error("Une erreur est survenue lors de l'enregistrement.", ex); }
        }

        private int? Id()
        {
            if (_grid.CurrentRow == null) return null;
            return Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
        }

        private void Print()
        {
            var id = Id(); if (id == null) return;
            var q = Database.Query(@"SELECT q.*, ISNULL(c.FullName,N'Comptoir') AS ClientName FROM Quotes q LEFT JOIN Clients c ON c.Id=q.ClientId WHERE q.Id=@id", Database.P("@id", id.Value)).Rows[0];
            PrintHelper.PreviewDocument("Devis", q["Number"].ToString(), Convert.ToDateTime(q["QuoteDate"]), q["ClientName"].ToString(),
                CommerceRepository.GetQuoteLines(id.Value), Convert.ToDecimal(q["Discount"]), Convert.ToDecimal(q["Total"]), false);
        }

        private void ConvertSale()
        {
            var id = Id(); if (id == null) return;
            try
            {
                var saleId = CommerceRepository.ConvertQuoteToSale(id.Value, Session.UserId, "Espèces");
                UiMessages.Info("Devis converti en vente n° interne " + saleId + ".");
                Bind();
            }
            catch (Exception ex) { UiMessages.Error("Conversion impossible.", ex); }
        }
    }

    public class DeliveryModule : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly ComboBox _client = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Font = AppTheme.BodyFont };
        private readonly ComboBox _product = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, Font = AppTheme.BodyFont };
        private readonly NumericUpDown _qty = new NumericUpDown { Minimum = 1, Maximum = 100000, Value = 1 };
        private readonly List<LigneDocument> _lines = new List<LigneDocument>();
        private readonly DataGridView _cart = new DataGridView();

        public DeliveryModule()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            AppTheme.ApplyGrid(_grid); AppTheme.ApplyGrid(_cart);
            var tabs = new TabControl { Dock = DockStyle.Fill };
            var create = new TabPage("Nouvelle facture");
            var hist = new TabPage("Factures enregistrées");
            _cart.Dock = DockStyle.Fill;
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48 };
            var add = AppTheme.SecondaryButton("Ajouter");
            var save = AppTheme.PrimaryButton("Émettre la facture");
            add.Click += (s, e) =>
            {
                var p = _product.SelectedItem as Produit;
                if (p == null) return;
                _lines.Add(new LigneDocument { ProductId = p.Id, Designation = p.Designation, Quantity = (int)_qty.Value, UnitPrice = p.SalePrice });
                _cart.DataSource = null;
                _cart.DataSource = _lines.Select(l => new { l.Designation, l.Quantity, Total = l.LineTotal }).ToList();
            };
            save.Click += (s, e) => Save();
            top.Controls.AddRange(new Control[] { _client, _product, _qty, add, save });
            create.Controls.Add(_cart); create.Controls.Add(top);
            _grid.Dock = DockStyle.Fill;
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44 };
            var print = AppTheme.PrimaryButton("Imprimer la facture A4");
            print.Click += (s, e) => Print();
            bar.Controls.Add(print);
            hist.Controls.Add(_grid); hist.Controls.Add(bar);
            tabs.TabPages.Add(create); tabs.TabPages.Add(hist);
            Controls.Add(tabs);
            Load += (s, e) => Fill();
        }

        private void Fill()
        {
            _client.Items.Clear();
            foreach (var c in ClientRepository.GetAll()) _client.Items.Add(c);
            _client.DisplayMember = "FullName";
            _product.Items.Clear();
            foreach (var p in ProductRepository.GetAll(true)) _product.Items.Add(p);
            _product.DisplayMember = "Designation";
            _grid.DataSource = Database.Query(@"
SELECT d.Id, d.Number, d.DeliveryDate, ISNULL(c.FullName,N'Comptoir') AS Client, d.Total, d.Status, d.StockApplied, d.SaleId
FROM DeliveryNotes d LEFT JOIN Clients c ON c.Id=d.ClientId ORDER BY d.DeliveryDate DESC");
        }

        private void Save()
        {
            if (_lines.Count == 0) { UiMessages.Warn("Ajoutez des articles."); return; }
            var c = _client.SelectedItem as Client;
            try
            {
                var id = CommerceRepository.CreateDirectDelivery(c == null ? (int?)null : c.Id, _lines, Session.UserId);
                UiMessages.Info("Facture enregistrée. Stock déduit une seule fois.");
                _lines.Clear(); Fill();
                PrintHelper.PreviewDeliveryInvoice(id);
            }
            catch (Exception ex) { UiMessages.Error("Une erreur est survenue lors de l'enregistrement.", ex); }
        }

        private void Print()
        {
            if (_grid.CurrentRow == null) return;
            var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            PrintHelper.PreviewDeliveryInvoice(id);
        }
    }

    public class ReportsModule : UserControl
    {
        public ReportsModule()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            var from = new DateTimePicker { Value = DateTime.Today.AddDays(-30) };
            var to = new DateTimePicker { Value = DateTime.Today };
            var grid = new DataGridView { Dock = DockStyle.Fill };
            AppTheme.ApplyGrid(grid);
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48 };
            void Show(System.Data.DataTable t) { grid.DataSource = t; if (t.Rows.Count == 0) UiMessages.Info("Aucun résultat trouvé."); }
            var b1 = AppTheme.SecondaryButton("Ventes période"); b1.Click += (s, e) => Show(ReportService.SalesPeriod(from.Value, to.Value));
            var b2 = AppTheme.SecondaryButton("Stock"); b2.Click += (s, e) => Show(ReportService.StockState());
            var b3 = AppTheme.SecondaryButton("Top produits"); b3.Click += (s, e) => Show(ReportService.TopProducts());
            var b4 = AppTheme.SecondaryButton("Rupture / expiration");
            b4.Click += (s, e) => Show(Database.Query(@"
SELECT Designation, Quantity, MinStock, ExpirationDate FROM Products
WHERE IsActive=1 AND (Quantity<=0 OR (ExpirationDate IS NOT NULL AND ExpirationDate<=DATEADD(DAY,60,GETDATE())))"));
            var b5 = AppTheme.SecondaryButton("Achats");
            b5.Click += (s, e) => Show(Database.Query("SELECT Number, PurchaseDate, Total, Status FROM Purchases ORDER BY PurchaseDate DESC"));
            var b6 = AppTheme.SecondaryButton("Par vendeur");
            b6.Click += (s, e) => Show(Database.Query(@"
SELECT u.FullName, COUNT(*) AS Ventes, SUM(s.Total) AS CA
FROM Sales s INNER JOIN Users u ON u.Id=s.UserId
WHERE s.Status=N'Finalisee' GROUP BY u.FullName"));
            var print = AppTheme.SecondaryButton("Aperçu impression");
            print.Click += (s, e) =>
            {
                var doc = new System.Drawing.Printing.PrintDocument();
                doc.PrintPage += (ps, pe) => pe.Graphics.DrawString("Rapport Èclat & Soin — " + DateTime.Now, AppTheme.HeaderFont, System.Drawing.Brushes.Black, 40, 40);
                using (var d = new PrintPreviewDialog { Document = doc, Width = 800, Height = 600 }) d.ShowDialog();
            };
            top.Controls.AddRange(new Control[] { from, to, b1, b2, b3, b4, b5, b6, print });
            Controls.Add(grid); Controls.Add(top);
        }
    }
}
