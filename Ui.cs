using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace IncidentCommand
{
    public static class Ui
    {
        public static readonly Color Dark = Color.FromArgb(18, 55, 42);
        public static readonly Color Acc = Color.FromArgb(31, 90, 67);
        public static readonly Color Bg = Color.FromArgb(238, 242, 238);
        public static readonly Color Line = Color.FromArgb(213, 221, 214);
        public static readonly Color Mut = Color.FromArgb(93, 107, 99);
        public static readonly Color Ink = Color.FromArgb(22, 35, 29);

        public static readonly Font F = new Font("Segoe UI", 10f);
        public static readonly Font FB = new Font("Segoe UI", 10f, FontStyle.Bold);
        public static readonly Font FS = new Font("Segoe UI", 9f);
        public static readonly Font FT = new Font("Segoe UI Semibold", 15f);
        public static readonly Font FH = new Font("Segoe UI Semibold", 20f);

        public static Color PriColor(string p)
        {
            switch (p)
            {
                case "Urgent": return Color.FromArgb(200, 55, 45);
                case "High": return Color.FromArgb(212, 105, 31);
                case "Medium": return Color.FromArgb(185, 138, 12);
                default: return Color.FromArgb(63, 138, 99);
            }
        }

        public static Color StatusColor(string s)
        {
            switch (s)
            {
                case "Reported": return Color.FromArgb(185, 100, 12);
                case "In progress": return Color.FromArgb(30, 100, 170);
                case "Resolved": return Color.FromArgb(63, 138, 99);
                default: return Mut;
            }
        }

        public static void Setup(Form f, string title, int w, int h)
        {
            f.Text = title;
            f.ClientSize = new Size(w, h);
            f.FormBorderStyle = FormBorderStyle.FixedDialog;
            f.MaximizeBox = false;
            f.MinimizeBox = false;
            f.ShowInTaskbar = false;
            f.StartPosition = FormStartPosition.CenterParent;
            f.Font = F;
            f.BackColor = Bg;
        }

        public static Button Btn(string text, bool primary)
        {
            Button b = new Button();
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.Font = FB;
            b.Height = 36;
            b.Width = Math.Max(96, text.Length * 9 + 28);
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = primary ? Acc : Line;
            b.BackColor = primary ? Acc : Color.White;
            b.ForeColor = primary ? Color.White : Dark;
            return b;
        }

        public static Label Lbl(string text, Font font, Color color)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.Font = font;
            l.ForeColor = color;
            l.BackColor = Color.Transparent;
            return l;
        }

        public static Label Lbl(string text)
        {
            return Lbl(text, F, Ink);
        }

        public static ComboBox Combo(IEnumerable<string> items)
        {
            ComboBox c = new ComboBox();
            c.DropDownStyle = ComboBoxStyle.DropDownList;
            c.Font = F;
            foreach (string s in items) c.Items.Add(s);
            if (c.Items.Count > 0) c.SelectedIndex = 0;
            return c;
        }

        // Places a small caption above a control.
        public static void Row(Control parent, string caption, Control c, int x, int y, int w)
        {
            Label l = Lbl(caption, FS, Mut);
            l.Location = new Point(x, y);
            parent.Controls.Add(l);
            c.Location = new Point(x, y + 22);
            c.Width = w;
            parent.Controls.Add(c);
        }

        public static Panel Card(string value, string caption, Color accent)
        {
            Panel p = new Panel();
            p.Size = new Size(152, 84);
            p.BackColor = Color.White;
            p.Margin = new Padding(0, 0, 10, 0);
            Panel stripe = new Panel();
            stripe.Dock = DockStyle.Left;
            stripe.Width = 6;
            stripe.BackColor = accent;
            p.Controls.Add(stripe);
            Label v = Lbl(value, FH, Ink);
            v.Location = new Point(16, 6);
            p.Controls.Add(v);
            Label c = Lbl(caption, FS, Mut);
            c.Location = new Point(18, 56);
            p.Controls.Add(c);
            return p;
        }

        public static DataGridView Grid()
        {
            DataGridView g = new DataGridView();
            g.Dock = DockStyle.Fill;
            g.ReadOnly = true;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;
            g.BackgroundColor = Color.White;
            g.BorderStyle = BorderStyle.None;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.GridColor = Line;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.ColumnHeadersDefaultCellStyle.BackColor = Dark;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Dark;
            g.ColumnHeadersDefaultCellStyle.Font = FB;
            g.ColumnHeadersHeight = 38;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.RowTemplate.Height = 34;
            g.DefaultCellStyle.Font = F;
            g.DefaultCellStyle.ForeColor = Ink;
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 232, 222);
            g.DefaultCellStyle.SelectionForeColor = Ink;
            return g;
        }

        public static void Cols(DataGridView g, params string[] names)
        {
            g.Columns.Clear();
            foreach (string n in names) g.Columns.Add(n, n);
        }

        public static void Weight(DataGridView g, string col, float w)
        {
            if (g.Columns.Contains(col)) g.Columns[col].FillWeight = w;
        }

        public static void Add(DataGridView g, object tag, params object[] cells)
        {
            int idx = g.Rows.Add(cells);
            DataGridViewRow r = g.Rows[idx];
            r.Tag = tag;
            for (int c = 0; c < g.Columns.Count; c++)
            {
                string n = g.Columns[c].Name;
                string v = Convert.ToString(r.Cells[c].Value);
                if (n == "Priority")
                {
                    r.Cells[c].Style.ForeColor = PriColor(v);
                    r.Cells[c].Style.Font = FB;
                }
                else if (n == "Status")
                {
                    r.Cells[c].Style.ForeColor = StatusColor(v);
                    r.Cells[c].Style.Font = FB;
                }
            }
        }

        public static void Info(IWin32Window w, string msg)
        {
            MessageBox.Show(w, msg, "Incident Command", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void Warn(IWin32Window w, string msg)
        {
            MessageBox.Show(w, msg, "Incident Command", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static bool Ask(IWin32Window w, string msg)
        {
            return MessageBox.Show(w, msg, "Incident Command", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }
    }
}
