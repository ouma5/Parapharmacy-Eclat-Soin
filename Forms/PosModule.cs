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
    public class PosModule : UserControl
    {
        private readonly TextBox _search = AppTheme.Input();
        private readonly TextBox _barcode = AppTheme.Input();
        private readonly DataGridView _cart = new DataGridView();
        private readonly ComboBox _client = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.BodyFont, Width = 220 };
        private readonly ComboBox _pay = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.BodyFont, Width = 160 };
        private readonly NumericUpDown _received = new NumericUpDown { DecimalPlaces = 2, Maximum = 1000000, Font = AppTheme.BodyFont, Width = 140 };
        private readonly Label _total = new Label { Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = AppTheme.PrimaryDark, AutoSize = true };
        private readonly Label _change = new Label { Font = AppTheme.HeaderFont, ForeColor = AppTheme.AccentGreen, AutoSize = true };
        private readonly PictureBox _img = new PictureBox { Size = new Size(90, 90), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
        private readonly List<LigneDocument> _lines = new List<LigneDocument>();
        private bool _saving;
        private readonly Button _validate;

        public PosModule()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            AppTheme.ApplyGrid(_cart);
            _cart.ReadOnly = false;
            _cart.Dock = DockStyle.Fill;
            _pay.Items.AddRange(new object[] { "Espèces", "Carte", "Chèque", "Autre" });
            _pay.SelectedIndex = 0;

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 8, 0, 0) };
            _search.Width = 260; _search.Text = "Désignation";
            _barcode.Width = 180;
            var add = AppTheme.PrimaryButton("Ajouter");
            add.Click += (s, e) => AddFromSearch();
            _barcode.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { AddBarcode(); e.SuppressKeyPress = true; } };
            top.Controls.AddRange(new Control[] { AppTheme.Label("Recherche"), _search, AppTheme.Label("Code-barres"), _barcode, add, _img });

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 90, BackColor = AppTheme.Surface };
            _validate = AppTheme.PrimaryButton("Valider la vente");
            var cancel = AppTheme.SecondaryButton("Annuler");
            var fresh = AppTheme.SecondaryButton("Nouvelle vente");
            var print = AppTheme.SecondaryButton("Imprimer la facture");
            _validate.SetBounds(20, 40, 180, 40);
            cancel.SetBounds(210, 40, 110, 40);
            fresh.SetBounds(330, 40, 140, 40);
            print.SetBounds(480, 40, 160, 40);
            _client.Location = new Point(20, 8);
            _pay.Location = new Point(250, 8);
            _received.Location = new Point(430, 8);
            _total.Location = new Point(620, 8);
            _change.Location = new Point(620, 48);
            _received.ValueChanged += (s, e) => Recalc();
            _validate.Click += (s, e) => ValidateSale();
            cancel.Click += (s, e) => { if (UiMessages.Confirm("Annuler le panier ?")) Reset(); };
            fresh.Click += (s, e) => Reset();
            print.Click += (s, e) => UiMessages.Warn("Imprimez après validation : la facture A4 reprend la vente enregistrée.");
            bottom.Controls.AddRange(new Control[] { _client, _pay, _received, _total, _change, _validate, cancel, fresh, print });

            _cart.CellEndEdit += (s, e) =>
            {
                if (_cart.CurrentRow == null) return;
                var i = _cart.CurrentRow.Index;
                if (i < 0 || i >= _lines.Count) return;
                int q; decimal d;
                int.TryParse(Convert.ToString(_cart.CurrentRow.Cells["Qté"].Value), out q);
                decimal.TryParse(Convert.ToString(_cart.CurrentRow.Cells["Remise"].Value), out d);
                _lines[i].Quantity = Math.Max(1, q);
                _lines[i].Discount = Math.Max(0, d);
                BindCart();
            };

            Controls.Add(_cart);
            Controls.Add(bottom);
            Controls.Add(top);
            Load += (s, e) => { FillClients(); Reset(); };
        }

        private void FillClients()
        {
            _client.Items.Clear();
            foreach (var c in ClientRepository.GetAll()) _client.Items.Add(c);
            _client.DisplayMember = "FullName";
            if (_client.Items.Count > 0) _client.SelectedIndex = 0;
        }

        private void AddFromSearch()
        {
            var list = ProductRepository.Search(_search.Text.Trim(), null).Where(p => p.IsActive).ToList();
            if (list.Count == 0) { UiMessages.Info("Aucun résultat trouvé."); return; }
            AddProduct(list[0]);
        }

        private void AddBarcode()
        {
            var p = ProductRepository.GetByBarcode(_barcode.Text.Trim());
            if (p == null) { UiMessages.Warn("Aucun résultat trouvé."); return; }
            AddProduct(p);
            _barcode.Clear();
        }

        private void AddProduct(Produit p)
        {
            var existing = _lines.FirstOrDefault(x => x.ProductId == p.Id);
            if (existing != null) existing.Quantity++;
            else _lines.Add(new LigneDocument { ProductId = p.Id, Designation = p.Designation, Quantity = 1, UnitPrice = p.SalePrice, Discount = p.Discount });
            if (!string.IsNullOrEmpty(p.ImagePath)) _img.ImageLocation = p.ImagePath;
            BindCart();
        }

        private void BindCart()
        {
            _cart.DataSource = null;
            _cart.DataSource = _lines.Select(l => new { Produit = l.Designation, Qté = l.Quantity, Prix = l.UnitPrice, Remise = l.Discount, Total = l.LineTotal }).ToList();
            Recalc();
        }

        private void Recalc()
        {
            decimal t = _lines.Sum(l => l.LineTotal);
            _total.Text = "Total " + t.ToString("N2") + " DH";
            var change = _received.Value - t;
            _change.Text = "À rendre : " + Math.Max(0, change).ToString("N2") + " DH";
        }

        private void Reset()
        {
            _lines.Clear();
            BindCart();
            _received.Value = 0;
            _img.Image = null;
        }

        private void ValidateSale()
        {
            if (_saving) return;
            if (_lines.Count == 0) { UiMessages.Warn("Le panier est vide."); return; }
            _saving = true;
            _validate.Enabled = false;
            try
            {
                var client = _client.SelectedItem as Client;
                decimal total = _lines.Sum(l => l.LineTotal);
                var received = _received.Value;
                if (_pay.Text == "Espèces" && received < total)
                {
                    UiMessages.Warn("Le montant reçu est insuffisant.");
                    return;
                }
                var change = Math.Max(0, received - total);
                var id = CommerceRepository.CreateSale(client == null ? (int?)null : client.Id, Session.UserId, 0, total, received, change, _pay.Text, _lines.ToList());
                UiMessages.Info("Vente enregistrée avec succès.");
                if (UiMessages.Confirm("Imprimer la facture A4 ?"))
                    PrintHelper.PreviewSaleInvoice(id);
                Reset();
            }
            catch (Exception ex)
            {
                UiMessages.Error("Une erreur est survenue lors de l'enregistrement.", ex);
            }
            finally
            {
                _saving = false;
                _validate.Enabled = true;
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // PosModule
            // 
            this.Name = "PosModule";
            this.Load += new System.EventHandler(this.PosModule_Load);
            this.ResumeLayout(false);

        }

        private void PosModule_Load(object sender, EventArgs e)
        {

        }
    }
}
