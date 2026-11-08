using System;
using System.Drawing;
using System.Windows.Forms;
using EclatEtSoin.Helpers;
using EclatEtSoin.Theme;

namespace EclatEtSoin.Forms
{
    public class MainForm : Form
    {
        private readonly Panel _content = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Background, Padding = new Padding(18, 8, 18, 16) };
        private readonly Label _userLabel = new Label { AutoSize = true, Font = AppTheme.BodyFont, ForeColor = AppTheme.Muted, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        private Button _activeNav;

        public MainForm()
        {
            Text = "Èclat & Soin — Gestion parapharmacie";
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1200, 720);
            BackColor = AppTheme.Background;
            StartPosition = FormStartPosition.CenterScreen;

            var top = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = AppTheme.Surface };
            top.Paint += (s, e) =>
            {
                using (var pen = new Pen(AppTheme.Accent, 3))
                    e.Graphics.DrawLine(pen, 0, top.Height - 2, top.Width, top.Height - 2);
            };
            var logo = new PictureBox { Image = AppTheme.DrawLogo(300, 54), Size = new Size(300, 54), Location = new Point(16, 8), SizeMode = PictureBoxSizeMode.Zoom };
            var sub = new Label
            {
                Text = "Parapharmacie  ·  Casablanca",
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.Muted,
                AutoSize = true,
                Location = new Point(318, 28)
            };
            _userLabel.Text = (Session.DisplayName ?? "") + "  ·  " + (Session.Current == null ? "" : Session.Current.Role);
            top.Resize += (s, e) => _userLabel.Location = new Point(Math.Max(340, top.Width - _userLabel.Width - 28), 24);
            top.Controls.Add(logo);
            top.Controls.Add(sub);
            top.Controls.Add(_userLabel);

            var side = new Panel { Dock = DockStyle.Left, Width = 248, BackColor = AppTheme.Sidebar, Padding = new Padding(10, 18, 10, 10) };
            var brand = new Label
            {
                Text = "MENU",
                Font = AppTheme.SmallFont,
                ForeColor = Color.FromArgb(150, 180, 176),
                Location = new Point(22, 4),
                AutoSize = true
            };
            side.Controls.Add(brand);

            string[] items = Session.IsAdmin
                ? new[] { "Tableau de bord", "Caisse", "Ventes", "Produits", "Catégories", "Clients", "Fournisseurs", "Achats", "Stock", "Devis", "Factures", "Rapports", "Utilisateurs", "Paramètres" }
                : new[] { "Tableau de bord", "Caisse", "Ventes", "Clients", "Devis" };

            int y = 28;
            foreach (var item in items)
            {
                var b = AppTheme.NavButton(item);
                b.Location = new Point(10, y);
                b.Click += (s, e) =>
                {
                    Highlight(b);
                    Open(item);
                };
                side.Controls.Add(b);
                y += 42;
                if (item == "Tableau de bord") _activeNav = b;
            }

            Controls.Add(_content);
            Controls.Add(side);
            Controls.Add(top);
            Load += (s, e) =>
            {
                if (_activeNav != null) Highlight(_activeNav);
                Open("Tableau de bord");
            };
        }

        private void Highlight(Button b)
        {
            if (_activeNav != null)
            {
                _activeNav.BackColor = Color.Transparent;
                _activeNav.ForeColor = Color.FromArgb(214, 232, 228);
            }
            _activeNav = b;
            b.BackColor = AppTheme.SidebarActive;
            b.ForeColor = Color.White;
        }

        private void Open(string name)
        {
            _content.Controls.Clear();
            UserControl view;
            switch (name)
            {
                case "Caisse": view = new PosModule(); break;
                case "Ventes": view = new SalesModule(); break;
                case "Produits": view = new ProductsModule(); break;
                case "Catégories": view = new CategoriesModule(); break;
                case "Clients": view = new ClientsModule(); break;
                case "Fournisseurs": view = new SuppliersModule(); break;
                case "Achats": view = new PurchasesModule(); break;
                case "Stock": view = new StockModule(); break;
                case "Devis": view = new QuotesModule(); break;
                case "Factures": view = new DeliveryModule(); break;
                case "Rapports": view = new ReportsModule(); break;
                case "Utilisateurs": view = new UsersModule(); break;
                case "Paramètres": view = new SettingsModule(); break;
                default: view = new DashboardModule(); break;
            }
            var header = AppTheme.PageHeader(name, Subtitle(name));
            view.Dock = DockStyle.Fill;
            _content.Controls.Add(view);
            _content.Controls.Add(header);
        }

        private static string Subtitle(string name)
        {
            switch (name)
            {
                case "Caisse": return "Encaissement et facturation au comptoir";
                case "Factures": return "Factures A4 — plus de tickets pour les livraisons";
                case "Ventes": return "Historique, factures et suivi du chiffre d'affaires";
                case "Devis": return "Propositions commerciales sans sortie de stock";
                default: return "Parapharmacie Èclat & Soin";
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "MainForm";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);

        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}
