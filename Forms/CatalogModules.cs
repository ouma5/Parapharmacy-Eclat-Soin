using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EclatEtSoin.Data;
using EclatEtSoin.Helpers;
using EclatEtSoin.Models;
using EclatEtSoin.Theme;

namespace EclatEtSoin.Forms
{
    public class CategoriesModule : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _name = AppTheme.Input();
        private readonly TextBox _desc = AppTheme.Input();
        private int _id;

        public CategoriesModule()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            AppTheme.ApplyGrid(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.SelectionChanged += (s, e) =>
            {
                if (_grid.CurrentRow == null) return;
                _id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
                _name.Text = Convert.ToString(_grid.CurrentRow.Cells["Name"].Value);
                _desc.Text = Convert.ToString(_grid.CurrentRow.Cells["Description"].Value);
            };
            var right = AppTheme.Card();
            right.Dock = DockStyle.Right;
            right.Width = 320;
            _name.SetBounds(12, 40, 280, 26);
            _desc.SetBounds(12, 90, 280, 26);
            var l1 = AppTheme.Label("Nom"); l1.Location = new Point(12, 18);
            var l2 = AppTheme.Label("Description"); l2.Location = new Point(12, 70);
            var add = AppTheme.PrimaryButton("Ajouter"); add.SetBounds(12, 140, 130, 36);
            var upd = AppTheme.SecondaryButton("Modifier"); upd.SetBounds(150, 140, 130, 36);
            var del = AppTheme.DangerButton("Supprimer"); del.SetBounds(12, 186, 268, 36);
            add.Click += (s, e) => { CategoryRepository.Insert(new Categorie { Name = _name.Text.Trim(), Description = _desc.Text }); RefreshGrid(); UiMessages.Info("Catégorie ajoutée avec succès."); };
            upd.Click += (s, e) => { CategoryRepository.Update(new Categorie { Id = _id, Name = _name.Text.Trim(), Description = _desc.Text }); RefreshGrid(); UiMessages.Info("Catégorie modifiée."); };
            del.Click += (s, e) =>
            {
                if (!UiMessages.Confirm("Supprimer cette catégorie ?")) return;
                if (!CategoryRepository.Delete(_id)) UiMessages.Warn("Catégorie utilisée par des produits.");
                RefreshGrid();
            };
            right.Controls.AddRange(new Control[] { l1, _name, l2, _desc, add, upd, del });
            Controls.Add(_grid);
            Controls.Add(right);
            Load += (s, e) => RefreshGrid();
        }

        private void RefreshGrid()
        {
            _grid.DataSource = CategoryRepository.GetAll();
        }
    }

    public class ClientsModule : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly DataGridView _sales = new DataGridView();
        private readonly TextBox _search = AppTheme.Input();
        private readonly TextBox _name = AppTheme.Input();
        private readonly TextBox _phone = AppTheme.Input();
        private readonly TextBox _addr = AppTheme.Input();
        private readonly TextBox _email = AppTheme.Input();
        private int _id;

        public ClientsModule()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            AppTheme.ApplyGrid(_grid);
            AppTheme.ApplyGrid(_sales);
            _grid.Dock = DockStyle.Fill;
            _sales.Dock = DockStyle.Bottom;
            _sales.Height = 180;
            _grid.SelectionChanged += (s, e) => LoadRow();

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44 };
            var b = AppTheme.SecondaryButton("Rechercher");
            b.Click += (s, e) => Bind();
            top.Controls.Add(_search);
            top.Controls.Add(b);

            var right = AppTheme.Card();
            right.Dock = DockStyle.Right;
            right.Width = 320;
            int y = 16;
            foreach (var pair in new[] { Tuple.Create("Nom complet", (Control)_name), Tuple.Create("Téléphone", (Control)_phone), Tuple.Create("Adresse", (Control)_addr), Tuple.Create("E-mail", (Control)_email) })
            {
                var l = AppTheme.Label(pair.Item1); l.Location = new Point(12, y); y += 18;
                pair.Item2.SetBounds(12, y, 280, 26); y += 34;
                right.Controls.Add(l); right.Controls.Add(pair.Item2);
            }
            var add = AppTheme.PrimaryButton("Ajouter"); add.SetBounds(12, y, 130, 36);
            var upd = AppTheme.SecondaryButton("Modifier"); upd.SetBounds(150, y, 130, 36);
            add.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_name.Text)) { UiMessages.Warn("Veuillez saisir le nom du client."); return; }
                ClientRepository.Insert(new Client { FullName = _name.Text.Trim(), Phone = _phone.Text, Address = _addr.Text, Email = _email.Text });
                UiMessages.Info("Client ajouté avec succès.");
                Bind();
            };
            upd.Click += (s, e) =>
            {
                ClientRepository.Update(new Client { Id = _id, FullName = _name.Text.Trim(), Phone = _phone.Text, Address = _addr.Text, Email = _email.Text });
                UiMessages.Info("Client modifié.");
                Bind();
            };
            right.Controls.Add(add); right.Controls.Add(upd);
            Controls.Add(_grid);
            Controls.Add(_sales);
            Controls.Add(right);
            Controls.Add(top);
            Load += (s, e) => Bind();
        }

        private void Bind()
        {
            var q = _search.Text.Trim();
            _grid.DataSource = string.IsNullOrEmpty(q) ? ClientRepository.GetAll() : ClientRepository.Search(q);
        }

        private void LoadRow()
        {
            if (_grid.CurrentRow == null) return;
            _id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            var c = ClientRepository.GetById(_id);
            if (c == null) return;
            _name.Text = c.FullName; _phone.Text = c.Phone; _addr.Text = c.Address; _email.Text = c.Email;
            _sales.DataSource = Database.Query(@"
SELECT Number, SaleDate, Total, PaymentMode, Status FROM Sales WHERE ClientId=@id ORDER BY SaleDate DESC",
                Database.P("@id", _id));
        }
    }

    public class SuppliersModule : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly DataGridView _purchases = new DataGridView();
        private readonly TextBox _search = AppTheme.Input();
        private readonly TextBox _name = AppTheme.Input();
        private readonly TextBox _company = AppTheme.Input();
        private readonly TextBox _phone = AppTheme.Input();
        private readonly TextBox _addr = AppTheme.Input();
        private readonly TextBox _email = AppTheme.Input();
        private readonly TextBox _notes = AppTheme.Input();
        private int _id;

        public SuppliersModule()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            AppTheme.ApplyGrid(_grid); AppTheme.ApplyGrid(_purchases);
            _grid.Dock = DockStyle.Fill;
            _purchases.Dock = DockStyle.Bottom; _purchases.Height = 160;
            _grid.SelectionChanged += (s, e) => LoadRow();
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44 };
            var b = AppTheme.SecondaryButton("Rechercher");
            b.Click += (s, e) => Bind();
            _search.Width = 220;
            top.Controls.AddRange(new Control[] { _search, b });

            var right = AppTheme.Card(); right.Dock = DockStyle.Right; right.Width = 330;
            var fields = new[] { Tuple.Create("Nom", (Control)_name), Tuple.Create("Société", (Control)_company), Tuple.Create("Téléphone", (Control)_phone), Tuple.Create("Adresse", (Control)_addr), Tuple.Create("E-mail", (Control)_email), Tuple.Create("Notes", (Control)_notes) };
            int y = 12;
            foreach (var f in fields)
            {
                var l = AppTheme.Label(f.Item1); l.Location = new Point(12, y); y += 18;
                f.Item2.SetBounds(12, y, 290, 26); y += 32;
                right.Controls.Add(l); right.Controls.Add(f.Item2);
            }
            var add = AppTheme.PrimaryButton("Ajouter"); add.SetBounds(12, y, 130, 36);
            var upd = AppTheme.SecondaryButton("Modifier"); upd.SetBounds(160, y, 130, 36);
            add.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_name.Text)) { UiMessages.Warn("Veuillez saisir le nom du fournisseur."); return; }
                SupplierRepository.Insert(new Fournisseur { Name = _name.Text.Trim(), Company = _company.Text, Phone = _phone.Text, Address = _addr.Text, Email = _email.Text, Notes = _notes.Text });
                UiMessages.Info("Fournisseur ajouté avec succès."); Bind();
            };
            upd.Click += (s, e) =>
            {
                SupplierRepository.Update(new Fournisseur { Id = _id, Name = _name.Text.Trim(), Company = _company.Text, Phone = _phone.Text, Address = _addr.Text, Email = _email.Text, Notes = _notes.Text });
                UiMessages.Info("Fournisseur modifié."); Bind();
            };
            right.Controls.Add(add); right.Controls.Add(upd);
            Controls.Add(_grid); Controls.Add(_purchases); Controls.Add(right); Controls.Add(top);
            Load += (s, e) => Bind();
        }

        private void Bind()
        {
            var q = _search.Text.Trim();
            _grid.DataSource = string.IsNullOrEmpty(q) ? SupplierRepository.GetAll() : SupplierRepository.Search(q);
        }

        private void LoadRow()
        {
            if (_grid.CurrentRow == null) return;
            _id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            var s = SupplierRepository.GetById(_id);
            if (s == null) return;
            _name.Text = s.Name; _company.Text = s.Company; _phone.Text = s.Phone; _addr.Text = s.Address; _email.Text = s.Email; _notes.Text = s.Notes;
            _purchases.DataSource = Database.Query("SELECT Number, PurchaseDate, Total, Status FROM Purchases WHERE SupplierId=@id ORDER BY PurchaseDate DESC", Database.P("@id", _id));
        }
    }
}
