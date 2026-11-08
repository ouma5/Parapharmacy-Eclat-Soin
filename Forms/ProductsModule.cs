using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EclatEtSoin.Data;
using EclatEtSoin.Helpers;
using EclatEtSoin.Models;
using EclatEtSoin.Services;
using EclatEtSoin.Theme;

namespace EclatEtSoin.Forms
{
    public class ProductsModule : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _search = AppTheme.Input();
        private readonly ComboBox _cat = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.BodyFont, Width = 180 };
        private Produit _current = new Produit();
        private readonly TextBox _barcode = AppTheme.Input();
        private readonly TextBox _ref = AppTheme.Input();
        private readonly TextBox _name = AppTheme.Input();
        private readonly ComboBox _catEdit = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.BodyFont };
        private readonly TextBox _brand = AppTheme.Input();
        private readonly NumericUpDown _buy = Num();
        private readonly NumericUpDown _sell = Num();
        private readonly NumericUpDown _pph = Num();
        private readonly NumericUpDown _disc = Num();
        private readonly NumericUpDown _qty = new NumericUpDown { Maximum = 100000, Font = AppTheme.BodyFont };
        private readonly NumericUpDown _min = new NumericUpDown { Maximum = 100000, Font = AppTheme.BodyFont, Value = 5 };
        private readonly DateTimePicker _exp = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false };
        private readonly ComboBox _sup = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.BodyFont };
        private readonly PictureBox _img = new PictureBox { Size = new Size(110, 110), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
        private string _imagePath;

        public ProductsModule()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            AppTheme.ApplyGrid(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.CellFormatting += GridFormat;
            _grid.SelectionChanged += (s, e) => LoadSelected();

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(0, 8, 0, 0) };
            _search.Width = 240;
            _search.Text = "Recherche désignation / code-barres";
            var bSearch = AppTheme.SecondaryButton("Rechercher");
            var bRefresh = AppTheme.SecondaryButton("Actualiser");
            var bNew = AppTheme.PrimaryButton("Nouveau");
            bSearch.Click += (s, e) => RefreshGrid();
            bRefresh.Click += (s, e) => { _search.Clear(); RefreshGrid(); };
            bNew.Click += (s, e) => ClearForm();
            top.Controls.AddRange(new Control[] { _search, _cat, bSearch, bRefresh, bNew });

            var form = BuildForm();
            form.Dock = DockStyle.Right;
            form.Width = 360;
            Controls.Add(_grid);
            Controls.Add(form);
            Controls.Add(top);
            Load += (s, e) => { FillLookups(); RefreshGrid(); };
        }

        private static NumericUpDown Num()
        {
            return new NumericUpDown { DecimalPlaces = 2, Maximum = 1000000, Font = AppTheme.BodyFont, Width = 140 };
        }

        private Panel BuildForm()
        {
            var p = AppTheme.Card();
            var y = 8;
            void Add(string cap, Control c)
            {
                var l = AppTheme.Label(cap);
                l.Location = new Point(12, y);
                y += 18;
                c.SetBounds(12, y, 320, 26);
                p.Controls.Add(l);
                p.Controls.Add(c);
                y += 32;
            }
            Add("Code-barres", _barcode);
            var gen = AppTheme.SecondaryButton("Générer");
            gen.SetBounds(210, y - 32, 122, 26);
            gen.Click += (s, e) => _barcode.Text = DateTime.Now.ToString("yyMMddHHmmss");
            p.Controls.Add(gen);
            Add("Référence", _ref);
            Add("Désignation", _name);
            Add("Catégorie", _catEdit);
            Add("Marque", _brand);
            Add("Prix d'achat", _buy);
            Add("Prix de vente", _sell);
            Add("PPH", _pph);
            Add("Remise %", _disc);
            Add("Quantité initiale", _qty);
            Add("Seuil minimal", _min);
            Add("Expiration", _exp);
            Add("Fournisseur", _sup);
            _img.Location = new Point(12, y);
            var imgBtn = AppTheme.SecondaryButton("Image...");
            imgBtn.SetBounds(140, y, 120, 30);
            imgBtn.Click += (s, e) => PickImage();
            p.Controls.Add(_img);
            p.Controls.Add(imgBtn);
            y += 120;
            var save = AppTheme.PrimaryButton("Enregistrer");
            var del = AppTheme.DangerButton("Supprimer");
            save.SetBounds(12, y, 150, 36);
            del.SetBounds(180, y, 150, 36);
            save.Click += (s, e) => Save();
            del.Click += (s, e) => Delete();
            p.Controls.Add(save);
            p.Controls.Add(del);
            return p;
        }

        private void FillLookups()
        {
            _cat.Items.Clear();
            _cat.Items.Add("Toutes les catégories");
            _catEdit.Items.Clear();
            _sup.Items.Clear();
            _sup.Items.Add("(aucun)");
            foreach (var c in CategoryRepository.GetAll())
            {
                _cat.Items.Add(c);
                _catEdit.Items.Add(c);
            }
            _cat.DisplayMember = _catEdit.DisplayMember = "Name";
            _cat.SelectedIndex = 0;
            foreach (var s in SupplierRepository.GetAll()) _sup.Items.Add(s);
            _sup.DisplayMember = "Name";
            _sup.SelectedIndex = 0;
        }

        private void RefreshGrid()
        {
            int? cid = null;
            var cat = _cat.SelectedItem as Categorie;
            if (cat != null) cid = cat.Id;
            var q = _search.Text == "Recherche désignation / code-barres" ? "" : _search.Text;
            _grid.DataSource = ProductRepository.Search(q, cid).Select(p => new
            {
                p.Id,
                p.Barcode,
                p.Reference,
                p.Designation,
                Catégorie = p.CategoryName,
                p.Brand,
                Achat = p.PurchasePrice,
                Vente = p.SalePrice,
                Stock = p.Quantity,
                Seuil = p.MinStock,
                Expiration = p.ExpirationDate,
                Actif = p.IsActive
            }).ToList();
        }

        private void GridFormat(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || !_grid.Columns.Contains("Stock")) return;
            var stock = Convert.ToInt32(_grid.Rows[e.RowIndex].Cells["Stock"].Value ?? 0);
            var seuil = Convert.ToInt32(_grid.Rows[e.RowIndex].Cells["Seuil"].Value ?? 0);
            if (stock <= 0) _grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
            else if (stock <= seuil) _grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 244, 214);
        }

        private void LoadSelected()
        {
            if (_grid.CurrentRow == null) return;
            var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            var p = ProductRepository.GetById(id);
            if (p == null) return;
            _current = p;
            _barcode.Text = p.Barcode;
            _ref.Text = p.Reference;
            _name.Text = p.Designation;
            _brand.Text = p.Brand;
            _buy.Value = p.PurchasePrice;
            _sell.Value = p.SalePrice;
            _pph.Value = p.Pph ?? 0;
            _disc.Value = p.Discount;
            _qty.Value = p.Quantity;
            _qty.Enabled = p.Id == 0;
            _min.Value = p.MinStock;
            _exp.Checked = p.ExpirationDate.HasValue;
            if (p.ExpirationDate.HasValue) _exp.Value = p.ExpirationDate.Value;
            _imagePath = p.ImagePath;
            _img.ImageLocation = File.Exists(p.ImagePath ?? "") ? p.ImagePath : null;
            SelectCombo(_catEdit, p.CategoryId);
            SelectCombo(_sup, p.SupplierId);
        }

        private static void SelectCombo(ComboBox cb, int? id)
        {
            for (int i = 0; i < cb.Items.Count; i++)
            {
                var cat = cb.Items[i] as Categorie;
                if (cat != null && cat.Id == id) { cb.SelectedIndex = i; return; }
                var sup = cb.Items[i] as Fournisseur;
                if (sup != null && sup.Id == id) { cb.SelectedIndex = i; return; }
            }
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }

        private void ClearForm()
        {
            _current = new Produit();
            _barcode.Clear(); _ref.Clear(); _name.Clear(); _brand.Clear();
            _buy.Value = _sell.Value = _pph.Value = _disc.Value = 0;
            _qty.Value = 0; _qty.Enabled = true; _min.Value = 5; _exp.Checked = false;
            _imagePath = null; _img.Image = null;
        }

        private void PickImage()
        {
            using (var dlg = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EclatEtSoin", "Images");
                Directory.CreateDirectory(dir);
                var dest = Path.Combine(dir, Guid.NewGuid().ToString("N") + Path.GetExtension(dlg.FileName));
                File.Copy(dlg.FileName, dest, true);
                _imagePath = dest;
                _img.ImageLocation = dest;
            }
        }

        private void Save()
        {
            try
            {
                _current.Barcode = _barcode.Text.Trim();
                _current.Reference = _ref.Text.Trim();
                _current.Designation = _name.Text.Trim();
                _current.Brand = _brand.Text.Trim();
                _current.PurchasePrice = _buy.Value;
                _current.SalePrice = _sell.Value;
                _current.Pph = _pph.Value;
                _current.Discount = _disc.Value;
                if (_current.Id == 0) _current.Quantity = (int)_qty.Value;
                _current.MinStock = (int)_min.Value;
                _current.ExpirationDate = _exp.Checked ? _exp.Value.Date : (DateTime?)null;
                _current.ImagePath = _imagePath;
                var cat = _catEdit.SelectedItem as Categorie;
                _current.CategoryId = cat == null ? (int?)null : cat.Id;
                var sup = _sup.SelectedItem as Fournisseur;
                _current.SupplierId = sup == null ? (int?)null : sup.Id;
                ProductService.Save(_current);
                UiMessages.Info("Produit enregistré avec succès.");
                RefreshGrid();
            }
            catch (Exception ex) { UiMessages.Error("Une erreur est survenue lors de l'enregistrement.", ex); }
        }

        private void Delete()
        {
            if (_current.Id == 0) return;
            if (!UiMessages.Confirm("Supprimer ou désactiver ce produit ?")) return;
            try
            {
                ProductService.Delete(_current);
                UiMessages.Info("Produit traité. S'il était utilisé dans une vente, il a été désactivé pour conserver l'historique.");
                ClearForm();
                RefreshGrid();
            }
            catch (Exception ex) { UiMessages.Error("Une erreur est survenue lors de l'enregistrement.", ex); }
        }
    }
}
