using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using EclatEtSoin.Helpers;
using EclatEtSoin.Services;
using EclatEtSoin.Theme;

namespace EclatEtSoin.Forms
{
    public class DashboardModule : UserControl
    {
        public DashboardModule()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            Load += (s, e) => Build();
        }

        private void Build()
        {
            Controls.Clear();
            try
            {
                var kpi = ReportService.DashboardKpis().Rows[0];
                var wrap = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 130, BackColor = AppTheme.Background, WrapContents = false, AutoScroll = true };
                wrap.Controls.Add(Card("Produits", kpi["Produits"].ToString()));
                wrap.Controls.Add(Card("Clients", kpi["Clients"].ToString()));
                wrap.Controls.Add(Card("Fournisseurs", kpi["Fournisseurs"].ToString()));
                wrap.Controls.Add(Card("Ventes du jour", kpi["VentesJour"].ToString()));
                wrap.Controls.Add(Card("CA du jour", Convert.ToDecimal(kpi["CaJour"]).ToString("N2") + " DH"));
                wrap.Controls.Add(Card("Stock faible", kpi["StockFaible"].ToString(), AppTheme.Warning));
                wrap.Controls.Add(Card("Rupture", kpi["Rupture"].ToString(), AppTheme.Danger));
                wrap.Controls.Add(Card("Expir. proche", kpi["Expiration"].ToString(), AppTheme.AccentGreen));

                var charts = new TableLayoutPanel { Dock = DockStyle.Top, Height = 280, ColumnCount = 2, Padding = new Padding(0, 8, 0, 8) };
                charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
                charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
                charts.Controls.Add(SalesChart(), 0, 0);
                charts.Controls.Add(TopChart(), 1, 0);

                var grid = new DataGridView { Dock = DockStyle.Fill };
                AppTheme.ApplyGrid(grid);
                grid.DataSource = ReportService.LatestSales();
                var title = new Label { Text = "Dernières ventes", Font = AppTheme.HeaderFont, ForeColor = AppTheme.PrimaryDark, Dock = DockStyle.Top, Height = 28 };

                Controls.Add(grid);
                Controls.Add(title);
                Controls.Add(charts);
                Controls.Add(wrap);
            }
            catch (Exception ex)
            {
                Controls.Add(new Label { Text = "Impossible de charger le tableau de bord : " + ex.Message, Dock = DockStyle.Fill, ForeColor = AppTheme.Danger, Font = AppTheme.BodyFont });
            }
        }

        private static Panel Card(string title, string value, Color? accent = null)
        {
            var color = accent ?? AppTheme.Primary;
            var p = AppTheme.Card();
            p.Size = new Size(164, 118);
            p.Margin = new Padding(8);
            p.Paint += (s, e) =>
            {
                using (var b = new SolidBrush(color))
                    e.Graphics.FillRectangle(b, 0, 0, 6, p.Height);
            };
            var t = new Label { Text = title, Font = AppTheme.SmallFont, ForeColor = AppTheme.Muted, Location = new Point(18, 16), AutoSize = true };
            var v = new Label { Text = value, Font = new Font("Segoe UI Semibold", 17, FontStyle.Bold), ForeColor = accent ?? AppTheme.PrimaryDark, Location = new Point(16, 46), AutoSize = true };
            p.Controls.Add(t);
            p.Controls.Add(v);
            return p;
        }

        private static Chart SalesChart()
        {
            var c = new Chart { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
            c.ChartAreas.Add(new ChartArea("a") { BackColor = AppTheme.Surface });
            var s = new Series("CA") { ChartType = SeriesChartType.Column, Color = AppTheme.Primary };
            foreach (System.Data.DataRow r in ReportService.SalesByDay(14).Rows)
                s.Points.AddXY(Convert.ToDateTime(r["Jour"]).ToString("dd/MM"), Convert.ToDouble(r["Montant"]));
            c.Series.Add(s);
            c.Titles.Add("Chiffre d'affaires (14 jours)");
            return c;
        }

        private static Chart TopChart()
        {
            var c = new Chart { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
            c.ChartAreas.Add(new ChartArea("a") { BackColor = AppTheme.Surface });
            var s = new Series("Top") { ChartType = SeriesChartType.Pie };
            foreach (System.Data.DataRow r in ReportService.TopProducts().Rows)
                s.Points.AddXY(r["Designation"].ToString(), Convert.ToDouble(r["Qte"]));
            c.Series.Add(s);
            c.Titles.Add("Produits les plus vendus");
            return c;
        }
    }
}
