using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EclatEtSoin.Data;
using EclatEtSoin.Helpers;
using EclatEtSoin.Models;
using EclatEtSoin.Theme;

namespace EclatEtSoin.Forms
{
    public class PurchasesModule : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly ComboBox _supplier = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.BodyFont, Width = 220 };
        private readonly ComboBox _product = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.BodyFont, Width = 240 };
        private readonly NumericUpDown _qty = new NumericUpDown { Minimum = 1, Maximum = 100000, Value = 1, Font = AppTheme.BodyFont };
        private readonly NumericUpDown _price = new NumericUpDown { DecimalPlaces = 2, Maximum = 1000000, Font = AppTheme.BodyFont };
        private readonly List<LigneDocument> _lines = new List<LigneDocument>();
        private readonly DataGridView _cart = new DataGridView();

        public PurchasesModule()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            AppTheme.ApplyGrid(_grid); AppTheme.ApplyGrid(_cart);
            var tabs = new TabControl { Dock = DockStyle.Fill };
            var list = new TabPage("Historique");
            var create = new TabPage("Nouvel achat");
            _grid.Dock = DockStyle.Fill;
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44 };
            var rec = AppTheme.PrimaryButton("Valider la réception");
            var print = AppTheme.SecondaryButton("Imprimer");
            rec.Click += (s, e) => Receive();
            print.Click += (s, e) => Print();
            bar.Controls.AddRange(new Control[] { rec, print });
            list.Controls.Add(_grid); list.Controls.Add(bar);

            _cart.Dock = DockStyle.Fill;
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48 };
            var add = AppTheme.SecondaryButton("Ajouter ligne");
            var save = AppTheme.PrimaryButton("Enregistrer brouillon");
            add.Click += (s, e) => AddLine();
            save.Click += (s, e) => Save();
            top.Controls.AddRange(new Control[] { AppTheme.Label("Fournisseur"), _supplier, _product, AppTheme.Label("Qté"), _qty, AppTheme.Label("Prix"), _price, add, save });
            create.Controls.Add(_cart); create.Controls.Add(top);
            tabs.TabPages.Add(create); tabs.TabPages.Add(list);
            Controls.Add(tabs);
            Load += (s, e) => { Fill(); Bind(); };
        }

        private void Fill()
        {
            _supplier.Items.Clear();
            foreach (var s in SupplierRepository.GetAll()) _supplier.Items.Add(s);
            _supplier.DisplayMember = "Name";
            if (_supplier.Items.Count > 0) _supplier.SelectedIndex = 0;
            _product.Items.Clear();
            foreach (var p in ProductRepository.GetAll(true)) _product.Items.Add(p);
            _product.DisplayMember = "Designation";
            if (_product.Items.Count > 0) _product.SelectedIndex = 0;
        }

        private void Bind()
        {
            _grid.DataSource = Database.Query(@"
SELECT p.Id, p.Number, s.Name AS Fournisseur, p.PurchaseDate, p.Total, p.Status
FROM Purchases p INNER JOIN Suppliers s ON s.Id=p.SupplierId ORDER BY p.PurchaseDate DESC");
        }

        private void AddLine()
        {
            var p = _product.SelectedItem as Produit;
            if (p == null) return;
            _lines.Add(new LigneDocument { ProductId = p.Id, Designation = p.Designation, Quantity = (int)_qty.Value, UnitPrice = _price.Value == 0 ? p.PurchasePrice : _price.Value });
            _cart.DataSource = null;
            _cart.DataSource = _lines.Select(l => new { l.Designation, l.Quantity, l.UnitPrice, Total = l.LineTotal }).ToList();
        }

        private void Save()
        {
            var s = _supplier.SelectedItem as Fournisseur;
            if (s == null) { UiMessages.Warn("Sélectionnez un fournisseur."); return; }
            if (_lines.Count == 0) { UiMessages.Warn("Ajoutez au moins un produit."); return; }
            try
            {
                CommerceRepository.CreatePurchase(s.Id, 0, _lines, Session.UserId);
                UiMessages.Info("Achat enregistré en brouillon. Validez la réception pour mettre à jour le stock.");
                _lines.Clear(); _cart.DataSource = null; Bind();
            }
            catch (Exception ex) { UiMessages.Error("Une erreur est survenue lors de l'enregistrement.", ex); }
        }

        private int? CurrentId()
        {
            if (_grid.CurrentRow == null) return null;
            return Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
        }

        private void Receive()
        {
            var id = CurrentId();
            if (id == null) return;
            if (!UiMessages.Confirm("Réceptionner cet achat et augmenter le stock une seule fois ?")) return;
            try
            {
                CommerceRepository.ReceivePurchase(id.Value, Session.UserId);
                UiMessages.Info("Réception validée. Stock mis à jour.");
                Bind();
            }
            catch (Exception ex) { UiMessages.Error("Réception impossible.", ex); }
        }

        private void Print()
        {
            var id = CurrentId();
            if (id == null) return;
            var p = Database.Query("SELECT p.*, s.Name FROM Purchases p INNER JOIN Suppliers s ON s.Id=p.SupplierId WHERE p.Id=@id", Database.P("@id", id.Value)).Rows[0];
            PrintHelper.PreviewDocument("Bon de réception", p["Number"].ToString(), Convert.ToDateTime(p["PurchaseDate"]), p["Name"].ToString(),
                CommerceRepository.GetPurchaseLines(id.Value), Convert.ToDecimal(p["Discount"]), Convert.ToDecimal(p["Total"]), false);
        }
    }

    public class StockModule : UserControl
    {
        private readonly DataGridView _prod = new DataGridView();
        private readonly DataGridView _mov = new DataGridView();
        private readonly NumericUpDown _delta = new NumericUpDown { Minimum = -100000, Maximum = 100000, Font = AppTheme.BodyFont };
        private readonly TextBox _reason = AppTheme.Input();

        public StockModule()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            AppTheme.ApplyGrid(_prod); AppTheme.ApplyGrid(_mov);
            _prod.Dock = DockStyle.Fill;
            _mov.Dock = DockStyle.Bottom; _mov.Height = 220;
            _prod.SelectionChanged += (s, e) => LoadMov();
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48 };
            var adj = AppTheme.PrimaryButton("Ajuster");
            adj.Click += (s, e) => Adjust();
            _reason.Width = 260;
            top.Controls.AddRange(new Control[] { AppTheme.Label("Écart"), _delta, AppTheme.Label("Motif"), _reason, adj });
            Controls.Add(_prod); Controls.Add(_mov); Controls.Add(top);
            Load += (s, e) => Bind();
        }

        private void Bind()
        {
            _prod.DataSource = Database.Query(@"
SELECT Id, Designation, Quantity, MinStock, ExpirationDate,
 CASE WHEN Quantity<=0 THEN N'Rupture' WHEN Quantity<=MinStock THEN N'Stock faible' WHEN ExpirationDate IS NOT NULL AND ExpirationDate<=DATEADD(DAY,60,GETDATE()) THEN N'Expiration' ELSE N'OK' END AS Alerte
FROM Products WHERE IsActive=1 ORDER BY Designation");
        }

        private int? Pid()
        {
            if (_prod.CurrentRow == null) return null;
            return Convert.ToInt32(_prod.CurrentRow.Cells["Id"].Value);
        }

        private void LoadMov()
        {
            var id = Pid();
            if (id == null) return;
            _mov.DataSource = Database.Query(@"
SELECT MovementDate, Type, Quantity, Reason, RelatedDocument FROM StockMovements WHERE ProductId=@id ORDER BY MovementDate DESC",
                Database.P("@id", id.Value));
        }

        private void Adjust()
        {
            var id = Pid();
            if (id == null) return;
            if (string.IsNullOrWhiteSpace(_reason.Text)) { UiMessages.Warn("Indiquez le motif de l'ajustement."); return; }
            if (!UiMessages.Confirm("Enregistrer cet ajustement de stock ?")) return;
            try
            {
                CommerceRepository.AdjustStock(id.Value, (int)_delta.Value, _reason.Text.Trim(), Session.UserId);
                UiMessages.Info("Mouvement enregistré.");
                Bind(); LoadMov();
            }
            catch (Exception ex) { UiMessages.Error("Ajustement impossible.", ex); }
        }
    }
}
