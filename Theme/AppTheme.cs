using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EclatEtSoin.Theme
{
    public static class AppTheme
    {
        public static readonly Color Primary = Color.FromArgb(15, 118, 110);
        public static readonly Color PrimaryDark = Color.FromArgb(17, 64, 64);
        public static readonly Color PrimaryLight = Color.FromArgb(45, 148, 138);
        public static readonly Color Accent = Color.FromArgb(196, 149, 74);
        public static readonly Color AccentGreen = Color.FromArgb(34, 139, 90);
        public static readonly Color Background = Color.FromArgb(241, 247, 245);
        public static readonly Color Surface = Color.White;
        public static readonly Color Sidebar = Color.FromArgb(16, 48, 51);
        public static readonly Color SidebarHover = Color.FromArgb(28, 78, 80);
        public static readonly Color SidebarActive = Color.FromArgb(15, 118, 110);
        public static readonly Color Text = Color.FromArgb(28, 42, 44);
        public static readonly Color Muted = Color.FromArgb(108, 128, 128);
        public static readonly Color Danger = Color.FromArgb(185, 58, 52);
        public static readonly Color Warning = Color.FromArgb(214, 144, 36);
        public static readonly Color Border = Color.FromArgb(214, 228, 224);
        public static readonly Font TitleFont = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
        public static readonly Font HeaderFont = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold);
        public static readonly Font BodyFont = new Font("Segoe UI", 10F);
        public static readonly Font SmallFont = new Font("Segoe UI", 8.5F);

        public static void ApplyGrid(DataGridView grid)
        {
            grid.BackgroundColor = Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = PrimaryDark;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            grid.ColumnHeadersHeight = 40;
            grid.DefaultCellStyle.Font = BodyFont;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(209, 237, 232);
            grid.DefaultCellStyle.SelectionForeColor = PrimaryDark;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 252, 250);
            grid.RowTemplate.Height = 34;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.RowHeadersVisible = false;
            grid.GridColor = Border;
        }

        public static Button PrimaryButton(string text)
        {
            var b = BaseButton(text, Primary, Color.White, HeaderFont, 42);
            WireHover(b, Primary, PrimaryLight);
            return b;
        }

        public static Button SecondaryButton(string text)
        {
            var b = BaseButton(text, Surface, PrimaryDark, BodyFont, 38);
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Primary;
            WireHover(b, Surface, Color.FromArgb(232, 246, 243));
            return b;
        }

        public static Button DangerButton(string text)
        {
            var b = BaseButton(text, Danger, Color.White, BodyFont, 38);
            WireHover(b, Danger, Color.FromArgb(160, 45, 40));
            return b;
        }

        public static Button NavButton(string text)
        {
            var b = new Button
            {
                Text = "   " + text,
                Width = 228,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = BodyFont,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(214, 232, 228),
                Cursor = Cursors.Hand,
                Padding = new Padding(8, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = SidebarHover;
            b.FlatAppearance.MouseDownBackColor = SidebarActive;
            return b;
        }

        public static TextBox Input()
        {
            return new TextBox
            {
                Font = BodyFont,
                BorderStyle = BorderStyle.FixedSingle,
                Height = 30
            };
        }

        public static Label Label(string text, bool muted = false)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = BodyFont,
                ForeColor = muted ? Muted : Text
            };
        }

        public static Panel Card()
        {
            var p = new Panel
            {
                BackColor = Surface,
                Padding = new Padding(16)
            };
            p.Paint += (s, e) =>
            {
                using (var pen = new Pen(Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            return p;
        }

        public static Panel PageHeader(string title, string subtitle)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Background };
            var t = new Label
            {
                Text = title,
                Font = TitleFont,
                ForeColor = PrimaryDark,
                AutoSize = true,
                Location = new Point(4, 6)
            };
            var s = new Label
            {
                Text = subtitle,
                Font = SmallFont,
                ForeColor = Muted,
                AutoSize = true,
                Location = new Point(6, 38)
            };
            p.Controls.Add(t);
            p.Controls.Add(s);
            return p;
        }

        public static Image DrawLogo(int width, int height)
        {
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                float s = System.Math.Min(width / 3.4f, height * 0.88f);
                float x = 6, y = (height - s) / 2f;
                using (var fill = new SolidBrush(Color.FromArgb(224, 242, 238)))
                    g.FillEllipse(fill, x, y, s, s);
                using (var teal = new SolidBrush(Primary))
                {
                    float t = s * 0.17f;
                    g.FillRectangle(teal, x + s * 0.41f, y + s * 0.16f, t, s * 0.68f);
                    g.FillRectangle(teal, x + s * 0.16f, y + s * 0.41f, s * 0.68f, t);
                }
                using (var gold = new SolidBrush(Accent))
                {
                    var leaf = new GraphicsPath();
                    leaf.AddBezier(x + s * 0.92f, y + s * 0.62f, x + s * 1.05f, y + s * 0.2f, x + s * 1.35f, y + s * 0.35f, x + s * 1.12f, y + s * 0.78f);
                    g.FillPath(gold, leaf);
                }
                using (var brush = new SolidBrush(PrimaryDark))
                using (var font = new Font("Segoe UI Semibold", System.Math.Max(10, height * 0.28f), FontStyle.Bold))
                    g.DrawString("Èclat & Soin", font, brush, x + s * 1.18f, y + s * 0.28f);
            }
            return bmp;
        }

        private static Button BaseButton(string text, Color back, Color fore, Font font, int height)
        {
            var b = new Button
            {
                Text = text,
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = font,
                Height = height,
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 0, 10, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private static void WireHover(Button b, Color normal, Color hover)
        {
            b.MouseEnter += (s, e) => b.BackColor = hover;
            b.MouseLeave += (s, e) => b.BackColor = normal;
        }
    }
}
