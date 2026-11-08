using System;
using System.Drawing;
using System.Windows.Forms;
using EclatEtSoin.Data;
using EclatEtSoin.Helpers;
using EclatEtSoin.Services;
using EclatEtSoin.Theme;

namespace EclatEtSoin.Forms
{
    public class LoginForm : Form
    {
        private readonly TextBox _user = AppTheme.Input();
        private readonly TextBox _pass = AppTheme.Input();
        private readonly CheckBox _show = new CheckBox { Text = "Afficher le mot de passe", AutoSize = true, Font = AppTheme.SmallFont, ForeColor = AppTheme.Muted };

        public LoginForm()
        {
            Text = "Connexion — Èclat & Soin";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(860, 540);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = AppTheme.Surface;

            var left = new Panel { Dock = DockStyle.Left, Width = 340, BackColor = AppTheme.PrimaryDark };
            left.Paint += (s, e) =>
            {
                using (var gold = new SolidBrush(AppTheme.Accent))
                    e.Graphics.FillRectangle(gold, 0, left.Height - 8, left.Width, 8);
            };
            var logo = new PictureBox
            {
                Image = AppTheme.DrawLogo(300, 80),
                Size = new Size(300, 80),
                Location = new Point(20, 70),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };
            var welcome = new Label
            {
                Text = "Bienvenue",
                Font = new Font("Segoe UI Semibold", 22, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(28, 180)
            };
            var tag = new Label
            {
                Text = "Gestion de parapharmacie\nSidi Othmane · Casablanca",
                Font = AppTheme.BodyFont,
                ForeColor = Color.FromArgb(196, 220, 216),
                AutoSize = true,
                Location = new Point(30, 228)
            };
            left.Controls.AddRange(new Control[] { logo, welcome, tag });

            var right = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(48, 50, 48, 40) };
            var title = new Label { Text = "Espace professionnel", Font = AppTheme.TitleFont, ForeColor = AppTheme.PrimaryDark, AutoSize = true, Location = new Point(48, 60) };
            var hint = new Label { Text = "Connectez-vous pour ouvrir la caisse et les factures.", Font = AppTheme.SmallFont, ForeColor = AppTheme.Muted, AutoSize = true, Location = new Point(50, 98) };
            var lu = AppTheme.Label("Nom d'utilisateur");
            lu.Location = new Point(48, 150);
            _user.SetBounds(48, 174, 380, 32);
            var lp = AppTheme.Label("Mot de passe");
            lp.Location = new Point(48, 222);
            _pass.UseSystemPasswordChar = true;
            _pass.SetBounds(48, 246, 380, 32);
            _show.Location = new Point(48, 288);
            _show.CheckedChanged += (s, e) => _pass.UseSystemPasswordChar = !_show.Checked;

            var btn = AppTheme.PrimaryButton("Se connecter");
            btn.SetBounds(48, 330, 380, 44);
            btn.Click += (s, e) => Connect();
            AcceptButton = btn;

            var note = new Label
            {
                Text = "Compte initial : admin / Admin@123",
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.Muted,
                AutoSize = true,
                Location = new Point(48, 392)
            };

            right.Controls.AddRange(new Control[] { title, hint, lu, _user, lp, _pass, _show, btn, note });
            Controls.Add(right);
            Controls.Add(left);
            Shown += (s, e) => _user.Focus();
        }

        private void Connect()
        {
            try
            {
                SchemaBootstrapper.EnsureDatabase();
                Session.Current = AuthService.Login(_user.Text, _pass.Text);
                Hide();
                using (var main = new MainForm())
                    main.ShowDialog();
                Session.Current = null;
                _pass.Clear();
                Show();
            }
            catch (InvalidOperationException ex)
            {
                UiMessages.Warn(ex.Message);
            }
            catch (Exception ex)
            {
                UiMessages.Error("Impossible de joindre SQL Server (instance .\\SQLEXPRESS, base PharmacyDB).", ex);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // LoginForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "LoginForm";
            this.Load += new System.EventHandler(this.LoginForm_Load);
            this.ResumeLayout(false);

        }

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }
    }
}
