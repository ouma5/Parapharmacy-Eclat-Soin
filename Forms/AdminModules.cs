using System;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using EclatEtSoin.Data;
using EclatEtSoin.Helpers;
using EclatEtSoin.Models;
using EclatEtSoin.Theme;

namespace EclatEtSoin.Forms
{
    public class UsersModule : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _user = AppTheme.Input();
        private readonly TextBox _name = AppTheme.Input();
        private readonly TextBox _pass = AppTheme.Input();
        private readonly ComboBox _role = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.BodyFont };
        private readonly CheckBox _active = new CheckBox { Text = "Compte actif", Checked = true, AutoSize = true, Font = AppTheme.BodyFont };
        private int _id;

        public UsersModule()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            AppTheme.ApplyGrid(_grid);
            _grid.Dock = DockStyle.Fill;
            _role.Items.AddRange(new object[] { "Administrateur", "Vendeur" });
            _role.SelectedIndex = 1;
            _pass.UseSystemPasswordChar = true;
            _grid.SelectionChanged += (s, e) =>
            {
                if (_grid.CurrentRow == null) return;
                _id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
                _user.Text = Convert.ToString(_grid.CurrentRow.Cells["Username"].Value);
                _name.Text = Convert.ToString(_grid.CurrentRow.Cells["FullName"].Value);
                _role.Text = Convert.ToString(_grid.CurrentRow.Cells["Role"].Value);
                _active.Checked = Convert.ToBoolean(_grid.CurrentRow.Cells["IsActive"].Value);
            };
            var right = AppTheme.Card();
            right.Dock = DockStyle.Right;
            right.Width = 330;
            _user.SetBounds(12, 36, 290, 26);
            _name.SetBounds(12, 90, 290, 26);
            _pass.SetBounds(12, 144, 290, 26);
            _role.SetBounds(12, 198, 290, 26);
            _active.Location = new System.Drawing.Point(12, 236);
            var l1 = AppTheme.Label("Identifiant"); l1.Location = new System.Drawing.Point(12, 16);
            var l2 = AppTheme.Label("Nom complet"); l2.Location = new System.Drawing.Point(12, 70);
            var l3 = AppTheme.Label("Mot de passe (nouveau)"); l3.Location = new System.Drawing.Point(12, 124);
            var l4 = AppTheme.Label("Rôle"); l4.Location = new System.Drawing.Point(12, 178);
            var add = AppTheme.PrimaryButton("Ajouter"); add.SetBounds(12, 280, 140, 36);
            var upd = AppTheme.SecondaryButton("Modifier"); upd.SetBounds(160, 280, 140, 36);
            add.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_user.Text) || string.IsNullOrWhiteSpace(_pass.Text))
                { UiMessages.Warn("Identifiant et mot de passe obligatoires."); return; }
                UserRepository.Insert(new User
                {
                    Username = _user.Text.Trim(),
                    PasswordHash = PasswordHasher.Hash(_pass.Text),
                    FullName = _name.Text.Trim(),
                    Role = _role.Text,
                    IsActive = _active.Checked
                });
                UiMessages.Info("Utilisateur ajouté.");
                Bind();
            };
            upd.Click += (s, e) =>
            {
                UserRepository.Update(new User { Id = _id, FullName = _name.Text.Trim(), Role = _role.Text, IsActive = _active.Checked });
                if (!string.IsNullOrWhiteSpace(_pass.Text))
                    UserRepository.UpdatePassword(_id, PasswordHasher.Hash(_pass.Text));
                UiMessages.Info("Utilisateur modifié.");
                Bind();
            };
            right.Controls.AddRange(new Control[] { l1, _user, l2, _name, l3, _pass, l4, _role, _active, add, upd });
            Controls.Add(_grid); Controls.Add(right);
            Load += (s, e) => Bind();
        }

        private void Bind()
        {
            _grid.DataSource = UserRepository.GetAll().Select(u => new { u.Id, u.Username, u.FullName, u.Role, u.IsActive, u.CreatedAt }).ToList();
        }
    }

    public class SettingsModule : UserControl
    {
        public SettingsModule()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            var name = AppTheme.Input(); name.Width = 420; name.Text = SchemaBootstrapper.GetSetting("TradeName", "Parapharmacie Èclat & Soin");
            var addr = AppTheme.Input(); addr.Width = 420; addr.Text = SchemaBootstrapper.GetSetting("Address", "Casablanca, Sidi Othmane, Lbaladia, Rue 46, N°49");
            var phone = AppTheme.Input(); phone.Width = 420; phone.Text = SchemaBootstrapper.GetSetting("Phone");
            var ice = AppTheme.Input(); ice.Width = 420; ice.Text = SchemaBootstrapper.GetSetting("Ice");
            var iff = AppTheme.Input(); iff.Width = 420; iff.Text = SchemaBootstrapper.GetSetting("If");
            var rc = AppTheme.Input(); rc.Width = 420; rc.Text = SchemaBootstrapper.GetSetting("Rc");
            var printer = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 420, Font = AppTheme.BodyFont };
            printer.Items.Add("");
            foreach (string p in PrinterSettings.InstalledPrinters) printer.Items.Add(p);
            printer.Text = SchemaBootstrapper.GetSetting("DefaultPrinter");
            var min = new NumericUpDown { Maximum = 10000, Value = decimal.Parse(SchemaBootstrapper.GetSetting("DefaultMinStock", "5")), Font = AppTheme.BodyFont };
            var panel = AppTheme.Card();
            panel.Dock = DockStyle.Fill;
            int y = 20;
            void Add(string cap, Control c)
            {
                var l = AppTheme.Label(cap); l.Location = new System.Drawing.Point(24, y); y += 20;
                c.Location = new System.Drawing.Point(24, y); y += 40;
                panel.Controls.Add(l); panel.Controls.Add(c);
            }
            Add("Nom commercial", name);
            Add("Adresse (documents)", addr);
            Add("Téléphone", phone);
            Add("ICE (facture)", ice);
            Add("Identifiant fiscal", iff);
            Add("Registre de commerce", rc);
            Add("Imprimante par défaut", printer);
            Add("Seuil de stock par défaut", min);
            var save = AppTheme.PrimaryButton("Enregistrer les paramètres");
            save.SetBounds(24, y, 280, 40);
            save.Click += (s, e) =>
            {
                SchemaBootstrapper.UpsertSetting("TradeName", name.Text);
                SchemaBootstrapper.UpsertSetting("Address", addr.Text);
                SchemaBootstrapper.UpsertSetting("Phone", phone.Text);
                SchemaBootstrapper.UpsertSetting("Ice", ice.Text);
                SchemaBootstrapper.UpsertSetting("If", iff.Text);
                SchemaBootstrapper.UpsertSetting("Rc", rc.Text);
                SchemaBootstrapper.UpsertSetting("DefaultPrinter", printer.Text);
                SchemaBootstrapper.UpsertSetting("DefaultMinStock", ((int)min.Value).ToString());
                UiMessages.Info("Paramètres enregistrés.");
            };
            panel.Controls.Add(save);
            var logo = new PictureBox { Image = AppTheme.DrawLogo(360, 90), Size = new System.Drawing.Size(360, 90), Location = new System.Drawing.Point(24, y + 60), SizeMode = PictureBoxSizeMode.Zoom };
            panel.Controls.Add(logo);
            Controls.Add(panel);
        }
    }
}
