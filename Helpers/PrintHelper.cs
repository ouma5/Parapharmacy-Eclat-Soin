using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using EclatEtSoin.Data;
using EclatEtSoin.Models;
using EclatEtSoin.Theme;

namespace EclatEtSoin.Helpers
{
    public class InvoiceDocument
    {
        public string Title { get; set; }
        public string Number { get; set; }
        public DateTime Date { get; set; }
        public string ClientName { get; set; }
        public string ClientPhone { get; set; }
        public string ClientAddress { get; set; }
        public string ClientEmail { get; set; }
        public List<LigneDocument> Lines { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
        public string Notes { get; set; }
    }

    public static class PrintHelper
    {
        public static void PreviewInvoice(InvoiceDocument invoice)
        {
            var doc = BuildInvoice(invoice);
            using (var dlg = new PrintPreviewDialog { Document = doc, Width = 1000, Height = 760, Text = "Aperçu — " + invoice.Title })
                dlg.ShowDialog();
        }

        public static void PreviewDeliveryInvoice(int deliveryId)
        {
            PreviewInvoice(LoadDelivery(deliveryId));
        }

        public static void PreviewSaleInvoice(int saleId)
        {
            PreviewInvoice(LoadSale(saleId, "FACTURE"));
        }

        public static void PreviewDocument(string title, string number, DateTime date, string client, List<LigneDocument> lines, decimal discount, decimal total, bool ticket)
        {
            if (ticket)
            {
                var doc = BuildTicket(title, number, date, client, lines, discount, total);
                using (var dlg = new PrintPreviewDialog { Document = doc, Width = 900, Height = 700, Text = "Aperçu — " + title })
                    dlg.ShowDialog();
                return;
            }
            PreviewInvoice(new InvoiceDocument
            {
                Title = title,
                Number = number,
                Date = date,
                ClientName = client,
                Lines = lines,
                Discount = discount,
                Total = total
            });
        }

        public static void PrintDocument(string title, string number, DateTime date, string client, List<LigneDocument> lines, decimal discount, decimal total, bool ticket)
        {
            PrintDocument doc = ticket
                ? BuildTicket(title, number, date, client, lines, discount, total)
                : BuildInvoice(new InvoiceDocument
                {
                    Title = title,
                    Number = number,
                    Date = date,
                    ClientName = client,
                    Lines = lines,
                    Discount = discount,
                    Total = total
                });
            var printer = SchemaBootstrapper.GetSetting("DefaultPrinter");
            if (!string.IsNullOrWhiteSpace(printer))
                doc.PrinterSettings.PrinterName = printer;
            using (var dlg = new PrintDialog { Document = doc, UseEXDialog = true })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    doc.Print();
            }
        }

        public static InvoiceDocument LoadDelivery(int id)
        {
            var d = Database.Query(@"
SELECT d.*, ISNULL(c.FullName,N'Comptoir') AS ClientName, c.Phone, c.Address, c.Email
FROM DeliveryNotes d LEFT JOIN Clients c ON c.Id=d.ClientId WHERE d.Id=@id", Database.P("@id", id)).Rows[0];
            return new InvoiceDocument
            {
                Title = "FACTURE",
                Number = d["Number"].ToString(),
                Date = Convert.ToDateTime(d["DeliveryDate"]),
                ClientName = Convert.ToString(d["ClientName"]),
                ClientPhone = NullStr(d["Phone"]),
                ClientAddress = NullStr(d["Address"]),
                ClientEmail = NullStr(d["Email"]),
                Lines = CommerceRepository.GetDeliveryLines(id),
                Discount = 0,
                Total = Convert.ToDecimal(d["Total"]),
                Notes = "Livraison effectuée — stock déduit."
            };
        }

        public static InvoiceDocument LoadSale(int id, string title)
        {
            var sale = Database.Query(@"
SELECT s.*, ISNULL(c.FullName,N'Comptoir') AS ClientName, c.Phone, c.Address, c.Email
FROM Sales s LEFT JOIN Clients c ON c.Id=s.ClientId WHERE s.Id=@id", Database.P("@id", id)).Rows[0];
            return new InvoiceDocument
            {
                Title = title,
                Number = sale["Number"].ToString(),
                Date = Convert.ToDateTime(sale["SaleDate"]),
                ClientName = Convert.ToString(sale["ClientName"]),
                ClientPhone = NullStr(sale["Phone"]),
                ClientAddress = NullStr(sale["Address"]),
                ClientEmail = NullStr(sale["Email"]),
                Lines = CommerceRepository.GetSaleLines(id),
                Discount = Convert.ToDecimal(sale["Discount"]),
                Total = Convert.ToDecimal(sale["Total"]),
                PaymentMethod = Convert.ToString(sale["PaymentMode"])
            };
        }

        private static string NullStr(object v)
        {
            if (v == null || v == DBNull.Value) return "";
            return v.ToString();
        }

        private static PrintDocument BuildInvoice(InvoiceDocument inv)
        {
            var doc = new PrintDocument();
            doc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
            doc.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
            int start = 0;
            int page = 1;
            doc.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                var left = e.MarginBounds.Left;
                var top = e.MarginBounds.Top;
                var width = e.MarginBounds.Width;
                var bottom = e.MarginBounds.Bottom;
                float y = top;

                using (var header = new SolidBrush(AppTheme.PrimaryDark))
                    g.FillRectangle(header, left, y, width, 8);
                y += 18;

                using (var logo = AppTheme.DrawLogo(280, 64))
                    g.DrawImage(logo, left, y);

                using (var titleFont = new Font("Segoe UI", 20, FontStyle.Bold))
                using (var brush = new SolidBrush(AppTheme.PrimaryDark))
                {
                    var title = string.IsNullOrWhiteSpace(inv.Title) ? "FACTURE" : inv.Title.ToUpperInvariant();
                    var sz = g.MeasureString(title, titleFont);
                    g.DrawString(title, titleFont, brush, left + width - sz.Width, y);
                }
                y += 68;

                using (var font = new Font("Segoe UI", 9))
                using (var bold = new Font("Segoe UI Semibold", 10, FontStyle.Bold))
                using (var small = new Font("Segoe UI", 8))
                using (var brush = new SolidBrush(AppTheme.Text))
                using (var muted = new SolidBrush(AppTheme.Muted))
                using (var teal = new SolidBrush(AppTheme.Primary))
                {
                    var trade = SchemaBootstrapper.GetSetting("TradeName", "Parapharmacie Èclat & Soin");
                    var addr = SchemaBootstrapper.GetSetting("Address", "Casablanca, Sidi Othmane, Lbaladia, Rue 46, N°49");
                    var phone = SchemaBootstrapper.GetSetting("Phone");
                    var ice = SchemaBootstrapper.GetSetting("Ice");
                    var iff = SchemaBootstrapper.GetSetting("If");
                    var rc = SchemaBootstrapper.GetSetting("Rc");

                    g.DrawString(trade, bold, teal, left, y); y += 16;
                    g.DrawString(addr, font, brush, left, y); y += 14;
                    if (!string.IsNullOrEmpty(phone)) { g.DrawString("Tél. " + phone, font, brush, left, y); y += 14; }
                    var legal = "";
                    if (!string.IsNullOrEmpty(ice)) legal += "ICE : " + ice + "    ";
                    if (!string.IsNullOrEmpty(iff)) legal += "IF : " + iff + "    ";
                    if (!string.IsNullOrEmpty(rc)) legal += "RC : " + rc;
                    if (!string.IsNullOrEmpty(legal)) { g.DrawString(legal, small, muted, left, y); y += 14; }

                    var boxW = 250f;
                    var boxX = left + width - boxW;
                    var boxY = y - 58;
                    using (var boxFill = new SolidBrush(Color.FromArgb(241, 247, 245)))
                    using (var boxPen = new Pen(AppTheme.Primary))
                    {
                        g.FillRectangle(boxFill, boxX, boxY, boxW, 70);
                        g.DrawRectangle(boxPen, boxX, boxY, boxW, 70);
                    }
                    g.DrawString("N° " + inv.Number, bold, teal, boxX + 10, boxY + 10);
                    g.DrawString("Date : " + inv.Date.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), font, brush, boxX + 10, boxY + 30);
                    g.DrawString("Page " + page, small, muted, boxX + 10, boxY + 48);

                    y += 10;
                    using (var clientFill = new SolidBrush(Color.FromArgb(252, 250, 244)))
                    using (var goldPen = new Pen(AppTheme.Accent, 1.5f))
                    {
                        g.FillRectangle(clientFill, left, y, width, 62);
                        g.DrawRectangle(goldPen, left, y, width, 62);
                    }
                    g.DrawString("Facturé à", small, muted, left + 10, y + 6);
                    g.DrawString(string.IsNullOrWhiteSpace(inv.ClientName) ? "Client comptoir" : inv.ClientName, bold, brush, left + 10, y + 20);
                    var extra = (inv.ClientAddress ?? "").Trim();
                    if (!string.IsNullOrEmpty(inv.ClientPhone)) extra = (extra + "   Tél. " + inv.ClientPhone).Trim();
                    if (!string.IsNullOrEmpty(inv.ClientEmail)) extra = (extra + "   " + inv.ClientEmail).Trim();
                    if (!string.IsNullOrEmpty(extra)) g.DrawString(extra, font, brush, left + 10, y + 40);
                    y += 78;

                    float[] cols = { 36, width * 0.42f, 50, 90, 70, 0 };
                    cols[5] = width - cols[0] - cols[1] - cols[2] - cols[3] - cols[4];
                    string[] heads = { "N°", "Désignation", "Qté", "P.U. DH", "Remise", "Total DH" };
                    using (var headBrush = new SolidBrush(AppTheme.PrimaryDark))
                    using (var white = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(headBrush, left, y, width, 26);
                        float x = left;
                        for (int i = 0; i < heads.Length; i++)
                        {
                            var align = i == 0 || i >= 2 ? StringAlignment.Far : StringAlignment.Near;
                            var sf = new StringFormat { Alignment = align };
                            var pad = i == 0 || i >= 2 ? -6 : 8;
                            g.DrawString(heads[i], bold, white, new RectangleF(x + pad, y + 4, cols[i], 20), sf);
                            x += cols[i];
                        }
                    }
                    y += 26;

                    int n = start;
                    while (n < inv.Lines.Count)
                    {
                        if (y > bottom - 160)
                        {
                            e.HasMorePages = true;
                            start = n;
                            page++;
                            return;
                        }
                        var l = inv.Lines[n];
                        if (n % 2 == 1)
                        {
                            using (var alt = new SolidBrush(Color.FromArgb(247, 252, 250)))
                                g.FillRectangle(alt, left, y, width, 22);
                        }
                        float x = left;
                        string[] vals =
                        {
                            (n + 1).ToString(),
                            l.Designation,
                            l.Quantity.ToString(),
                            l.UnitPrice.ToString("N2"),
                            l.Discount > 0 ? l.Discount.ToString("N1") + " %" : "—",
                            l.LineTotal.ToString("N2")
                        };
                        for (int i = 0; i < vals.Length; i++)
                        {
                            var align = i == 0 || i >= 2 ? StringAlignment.Far : StringAlignment.Near;
                            var sf = new StringFormat { Alignment = align, Trimming = StringTrimming.EllipsisCharacter };
                            var pad = i == 0 || i >= 2 ? -6 : 8;
                            g.DrawString(vals[i], font, brush, new RectangleF(x + pad, y + 3, cols[i], 18), sf);
                            x += cols[i];
                        }
                        y += 22;
                        n++;
                    }

                    using (var linePen = new Pen(AppTheme.Border))
                        g.DrawLine(linePen, left, y, left + width, y);
                    y += 12;

                    var totW = 260f;
                    var totX = left + width - totW;
                    using (var totFill = new SolidBrush(Color.FromArgb(241, 247, 245)))
                    using (var totPen = new Pen(AppTheme.PrimaryDark))
                    {
                        g.FillRectangle(totFill, totX, y, totW, 78);
                        g.DrawRectangle(totPen, totX, y, totW, 78);
                    }
                    var ht = inv.Total + inv.Discount;
                    g.DrawString("Sous-total", font, brush, totX + 12, y + 10);
                    g.DrawString(ht.ToString("N2") + " DH", font, brush, totX + totW - 110, y + 10);
                    g.DrawString("Remise", font, brush, totX + 12, y + 30);
                    g.DrawString(inv.Discount.ToString("N2") + " DH", font, brush, totX + totW - 110, y + 30);
                    using (var totFont = new Font("Segoe UI", 11, FontStyle.Bold))
                    {
                        g.DrawString("TOTAL TTC", totFont, teal, totX + 12, y + 50);
                        g.DrawString(inv.Total.ToString("N2") + " DH", totFont, teal, totX + totW - 130, y + 50);
                    }
                    y += 96;

                    if (!string.IsNullOrEmpty(inv.PaymentMethod))
                        g.DrawString("Mode de règlement : " + inv.PaymentMethod, font, brush, left, y);
                    y += 18;
                    g.DrawString("Montants exprimés en dirhams marocains (DH), TTC.", small, muted, left, y);
                    y += 14;
                    if (!string.IsNullOrEmpty(inv.Notes))
                        g.DrawString(inv.Notes, small, muted, left, y);
                    y += 36;
                    g.DrawString("Cachet et signature", small, muted, left + width - 180, y);
                    using (var signPen = new Pen(AppTheme.Border))
                        g.DrawRectangle(signPen, left + width - 200, y + 16, 190, 70);
                    using (var footPen = new Pen(AppTheme.PrimaryDark, 2))
                        g.DrawLine(footPen, left, bottom - 18, left + width, bottom - 18);
                    g.DrawString("Parapharmacie Èclat & Soin  ·  Casablanca  ·  Document commercial", small, muted, left, bottom - 14);
                }
                e.HasMorePages = false;
                start = 0;
                page = 1;
            };
            return doc;
        }

        private static PrintDocument BuildTicket(string title, string number, DateTime date, string client, List<LigneDocument> lines, decimal discount, decimal total)
        {
            var doc = new PrintDocument();
            doc.DefaultPageSettings.PaperSize = new PaperSize("Ticket", 280, 800);
            doc.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                var y = 20f;
                var left = 10f;
                var width = 250f;
                using (var logo = AppTheme.DrawLogo(220, 56))
                    g.DrawImage(logo, left, y);
                y += 62;
                using (var font = new Font("Segoe UI", 8))
                using (var bold = new Font("Segoe UI", 9, FontStyle.Bold))
                using (var brush = new SolidBrush(AppTheme.Text))
                {
                    g.DrawString(SchemaBootstrapper.GetSetting("TradeName", "Parapharmacie Èclat & Soin"), bold, brush, left, y);
                    y += 18;
                    g.DrawString(SchemaBootstrapper.GetSetting("Address", "Casablanca, Sidi Othmane, Lbaladia, Rue 46, N°49"), font, brush, left, y);
                    y += 16;
                    y += 8;
                    g.DrawString(title, bold, brush, left, y); y += 20;
                    g.DrawString("N° " + number + "   " + date.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), font, brush, left, y); y += 16;
                    if (!string.IsNullOrEmpty(client)) { g.DrawString("Client : " + client, font, brush, left, y); y += 18; }
                    g.DrawLine(Pens.Gray, left, y, left + width, y); y += 8;
                    foreach (var l in lines)
                    {
                        g.DrawString(l.Designation, font, brush, left, y);
                        y += 14;
                        g.DrawString(l.Quantity + " x " + l.UnitPrice.ToString("N2") + "  =  " + l.LineTotal.ToString("N2") + " DH", font, brush, left, y);
                        y += 16;
                    }
                    g.DrawLine(Pens.Gray, left, y, left + width, y); y += 8;
                    if (discount > 0) { g.DrawString("Remise : " + discount.ToString("N2") + " DH", font, brush, left, y); y += 16; }
                    g.DrawString("TOTAL : " + total.ToString("N2") + " DH", bold, brush, left, y);
                }
            };
            return doc;
        }
    }
}
