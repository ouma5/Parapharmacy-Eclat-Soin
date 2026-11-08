using System;
using System.Drawing;
using System.Windows.Forms;
using EclatEtSoin.Data;
using EclatEtSoin.Helpers;
using EclatEtSoin.Services;
using EclatEtSoin.Theme;

namespace EclatEtSoin.Forms
{
    public class SalesModule : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly DataGridView _lines = new DataGridView();
        private readonly TextBox _number = AppTheme.Input();
        private readonly DateTimePicker _from = new DateTimePicker { Value = DateTime.Today.AddDays(-30) };
        private readonly DateTimePicker _to = new DateTimePicker { Value = DateTime.Today };
        private readonly Label _ca = new Label { Font = AppTheme.HeaderFont, ForeColor = AppTheme.PrimaryDark, AutoSize = true, Padding = new Padding(12, 10, 0, 0) };

        public SalesModule()
        {
            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            AppTheme.ApplyGrid(_grid); AppTheme.ApplyGrid(_lines);
            _grid.Dock = DockStyle.Fill;
            _lines.Dock = DockStyle.Bottom; _lines.Height = 180;
            _grid.SelectionChanged += (s, e) => ShowLines();

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48 };
            var search = AppTheme.SecondaryButton("Rechercher");
            var reprint = AppTheme.SecondaryButton("Ticket caisse");
            var invoice = AppTheme.PrimaryButton("Facture A4");
            var cancel = AppTheme.DangerButton("Annuler la vente");
            var bl = AppTheme.SecondaryButton("Créer facture");
            search.Click += (s, e) => Bind();
            reprint.Click += (s, e) => Print(true);
            invoice.Click += (s, e) => Print(false);
            cancel.Click += (s, e) => Cancel();
            bl.Click += (s, e) => MakeBl();
            _number.Width = 140;
            top.Controls.AddRange(new Control[] { AppTheme.Label("N°"), _number, AppTheme.Label("Du"), _from, AppTheme.Label("Au"), _to, search, reprint, invoice, bl, cancel, _ca });
            Controls.Add(_grid); Controls.Add(_lines); Controls.Add(top);
            Load += (s, e) => Bind();
        }

        private void Bind()
        {
            var t = ReportService.SalesPeriod(_from.Value, _to.Value);
            if (!string.IsNullOrWhiteSpace(_number.Text))
            {
                t.DefaultView.RowFilter = "Number LIKE '%" + _number.Text.Replace("'", "''") + "%'";
                _grid.DataSource = t.DefaultView;
            }
            else _grid.DataSource = t;
            decimal sum = 0;
            foreach (System.Data.DataRow r in t.Rows)
                if (Convert.ToString(r["Status"]) == "Finalisee") sum += Convert.ToDecimal(r["Total"]);
            _ca.Text = "CA période : " + sum.ToString("N2") + " DH";
        }

        private int? CurrentId()
        {
            if (_grid.CurrentRow == null) return null;
            var number = Convert.ToString(_grid.CurrentRow.Cells["Number"].Value);
            var o = Database.Scalar("SELECT Id FROM Sales WHERE Number=@n", Database.P("@n", number));
            return o == null ? (int?)null : Convert.ToInt32(o);
        }

        private void ShowLines()
        {
            var id = CurrentId();
            if (id == null) return;
            _lines.DataSource = CommerceRepository.GetSaleLines(id.Value);
        }

        private void Print(bool ticket)
        {
            var id = CurrentId();
            if (id == null) return;
            if (!ticket)
            {
                PrintHelper.PreviewSaleInvoice(id.Value);
                return;
            }
            var sale = Database.Query(@"SELECT s.*, ISNULL(c.FullName,N'Comptoir') AS ClientName FROM Sales s LEFT JOIN Clients c ON c.Id=s.ClientId WHERE s.Id=@id", Database.P("@id", id.Value)).Rows[0];
            PrintHelper.PreviewDocument("Ticket de caisse", sale["Number"].ToString(), Convert.ToDateTime(sale["SaleDate"]), sale["ClientName"].ToString(),
                CommerceRepository.GetSaleLines(id.Value), Convert.ToDecimal(sale["Discount"]), Convert.ToDecimal(sale["Total"]), true);
        }

        private void Cancel()
        {
            if (!Session.IsAdmin) { UiMessages.Warn("Annulation réservée à l'administrateur."); return; }
            var id = CurrentId();
            if (id == null) return;
            if (!UiMessages.Confirm("Annuler cette vente et restaurer le stock ?")) return;
            var reason = Prompt.Show("Motif d'annulation");
            if (string.IsNullOrWhiteSpace(reason)) return;
            try
            {
                CommerceRepository.CancelSale(id.Value, reason, Session.UserId);
                UiMessages.Info("Vente annulée. Le stock a été restauré.");
                Bind();
            }
            catch (Exception ex) { UiMessages.Error("Une erreur est survenue lors de l'enregistrement.", ex); }
        }

        private void MakeBl()
        {
            var id = CurrentId();
            if (id == null) return;
            try
            {
                var bl = CommerceRepository.CreateDeliveryFromSale(id.Value);
                UiMessages.Info("Facture créée (stock déjà déduit à la vente, pas de double sortie).");
                PrintHelper.PreviewDeliveryInvoice(bl);
            }
            catch (Exception ex) { UiMessages.Error("Impossible de créer la facture.", ex); }
        }
    }

    public static class Prompt
    {
        public static string Show(string title)
        {
            using (var f = new Form { Text = title, Size = new Size(420, 160), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog })
            {
                var t = AppTheme.Input();
                t.SetBounds(20, 20, 360, 28);
                var ok = AppTheme.PrimaryButton("OK");
                ok.SetBounds(250, 60, 130, 36);
                ok.Click += (s, e) => { f.DialogResult = DialogResult.OK; f.Close(); };
                f.Controls.Add(t); f.Controls.Add(ok);
                return f.ShowDialog() == DialogResult.OK ? t.Text : "";
            }
        }
    }
}
