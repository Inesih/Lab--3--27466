using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IncidentCommand
{
    public class MainForm : Form
    {
        public static bool LoggedOut;

        Panel area, navPanel;
        Label hTitle, hSub;
        FlowLayoutPanel hBtns;
        Button notifBtn, curBtn;
        int navY = 0;

        public MainForm()
        {
            Text = "Incident Command - Primate Safaris";
            Font = Ui.F;
            BackColor = Ui.Bg;
            ClientSize = new Size(1280, 760);
            MinimumSize = new Size(1100, 680);
            StartPosition = FormStartPosition.CenterScreen;

            // Content (Fill control is added first, then the docked sidebar).
            Panel body = new Panel();
            body.Dock = DockStyle.Fill;
            body.Padding = new Padding(24, 16, 24, 16);
            area = new Panel();
            area.Dock = DockStyle.Fill;
            body.Controls.Add(area);
            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 84;
            hTitle = Ui.Lbl("", Ui.FH, Ui.Ink);
            hTitle.Location = new Point(0, 4);
            header.Controls.Add(hTitle);
            hSub = Ui.Lbl("", Ui.F, Ui.Mut);
            hSub.Location = new Point(2, 50);
            header.Controls.Add(hSub);
            hBtns = new FlowLayoutPanel();
            hBtns.Dock = DockStyle.Right;
            hBtns.Width = 560;
            hBtns.FlowDirection = FlowDirection.RightToLeft;
            hBtns.WrapContents = false;
            hBtns.Padding = new Padding(0, 10, 0, 0);
            header.Controls.Add(hBtns);
            body.Controls.Add(header);
            Controls.Add(body);

            Panel side = new Panel();
            side.Dock = DockStyle.Left;
            side.Width = 230;
            side.BackColor = Ui.Dark;
            navPanel = new Panel();
            navPanel.Dock = DockStyle.Fill;
            side.Controls.Add(navPanel);
            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 120;
            Label un = Ui.Lbl(Session.Me.FullName, Ui.FB, Color.White);
            un.Location = new Point(18, 12);
            bottom.Controls.Add(un);
            Label ur = Ui.Lbl(Session.Me.Role, Ui.FS, Color.FromArgb(170, 200, 185));
            ur.Location = new Point(18, 36);
            bottom.Controls.Add(ur);
            Button so = Ui.Btn("Sign out", false);
            so.Location = new Point(18, 66);
            so.Width = 190;
            so.Click += delegate
            {
                Store.Log(Session.Me.Username, "Signed out");
                LoggedOut = true;
                Close();
            };
            bottom.Controls.Add(so);
            side.Controls.Add(bottom);
            Panel logo = new Panel();
            logo.Dock = DockStyle.Top;
            logo.Height = 92;
            Label l1 = Ui.Lbl("Incident Command", new Font("Segoe UI Semibold", 14f), Color.White);
            l1.Location = new Point(18, 22);
            logo.Controls.Add(l1);
            Label l2 = Ui.Lbl("Primate Safaris Limited", Ui.FS, Color.FromArgb(170, 200, 185));
            l2.Location = new Point(20, 54);
            logo.Controls.Add(l2);
            side.Controls.Add(logo);
            Controls.Add(side);

            Button first = AddNav("Dashboard", Dashboard);
            AddNav("Incidents", Incidents);
            AddNav("Trips and travelers", Trips);
            notifBtn = AddNav("Notifications", Notifications);
            if (Session.CanManage || Session.Me.Role == Roles.View) AddNav("Reports", Reports);
            if (Session.IsAdmin) AddNav("Users", Users);
            if (Session.CanManage) AddNav("Audit log", AuditLog);
            Select(first);
            Dashboard();
            UpdateBadge();
        }

        // ------------------------------------------------------ navigation
        Button AddNav(string text, Action show)
        {
            Button b = new Button();
            b.Text = "   " + text;
            b.Tag = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.Font = Ui.F;
            b.ForeColor = Color.White;
            b.BackColor = Ui.Dark;
            b.Size = new Size(230, 46);
            b.Location = new Point(0, navY);
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
            b.Click += delegate
            {
                Select(b);
                show();
                UpdateBadge();
            };
            navPanel.Controls.Add(b);
            navY += 46;
            return b;
        }

        void Select(Button b)
        {
            if (curBtn != null) curBtn.BackColor = Ui.Dark;
            curBtn = b;
            b.BackColor = Ui.Acc;
        }

        void UpdateBadge()
        {
            int n = Store.D.Notes.Count(x => x.To == Session.Me.Username && !x.Read);
            notifBtn.Text = "   Notifications" + (n > 0 ? "  (" + n + ")" : "");
        }

        void Start(string title, string sub)
        {
            foreach (Control c in area.Controls.Cast<Control>().ToList()) c.Dispose();
            foreach (Control c in hBtns.Controls.Cast<Control>().ToList()) c.Dispose();
            hTitle.Text = title;
            hSub.Text = sub;
        }

        void HeadBtn(string text, bool primary, Action a)
        {
            Button b = Ui.Btn(text, primary);
            b.Margin = new Padding(8, 0, 0, 0);
            b.Click += delegate { a(); };
            hBtns.Controls.Add(b);
        }

        void OpenIncident(Incident i)
        {
            using (DetailDialog d = new DetailDialog(i)) { d.ShowDialog(this); }
            UpdateBadge();
        }

        void NewIncident(Action after)
        {
            using (IncidentDialog d = new IncidentDialog(null))
            {
                if (d.ShowDialog(this) == DialogResult.OK) after();
            }
            UpdateBadge();
        }

        // ------------------------------------------------------- dashboard
        void Dashboard()
        {
            Start("Dashboard", "Welcome, " + Session.Me.FullName + ". Here is the current safety picture.");
            if (Session.CanReport) HeadBtn("Report incident", true, delegate { NewIncident(Dashboard); });

            List<Incident> vis = Store.D.Incidents.Where(Store.CanSee).ToList();
            DataGridView g = Ui.Grid();
            Ui.Cols(g, "Ref", "Title", "Location", "Priority", "Status", "Responsible", "Reported");
            Ui.Weight(g, "Title", 150);
            Ui.Weight(g, "Location", 120);
            Ui.Weight(g, "Ref", 55);
            foreach (Incident i in vis.Where(x => (x.Status == "Reported" || x.Status == "In progress") && (x.Priority == "Urgent" || x.Priority == "High"))
                                      .OrderByDescending(x => Array.IndexOf(Lists.Priorities, x.Priority)).ThenBy(x => x.When))
                Ui.Add(g, i, i.Ref, i.Title, i.Location, i.Priority, i.Status, Store.Name(i.Assignee), i.When.ToString("dd MMM HH:mm"));
            g.CellDoubleClick += delegate (object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex < 0) return;
                Incident i = g.Rows[e.RowIndex].Tag as Incident;
                if (i != null) { OpenIncident(i); Dashboard(); }
            };
            area.Controls.Add(g);

            Label sub = Ui.Lbl("Needs attention: open urgent and high priority incidents (double-click to open)", Ui.FB, Ui.Ink);
            sub.AutoSize = false;
            sub.Dock = DockStyle.Top;
            sub.Height = 40;
            sub.TextAlign = ContentAlignment.BottomLeft;
            area.Controls.Add(sub);

            FlowLayoutPanel cards = new FlowLayoutPanel();
            cards.Dock = DockStyle.Top;
            cards.Height = 98;
            cards.WrapContents = false;
            cards.Controls.Add(Ui.Card(vis.Count.ToString(), "All incidents", Ui.Dark));
            cards.Controls.Add(Ui.Card(vis.Count(x => x.Status == "Reported").ToString(), "Reported", Ui.StatusColor("Reported")));
            cards.Controls.Add(Ui.Card(vis.Count(x => x.Status == "In progress").ToString(), "In progress", Ui.StatusColor("In progress")));
            cards.Controls.Add(Ui.Card(vis.Count(x => x.Status == "Resolved").ToString(), "Resolved", Ui.StatusColor("Resolved")));
            cards.Controls.Add(Ui.Card(vis.Count(x => x.Status == "Closed").ToString(), "Closed", Ui.Mut));
            area.Controls.Add(cards);
        }

        // ------------------------------------------------------- incidents
        void Incidents()
        {
            Start("Incidents", "Search, filter and open incident reports.");
            DataGridView g = Ui.Grid();
            Ui.Cols(g, "Ref", "Title", "Type", "Location", "Priority", "Status", "Responsible", "Date");
            Ui.Weight(g, "Title", 150);
            Ui.Weight(g, "Location", 120);
            Ui.Weight(g, "Ref", 55);
            area.Controls.Add(g);

            Panel bar = new Panel();
            bar.Dock = DockStyle.Top;
            bar.Height = 74;
            TextBox tbS = new TextBox();
            Ui.Row(bar, "Search title, location or reference", tbS, 0, 0, 230);
            List<string> tl = new List<string> { "All types" }; tl.AddRange(Lists.Types);
            ComboBox cbT = Ui.Combo(tl);
            Ui.Row(bar, "Type", cbT, 242, 0, 140);
            List<string> sl = new List<string> { "All statuses" }; sl.AddRange(Lists.Statuses);
            ComboBox cbS = Ui.Combo(sl);
            Ui.Row(bar, "Status", cbS, 394, 0, 130);
            DateTimePicker dF = new DateTimePicker();
            dF.Format = DateTimePickerFormat.Short;
            dF.ShowCheckBox = true;
            dF.Checked = false;
            Ui.Row(bar, "From date", dF, 536, 0, 130);
            DateTimePicker dT = new DateTimePicker();
            dT.Format = DateTimePickerFormat.Short;
            dT.ShowCheckBox = true;
            dT.Checked = false;
            Ui.Row(bar, "To date", dT, 678, 0, 130);
            area.Controls.Add(bar);

            Action refresh = null;
            refresh = delegate
            {
                g.Rows.Clear();
                string q = tbS.Text.Trim().ToLower();
                foreach (Incident i in Store.D.Incidents.Where(Store.CanSee).OrderByDescending(x => x.When))
                {
                    if (q.Length > 0 && !(i.Title + " " + i.Location + " " + i.Ref).ToLower().Contains(q)) continue;
                    if (cbT.SelectedIndex > 0 && i.Type != (string)cbT.SelectedItem) continue;
                    if (cbS.SelectedIndex > 0 && i.Status != (string)cbS.SelectedItem) continue;
                    if (dF.Checked && i.When.Date < dF.Value.Date) continue;
                    if (dT.Checked && i.When.Date > dT.Value.Date) continue;
                    Ui.Add(g, i, i.Ref, i.Title, i.Type, i.Location, i.Priority, i.Status, Store.Name(i.Assignee), i.When.ToString("dd MMM yyyy HH:mm"));
                }
                if (g.Rows.Count > 0) g.ClearSelection();
            };
            tbS.TextChanged += delegate { refresh(); };
            cbT.SelectedIndexChanged += delegate { refresh(); };
            cbS.SelectedIndexChanged += delegate { refresh(); };
            dF.ValueChanged += delegate { refresh(); };
            dT.ValueChanged += delegate { refresh(); };
            g.CellDoubleClick += delegate (object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex < 0) return;
                Incident i = g.Rows[e.RowIndex].Tag as Incident;
                if (i != null) { OpenIncident(i); refresh(); }
            };
            if (Session.CanReport) HeadBtn("New incident", true, delegate { NewIncident(refresh); });
            refresh();
        }

        // ------------------------------------------------ trips and travelers
        void Trips()
        {
            Start("Trips and travelers", "Link incidents to trips. Only the traveler details needed for handling an incident are shown.");
            DataGridView g = Ui.Grid();
            Ui.Cols(g, "Code", "Trip", "Start date", "Guide", "Travelers", "Incidents");
            Ui.Weight(g, "Trip", 140);
            Ui.Weight(g, "Travelers", 60);
            Ui.Weight(g, "Incidents", 60);
            area.Controls.Add(g);

            TextBox tb = new TextBox();
            tb.Multiline = true;
            tb.ReadOnly = true;
            tb.BackColor = Color.White;
            tb.ScrollBars = ScrollBars.Vertical;
            tb.Dock = DockStyle.Bottom;
            tb.Height = 170;
            area.Controls.Add(tb);

            Action refresh = null;
            refresh = delegate
            {
                g.Rows.Clear();
                foreach (Trip t in Store.D.Trips)
                {
                    int n = Store.D.Incidents.Count(x => x.TripCode == t.Code && Store.CanSee(x));
                    int tc = string.IsNullOrWhiteSpace(t.Travelers) ? 0 : t.Travelers.Split(',').Length;
                    Ui.Add(g, t, t.Code, t.Name, t.StartDate.ToString("dd MMM yyyy"), t.Guide, tc, n);
                }
                if (g.Rows.Count > 0) g.ClearSelection();
                tb.Text = "Select a trip to see its travelers and linked incidents.";
            };
            g.SelectionChanged += delegate
            {
                if (g.SelectedRows.Count == 0) return;
                Trip t = g.SelectedRows[0].Tag as Trip;
                if (t == null) return;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(t.Code + "  " + t.Name);
                sb.AppendLine("Travelers: " + (string.IsNullOrWhiteSpace(t.Travelers) ? "none recorded" : t.Travelers));
                sb.AppendLine();
                sb.AppendLine("Linked incidents:");
                bool any = false;
                foreach (Incident i in Store.D.Incidents.Where(x => x.TripCode == t.Code && Store.CanSee(x)))
                {
                    sb.AppendLine("  " + i.Ref + "  " + i.Title + "  (" + i.Priority + ", " + i.Status + ")");
                    any = true;
                }
                if (!any) sb.AppendLine("  None");
                tb.Text = sb.ToString();
            };
            if (Session.CanManage)
            {
                HeadBtn("Edit trip", false, delegate
                {
                    if (g.SelectedRows.Count == 0) { Ui.Info(this, "Select a trip first."); return; }
                    using (TripDialog d = new TripDialog(g.SelectedRows[0].Tag as Trip))
                    {
                        if (d.ShowDialog(this) == DialogResult.OK) refresh();
                    }
                });
                HeadBtn("Add trip", true, delegate
                {
                    using (TripDialog d = new TripDialog(null))
                    {
                        if (d.ShowDialog(this) == DialogResult.OK) refresh();
                    }
                });
            }
            refresh();
        }

        // --------------------------------------------------- notifications
        void Notifications()
        {
            Start("Notifications", "Assignments, status changes and updates that involve you.");
            DataGridView g = Ui.Grid();
            Ui.Cols(g, "Time", "Message", "State");
            Ui.Weight(g, "Time", 22);
            Ui.Weight(g, "Message", 100);
            Ui.Weight(g, "State", 15);
            area.Controls.Add(g);
            Action refresh = null;
            refresh = delegate
            {
                g.Rows.Clear();
                foreach (Note n in Store.D.Notes.Where(x => x.To == Session.Me.Username).OrderByDescending(x => x.Time))
                {
                    Ui.Add(g, n, n.Time.ToString("dd MMM yyyy HH:mm"), n.Text, n.Read ? "Read" : "Unread");
                    if (!n.Read) g.Rows[g.Rows.Count - 1].DefaultCellStyle.Font = Ui.FB;
                }
                if (g.Rows.Count > 0) g.ClearSelection();
                UpdateBadge();
            };
            g.CellDoubleClick += delegate (object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex < 0) return;
                Note n = g.Rows[e.RowIndex].Tag as Note;
                if (n != null) { n.Read = true; Store.Save(); refresh(); }
            };
            HeadBtn("Mark all as read", true, delegate
            {
                foreach (Note n in Store.D.Notes.Where(x => x.To == Session.Me.Username)) n.Read = true;
                Store.Save();
                refresh();
            });
            refresh();
        }

        // ------------------------------------------------------------ reports
        static string Q(string s)
        {
            return "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
        }

        void Reports()
        {
            Start("Reports", "Summary of incidents for a selected period. Change a filter to update the report.");
            DataGridView g = Ui.Grid();
            Ui.Cols(g, "Ref", "Title", "Type", "Location", "Priority", "Status", "Responsible", "Date");
            Ui.Weight(g, "Title", 150);
            Ui.Weight(g, "Location", 120);
            Ui.Weight(g, "Ref", 55);
            area.Controls.Add(g);

            Label sum = Ui.Lbl("", Ui.FB, Ui.Ink);
            sum.AutoSize = false;
            sum.Dock = DockStyle.Top;
            sum.Height = 40;
            sum.TextAlign = ContentAlignment.BottomLeft;
            area.Controls.Add(sum);

            Panel bar = new Panel();
            bar.Dock = DockStyle.Top;
            bar.Height = 74;
            DateTimePicker dF = new DateTimePicker();
            dF.Format = DateTimePickerFormat.Short;
            dF.Value = DateTime.Today.AddDays(-30);
            Ui.Row(bar, "From date", dF, 0, 0, 130);
            DateTimePicker dT = new DateTimePicker();
            dT.Format = DateTimePickerFormat.Short;
            dT.Value = DateTime.Today;
            Ui.Row(bar, "To date", dT, 142, 0, 130);
            List<string> tl = new List<string> { "All types" }; tl.AddRange(Lists.Types);
            ComboBox cbT = Ui.Combo(tl);
            Ui.Row(bar, "Type", cbT, 284, 0, 150);
            List<string> sl = new List<string> { "All statuses" }; sl.AddRange(Lists.Statuses);
            ComboBox cbS = Ui.Combo(sl);
            Ui.Row(bar, "Status", cbS, 446, 0, 130);
            TextBox tbL = new TextBox();
            Ui.Row(bar, "Location contains", tbL, 588, 0, 170);
            area.Controls.Add(bar);

            List<Incident> cur = new List<Incident>();
            Action refresh = null;
            refresh = delegate
            {
                cur.Clear();
                g.Rows.Clear();
                string loc = tbL.Text.Trim().ToLower();
                foreach (Incident i in Store.D.Incidents.Where(Store.CanSee).OrderByDescending(x => x.When))
                {
                    if (i.When.Date < dF.Value.Date || i.When.Date > dT.Value.Date) continue;
                    if (cbT.SelectedIndex > 0 && i.Type != (string)cbT.SelectedItem) continue;
                    if (cbS.SelectedIndex > 0 && i.Status != (string)cbS.SelectedItem) continue;
                    if (loc.Length > 0 && !i.Location.ToLower().Contains(loc)) continue;
                    cur.Add(i);
                    Ui.Add(g, i, i.Ref, i.Title, i.Type, i.Location, i.Priority, i.Status, Store.Name(i.Assignee), i.When.ToString("dd MMM yyyy HH:mm"));
                }
                if (g.Rows.Count > 0) g.ClearSelection();
                sum.Text = "Incidents in period: " + cur.Count + "     Reported " + cur.Count(x => x.Status == "Reported")
                    + "  |  In progress " + cur.Count(x => x.Status == "In progress")
                    + "  |  Resolved " + cur.Count(x => x.Status == "Resolved")
                    + "  |  Closed " + cur.Count(x => x.Status == "Closed");
            };
            dF.ValueChanged += delegate { refresh(); };
            dT.ValueChanged += delegate { refresh(); };
            cbT.SelectedIndexChanged += delegate { refresh(); };
            cbS.SelectedIndexChanged += delegate { refresh(); };
            tbL.TextChanged += delegate { refresh(); };

            HeadBtn("Export CSV", true, delegate
            {
                if (cur.Count == 0) { Ui.Info(this, "There are no incidents to export for these filters."); return; }
                using (SaveFileDialog sd = new SaveFileDialog())
                {
                    sd.Filter = "CSV file (Excel)|*.csv";
                    sd.FileName = "incident-report-" + DateTime.Today.ToString("yyyy-MM-dd") + ".csv";
                    if (sd.ShowDialog(this) != DialogResult.OK) return;
                    try
                    {
                        List<string> lines = new List<string>();
                        lines.Add("Ref,Title,Type,Location,Priority,Status,Responsible,Date,Trip,Description");
                        foreach (Incident i in cur)
                            lines.Add(string.Join(",", new string[] { Q(i.Ref), Q(i.Title), Q(i.Type), Q(i.Location), Q(i.Priority), Q(i.Status),
                                Q(Store.Name(i.Assignee)), Q(i.When.ToString("yyyy-MM-dd HH:mm")), Q(i.TripCode), Q(i.Description) }));
                        File.WriteAllLines(sd.FileName, lines, new UTF8Encoding(true));
                        Store.Log(Session.Me.Username, "Exported incident report");
                        Ui.Info(this, "Report saved to " + sd.FileName);
                    }
                    catch (Exception ex)
                    {
                        Ui.Warn(this, "The file could not be saved: " + ex.Message);
                    }
                }
            });
            refresh();
        }

        // -------------------------------------------------------------- users
        void Users()
        {
            Start("Users", "Create accounts, change roles and deactivate access.");
            DataGridView g = Ui.Grid();
            Ui.Cols(g, "Name", "Username", "Email", "Role", "Account");
            Ui.Weight(g, "Email", 150);
            area.Controls.Add(g);
            Action refresh = null;
            refresh = delegate
            {
                g.Rows.Clear();
                foreach (User u in Store.D.Users)
                    Ui.Add(g, u, u.FullName, u.Username, u.Email, u.Role, u.Active ? "Active" : "Deactivated");
                if (g.Rows.Count > 0) g.ClearSelection();
            };
            Func<User> picked = delegate
            {
                if (g.SelectedRows.Count == 0) { Ui.Info(this, "Select a user first."); return null; }
                return g.SelectedRows[0].Tag as User;
            };
            HeadBtn("Reset demo data", false, delegate
            {
                if (!Ui.Ask(this, "Replace all data with the demo data? Incidents, trips and users you added will be lost.")) return;
                string me = Session.Me.Username;
                Store.Seed();
                Store.Save();
                Session.Me = Store.D.Users.First(x => x.Username == me);
                refresh();
                Ui.Info(this, "Demo data restored.");
            });
            HeadBtn("Activate or deactivate", false, delegate
            {
                User u = picked();
                if (u == null) return;
                if (u == Session.Me) { Ui.Warn(this, "You cannot deactivate your own account."); return; }
                u.Active = !u.Active;
                Store.Log(Session.Me.Username, (u.Active ? "Activated " : "Deactivated ") + u.Username);
                refresh();
            });
            HeadBtn("Edit user", false, delegate
            {
                User u = picked();
                if (u == null) return;
                using (UserDialog d = new UserDialog(u)) { if (d.ShowDialog(this) == DialogResult.OK) refresh(); }
            });
            HeadBtn("Add user", true, delegate
            {
                using (UserDialog d = new UserDialog(null)) { if (d.ShowDialog(this) == DialogResult.OK) refresh(); }
            });
            refresh();
        }

        // ---------------------------------------------------------- audit log
        void AuditLog()
        {
            Start("Audit log", "Important account and incident activity, newest first.");
            DataGridView g = Ui.Grid();
            Ui.Cols(g, "Time", "User", "Action");
            Ui.Weight(g, "Time", 25);
            Ui.Weight(g, "User", 25);
            Ui.Weight(g, "Action", 100);
            area.Controls.Add(g);
            TextBox tb = new TextBox();
            Panel bar = new Panel();
            bar.Dock = DockStyle.Top;
            bar.Height = 74;
            Ui.Row(bar, "Filter by user or action", tb, 0, 0, 300);
            area.Controls.Add(bar);
            Action refresh = delegate
            {
                g.Rows.Clear();
                string q = tb.Text.Trim().ToLower();
                foreach (AuditEntry a in Store.D.Audit.OrderByDescending(x => x.Time).Take(500))
                {
                    if (q.Length > 0 && !(Store.Name(a.User) + " " + a.User + " " + a.Action).ToLower().Contains(q)) continue;
                    Ui.Add(g, a, a.Time.ToString("dd MMM yyyy HH:mm:ss"), Store.Name(a.User), a.Action);
                }
                if (g.Rows.Count > 0) g.ClearSelection();
            };
            tb.TextChanged += delegate { refresh(); };
            refresh();
        }
    }
}
