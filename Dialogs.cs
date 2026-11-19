using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IncidentCommand
{
    // ---------------------------------------------------------------- Login
    public class LoginForm : Form
    {
        TextBox tbU, tbP;
        Label err;

        public LoginForm()
        {
            Ui.Setup(this, "Incident Command - Sign in", 780, 470);
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;

            Panel left = new Panel();
            left.Dock = DockStyle.Left;
            left.Width = 300;
            left.BackColor = Ui.Dark;
            Label t = Ui.Lbl("Incident Command", new Font("Segoe UI Semibold", 20f), Color.White);
            t.Location = new Point(28, 150);
            left.Controls.Add(t);
            Label s = Ui.Lbl("Real-time incident coordination and traveler safety for Primate Safaris Limited.", Ui.F, Color.FromArgb(200, 220, 208));
            s.AutoSize = false;
            s.Size = new Size(244, 90);
            s.Location = new Point(30, 198);
            left.Controls.Add(s);
            Controls.Add(left);

            Label h = Ui.Lbl("Sign in", Ui.FT, Ui.Ink);
            h.Location = new Point(340, 50);
            Controls.Add(h);

            tbU = new TextBox();
            Ui.Row(this, "Username", tbU, 340, 110, 400);
            tbP = new TextBox();
            tbP.UseSystemPasswordChar = true;
            Ui.Row(this, "Password", tbP, 340, 170, 400);

            err = Ui.Lbl("", Ui.FS, Color.FromArgb(200, 55, 45));
            err.Location = new Point(340, 226);
            Controls.Add(err);

            Button b = Ui.Btn("Sign in", true);
            b.Location = new Point(340, 252);
            b.Width = 400;
            b.Click += DoLogin;
            Controls.Add(b);
            AcceptButton = b;

            Label hint = Ui.Lbl("Demo accounts (username / password)\r\nadmin / Admin@123\r\nmanager / Manager@123\r\nguide / Guide@123\r\nviewer / Viewer@123", Ui.FS, Ui.Mut);
            hint.Location = new Point(340, 318);
            Controls.Add(hint);
        }

        void DoLogin(object sender, EventArgs e)
        {
            string name = tbU.Text.Trim();
            User u = Store.D.Users.FirstOrDefault(x => string.Equals(x.Username, name, StringComparison.OrdinalIgnoreCase));
            if (u == null || u.PasswordHash != Store.Hash(tbP.Text))
            {
                err.Text = "Incorrect username or password.";
                tbP.Clear();
                tbP.Focus();
                return;
            }
            if (!u.Active)
            {
                err.Text = "This account is deactivated. Contact the administrator.";
                return;
            }
            Session.Me = u;
            Store.Log(u.Username, "Signed in");
            DialogResult = DialogResult.OK;
        }
    }

    // ------------------------------------------------- New / edit incident
    public class IncidentDialog : Form
    {
        Incident inc;
        TextBox tbTitle, tbLoc, tbTrav, tbDesc, tbAtt;
        ComboBox cbType, cbPri, cbTrip;
        DateTimePicker dDate, dTime;

        public IncidentDialog(Incident existing)
        {
            inc = existing;
            Ui.Setup(this, existing == null ? "New incident report" : "Edit " + existing.Ref, 640, 532);

            tbTitle = new TextBox();
            Ui.Row(this, "Title *", tbTitle, 16, 12, 608);
            cbType = Ui.Combo(Lists.Types);
            Ui.Row(this, "Incident type", cbType, 16, 68, 190);
            cbPri = Ui.Combo(Lists.Priorities);
            cbPri.SelectedItem = "Medium";
            Ui.Row(this, "Priority", cbPri, 216, 68, 190);
            dDate = new DateTimePicker();
            dDate.Format = DateTimePickerFormat.Short;
            Ui.Row(this, "Date", dDate, 416, 68, 208);
            dTime = new DateTimePicker();
            dTime.Format = DateTimePickerFormat.Time;
            dTime.ShowUpDown = true;
            Ui.Row(this, "Time", dTime, 16, 124, 190);
            tbLoc = new TextBox();
            Ui.Row(this, "Location *", tbLoc, 216, 124, 408);

            List<string> trips = new List<string>();
            trips.Add("(no trip)");
            foreach (Trip tr in Store.D.Trips) trips.Add(tr.Code + "  " + tr.Name);
            cbTrip = Ui.Combo(trips);
            Ui.Row(this, "Trip", cbTrip, 16, 180, 300);
            tbTrav = new TextBox();
            Ui.Row(this, "Travelers involved", tbTrav, 326, 180, 298);
            cbTrip.SelectedIndexChanged += delegate
            {
                if (cbTrip.SelectedIndex > 0 && tbTrav.Text.Trim().Length == 0)
                    tbTrav.Text = Store.D.Trips[cbTrip.SelectedIndex - 1].Travelers;
            };

            tbDesc = new TextBox();
            tbDesc.Multiline = true;
            tbDesc.Height = 110;
            tbDesc.ScrollBars = ScrollBars.Vertical;
            Ui.Row(this, "Description *", tbDesc, 16, 236, 608);

            tbAtt = new TextBox();
            tbAtt.ReadOnly = true;
            Ui.Row(this, "Attachment (optional photo or document)", tbAtt, 16, 370, 480);
            Button br = Ui.Btn("Browse", false);
            br.Location = new Point(506, 392);
            br.Width = 118;
            br.Height = 27;
            br.Click += delegate
            {
                using (OpenFileDialog o = new OpenFileDialog())
                {
                    o.Filter = "Photos and documents|*.jpg;*.jpeg;*.png;*.pdf;*.doc;*.docx|All files|*.*";
                    if (o.ShowDialog(this) == DialogResult.OK) tbAtt.Text = o.FileName;
                }
            };
            Controls.Add(br);

            Button save = Ui.Btn("Save report", true);
            save.Location = new Point(398, 474);
            save.Click += DoSave;
            Controls.Add(save);
            Button cancel = Ui.Btn("Cancel", false);
            cancel.Location = new Point(524, 474);
            cancel.Width = 100;
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            Controls.Add(cancel);

            if (inc != null)
            {
                tbTitle.Text = inc.Title;
                cbType.SelectedItem = inc.Type;
                cbPri.SelectedItem = inc.Priority;
                dDate.Value = inc.When;
                dTime.Value = inc.When;
                tbLoc.Text = inc.Location;
                tbTrav.Text = inc.Travelers;
                tbDesc.Text = inc.Description;
                tbAtt.Text = inc.Attachment;
                for (int k = 0; k < Store.D.Trips.Count; k++)
                    if (Store.D.Trips[k].Code == inc.TripCode) cbTrip.SelectedIndex = k + 1;
            }
        }

        void DoSave(object sender, EventArgs e)
        {
            if (tbTitle.Text.Trim().Length == 0) { Ui.Warn(this, "Enter a title for the incident."); return; }
            if (tbLoc.Text.Trim().Length == 0) { Ui.Warn(this, "Enter the location of the incident."); return; }
            if (tbDesc.Text.Trim().Length == 0) { Ui.Warn(this, "Describe what happened."); return; }
            DateTime when = dDate.Value.Date + dTime.Value.TimeOfDay;
            string trip = cbTrip.SelectedIndex > 0 ? Store.D.Trips[cbTrip.SelectedIndex - 1].Code : "";
            string me = Session.Me.Username;

            if (inc == null)
            {
                Incident i = new Incident();
                i.Id = Store.D.NextId++;
                i.Status = "Reported";
                i.ReportedBy = me;
                Apply(i, when, trip);
                i.Updates.Add(new UpdateEntry { Time = DateTime.Now, By = me, Text = "Incident reported." });
                Store.D.Incidents.Insert(0, i);
                foreach (User m in Store.D.Users.Where(x => x.Active && x.Role == Roles.Mgr && x.Username != me))
                    Store.Notify(m.Username, "New " + i.Priority + " " + i.Type + " incident " + i.Ref + ": " + i.Title);
                Store.Log(me, "Reported incident " + i.Ref);
                Ui.Info(this, "Report saved. Reference number: " + i.Ref);
            }
            else
            {
                Apply(inc, when, trip);
                inc.Updates.Add(new UpdateEntry { Time = DateTime.Now, By = me, Text = "Report details edited." });
                Store.NotifyInvolved(inc, inc.Ref + " was edited", true);
                Store.Log(me, "Edited incident " + inc.Ref);
            }
            Store.Save();
            DialogResult = DialogResult.OK;
        }

        void Apply(Incident i, DateTime when, string trip)
        {
            i.Title = tbTitle.Text.Trim();
            i.Description = tbDesc.Text.Trim();
            i.When = when;
            i.Location = tbLoc.Text.Trim();
            i.Type = (string)cbType.SelectedItem;
            i.Priority = (string)cbPri.SelectedItem;
            i.TripCode = trip;
            i.Travelers = tbTrav.Text.Trim();
            i.Attachment = tbAtt.Text;
        }
    }

    // ------------------------------------------- Incident details / response
    public class DetailDialog : Form
    {
        Incident inc;
        Label lPri, lStat;
        Label[] info = new Label[8];
        DataGridView tl;
        ComboBox cbAsg, cbSt;
        TextBox tbUp, tbDesc;
        List<User> resp = new List<User>();
        bool isAssignee;

        public DetailDialog(Incident i)
        {
            inc = i;
            Ui.Setup(this, i.Ref + " - " + i.Title, 820, 760);
            isAssignee = i.Assignee == Session.Me.Username;

            Label title = Ui.Lbl(i.Title, Ui.FT, Ui.Ink);
            title.AutoSize = false;
            title.AutoEllipsis = true;
            title.Size = new Size(788, 34);
            title.Location = new Point(16, 12);
            Controls.Add(title);

            lPri = new Label();
            lPri.AutoSize = false;
            lPri.Size = new Size(90, 24);
            lPri.Location = new Point(16, 54);
            lPri.TextAlign = ContentAlignment.MiddleCenter;
            lPri.ForeColor = Color.White;
            lPri.Font = Ui.FB;
            Controls.Add(lPri);
            lStat = new Label();
            lStat.AutoSize = false;
            lStat.Size = new Size(110, 24);
            lStat.Location = new Point(114, 54);
            lStat.TextAlign = ContentAlignment.MiddleCenter;
            lStat.ForeColor = Color.White;
            lStat.Font = Ui.FB;
            Controls.Add(lStat);
            Label rf = Ui.Lbl(i.Ref + "   " + i.Type, Ui.F, Ui.Mut);
            rf.Location = new Point(236, 56);
            Controls.Add(rf);

            tbDesc = new TextBox();
            tbDesc.Multiline = true;
            tbDesc.ReadOnly = true;
            tbDesc.BackColor = Color.White;
            tbDesc.ScrollBars = ScrollBars.Vertical;
            tbDesc.Location = new Point(16, 90);
            tbDesc.Size = new Size(788, 58);
            Controls.Add(tbDesc);

            for (int k = 0; k < info.Length; k++)
            {
                info[k] = new Label();
                info[k].AutoSize = false;
                info[k].AutoEllipsis = true;
                info[k].Size = new Size(388, 24);
                info[k].Location = new Point(16 + (k % 2) * 400, 158 + (k / 2) * 26);
                info[k].ForeColor = Ui.Ink;
                Controls.Add(info[k]);
            }

            bool closed = i.Status == "Closed";
            bool canAssign = Session.CanManage && !closed;
            bool canStatus = (Session.CanManage || isAssignee) && !closed;
            bool canNote = Session.CanReport && !closed;
            bool canEdit = Session.CanReport && !closed && (Session.CanManage || i.ReportedBy == Session.Me.Username);
            int y = 270;

            if (canAssign || canStatus)
            {
                if (canAssign)
                {
                    resp = Store.D.Users.Where(x => x.Active && (x.Role == Roles.Staff || x.Role == Roles.Mgr)).ToList();
                    cbAsg = Ui.Combo(resp.Select(x => x.FullName + " (" + x.Role + ")"));
                    Ui.Row(this, "Responsible user", cbAsg, 16, y, 250);
                    for (int k = 0; k < resp.Count; k++)
                        if (resp[k].Username == i.Assignee) cbAsg.SelectedIndex = k;
                    Button ba = Ui.Btn("Assign", false);
                    ba.Location = new Point(274, y + 20);
                    ba.Width = 90;
                    ba.Click += DoAssign;
                    Controls.Add(ba);
                }
                if (canStatus)
                {
                    List<string> opts = Session.CanManage ? new List<string>(Lists.Statuses) : new List<string> { "In progress", "Resolved" };
                    cbSt = Ui.Combo(opts);
                    if (opts.Contains(i.Status)) cbSt.SelectedItem = i.Status;
                    Ui.Row(this, "Status", cbSt, 400, y, 190);
                    Button bs = Ui.Btn("Update status", true);
                    bs.Location = new Point(598, y + 20);
                    bs.Width = 150;
                    bs.Click += DoStatus;
                    Controls.Add(bs);
                }
                y += 66;
            }
            if (canNote)
            {
                tbUp = new TextBox();
                Ui.Row(this, "Add an update or record an action taken", tbUp, 16, y, 640);
                Button bu = Ui.Btn("Add update", true);
                bu.Location = new Point(666, y + 20);
                bu.Width = 138;
                bu.Click += DoNote;
                Controls.Add(bu);
                y += 66;
            }

            Label tlt = Ui.Lbl("Timeline", Ui.FB, Ui.Ink);
            tlt.Location = new Point(16, y);
            Controls.Add(tlt);
            tl = Ui.Grid();
            tl.Dock = DockStyle.None;
            tl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            tl.Location = new Point(16, y + 26);
            tl.Size = new Size(788, 760 - 62 - (y + 26));
            tl.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            tl.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            Ui.Cols(tl, "When", "By", "Update");
            Ui.Weight(tl, "When", 22);
            Ui.Weight(tl, "By", 22);
            Ui.Weight(tl, "Update", 70);
            Controls.Add(tl);

            Button close = Ui.Btn("Close", false);
            close.Location = new Point(704, 760 - 50);
            close.Width = 100;
            close.Click += delegate { DialogResult = DialogResult.OK; };
            Controls.Add(close);
            if (canEdit)
            {
                Button be = Ui.Btn("Edit report", false);
                be.Location = new Point(16, 760 - 50);
                be.Click += delegate
                {
                    using (IncidentDialog d = new IncidentDialog(inc))
                    {
                        if (d.ShowDialog(this) == DialogResult.OK) Redraw();
                    }
                };
                Controls.Add(be);
            }
            Redraw();
        }

        void Redraw()
        {
            lPri.Text = inc.Priority;
            lPri.BackColor = Ui.PriColor(inc.Priority);
            lStat.Text = inc.Status;
            lStat.BackColor = Ui.StatusColor(inc.Status);
            tbDesc.Text = inc.Description;
            Trip trip = Store.D.Trips.FirstOrDefault(x => x.Code == inc.TripCode);
            string[] vals =
            {
                "Location:  " + inc.Location,
                "Date and time:  " + inc.When.ToString("dd MMM yyyy HH:mm"),
                "Trip:  " + (trip == null ? "None" : trip.Code + " " + trip.Name),
                "Travelers:  " + (string.IsNullOrEmpty(inc.Travelers) ? "Not recorded" : inc.Travelers),
                "Reported by:  " + Store.Name(inc.ReportedBy),
                "Responsible user:  " + Store.Name(inc.Assignee),
                "Attachment:  " + (string.IsNullOrEmpty(inc.Attachment) ? "None" : Path.GetFileName(inc.Attachment)),
                "Type:  " + inc.Type
            };
            for (int k = 0; k < info.Length; k++) info[k].Text = vals[k];
            tl.Rows.Clear();
            foreach (UpdateEntry u in inc.Updates.OrderByDescending(x => x.Time))
                Ui.Add(tl, u, u.Time.ToString("dd MMM HH:mm"), Store.Name(u.By), u.Text);
            if (tl.Rows.Count > 0) tl.ClearSelection();
        }

        void AddEntry(string text)
        {
            inc.Updates.Add(new UpdateEntry { Time = DateTime.Now, By = Session.Me.Username, Text = text });
        }

        void DoAssign(object sender, EventArgs e)
        {
            if (cbAsg == null || cbAsg.SelectedIndex < 0) { Ui.Warn(this, "Choose a responsible user."); return; }
            User u = resp[cbAsg.SelectedIndex];
            if (u.Username == inc.Assignee) { Ui.Info(this, u.FullName + " is already responsible for this incident."); return; }
            inc.Assignee = u.Username;
            AddEntry("Assigned to " + u.FullName + ".");
            Store.Notify(u.Username, inc.Ref + " was assigned to you: " + inc.Title);
            Store.NotifyInvolved(inc, inc.Ref + " assigned to " + u.FullName, false);
            Store.Log(Session.Me.Username, "Assigned " + inc.Ref + " to " + u.Username);
            Store.Save();
            Redraw();
        }

        void DoStatus(object sender, EventArgs e)
        {
            string s = (string)cbSt.SelectedItem;
            if (s == null || s == inc.Status) { Ui.Info(this, "Choose a different status first."); return; }
            if (s == "In progress" && string.IsNullOrEmpty(inc.Assignee)) { Ui.Warn(this, "Assign a responsible user before starting the response."); return; }
            if (s == "Closed")
            {
                if (!Session.CanManage) { Ui.Warn(this, "Only a manager or administrator can close an incident."); return; }
                if (inc.Status != "Resolved") { Ui.Warn(this, "Mark the incident as Resolved before closing it."); return; }
                if (!Ui.Ask(this, "Close " + inc.Ref + "? Closed incidents can no longer be changed.")) return;
            }
            string old = inc.Status;
            inc.Status = s;
            AddEntry("Status changed from " + old + " to " + s + ".");
            Store.NotifyInvolved(inc, inc.Ref + " is now " + s, true);
            Store.Log(Session.Me.Username, inc.Ref + " status " + old + " -> " + s);
            Store.Save();
            Close2();
        }

        void Close2()
        {
            // Rebuild the dialog so the controls match the new status.
            DialogResult = DialogResult.OK;
        }

        void DoNote(object sender, EventArgs e)
        {
            string t = tbUp.Text.Trim();
            if (t.Length == 0) { Ui.Warn(this, "Write the update first."); return; }
            AddEntry(t);
            Store.NotifyInvolved(inc, "New update on " + inc.Ref + ": " + t, false);
            Store.Log(Session.Me.Username, "Update added to " + inc.Ref);
            Store.Save();
            tbUp.Clear();
            Redraw();
        }
    }

    // ------------------------------------------------------- Trip dialog
    public class TripDialog : Form
    {
        Trip trip;
        TextBox tbCode, tbName, tbGuide, tbTrav;
        DateTimePicker dStart;

        public TripDialog(Trip existing)
        {
            trip = existing;
            Ui.Setup(this, existing == null ? "Add trip" : "Edit trip", 520, 420);
            tbCode = new TextBox();
            Ui.Row(this, "Trip code *", tbCode, 16, 12, 220);
            dStart = new DateTimePicker();
            dStart.Format = DateTimePickerFormat.Short;
            Ui.Row(this, "Start date", dStart, 252, 12, 252);
            tbName = new TextBox();
            Ui.Row(this, "Trip name *", tbName, 16, 68, 488);
            tbGuide = new TextBox();
            Ui.Row(this, "Guide", tbGuide, 16, 124, 488);
            tbTrav = new TextBox();
            tbTrav.Multiline = true;
            tbTrav.Height = 100;
            tbTrav.ScrollBars = ScrollBars.Vertical;
            Ui.Row(this, "Travelers (names only, separated by commas)", tbTrav, 16, 180, 488);
            Button save = Ui.Btn("Save trip", true);
            save.Location = new Point(280, 360);
            save.Click += DoSave;
            Controls.Add(save);
            Button cancel = Ui.Btn("Cancel", false);
            cancel.Location = new Point(404, 360);
            cancel.Width = 100;
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            Controls.Add(cancel);
            if (trip != null)
            {
                tbCode.Text = trip.Code;
                tbCode.ReadOnly = true;
                tbName.Text = trip.Name;
                tbGuide.Text = trip.Guide;
                tbTrav.Text = trip.Travelers;
                dStart.Value = trip.StartDate;
            }
        }

        void DoSave(object sender, EventArgs e)
        {
            string code = tbCode.Text.Trim().ToUpper();
            if (code.Length == 0 || tbName.Text.Trim().Length == 0) { Ui.Warn(this, "Enter the trip code and name."); return; }
            if (trip == null)
            {
                if (Store.D.Trips.Any(x => x.Code == code)) { Ui.Warn(this, "A trip with this code already exists."); return; }
                trip = new Trip { Code = code };
                Store.D.Trips.Add(trip);
            }
            trip.Name = tbName.Text.Trim();
            trip.Guide = tbGuide.Text.Trim();
            trip.Travelers = tbTrav.Text.Trim();
            trip.StartDate = dStart.Value.Date;
            Store.Log(Session.Me.Username, "Saved trip " + trip.Code);
            DialogResult = DialogResult.OK;
        }
    }

    // ------------------------------------------------------- User dialog
    public class UserDialog : Form
    {
        User user;
        TextBox tbName, tbUser, tbMail, tbPass;
        ComboBox cbRole;
        CheckBox cbActive;

        public UserDialog(User existing)
        {
            user = existing;
            Ui.Setup(this, existing == null ? "Add user" : "Edit user", 520, 400);
            tbName = new TextBox();
            Ui.Row(this, "Full name *", tbName, 16, 12, 488);
            tbUser = new TextBox();
            Ui.Row(this, "Username *", tbUser, 16, 68, 236);
            tbMail = new TextBox();
            Ui.Row(this, "Email", tbMail, 268, 68, 236);
            cbRole = Ui.Combo(Roles.All);
            Ui.Row(this, "Role", cbRole, 16, 124, 236);
            tbPass = new TextBox();
            tbPass.UseSystemPasswordChar = true;
            Ui.Row(this, existing == null ? "Password * (at least 6 characters)" : "New password (leave blank to keep)", tbPass, 268, 124, 236);
            cbActive = new CheckBox();
            cbActive.Text = "Account is active";
            cbActive.Checked = true;
            cbActive.AutoSize = true;
            cbActive.Location = new Point(16, 188);
            Controls.Add(cbActive);
            Button save = Ui.Btn("Save user", true);
            save.Location = new Point(280, 340);
            save.Click += DoSave;
            Controls.Add(save);
            Button cancel = Ui.Btn("Cancel", false);
            cancel.Location = new Point(404, 340);
            cancel.Width = 100;
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            Controls.Add(cancel);
            if (user != null)
            {
                tbName.Text = user.FullName;
                tbUser.Text = user.Username;
                tbUser.ReadOnly = true;
                tbMail.Text = user.Email;
                cbRole.SelectedItem = user.Role;
                cbActive.Checked = user.Active;
            }
        }

        void DoSave(object sender, EventArgs e)
        {
            string uname = tbUser.Text.Trim().ToLower();
            if (tbName.Text.Trim().Length == 0 || uname.Length == 0) { Ui.Warn(this, "Enter the full name and username."); return; }
            if (tbMail.Text.Trim().Length > 0 && !tbMail.Text.Contains("@")) { Ui.Warn(this, "Enter a valid email address."); return; }
            string role = (string)cbRole.SelectedItem;
            if (user == null)
            {
                if (Store.D.Users.Any(x => x.Username == uname)) { Ui.Warn(this, "This username is already taken."); return; }
                if (tbPass.Text.Length < 6) { Ui.Warn(this, "The password must have at least 6 characters."); return; }
                user = new User { Username = uname };
                Store.D.Users.Add(user);
            }
            else
            {
                if (user == Session.Me && (role != Roles.Admin || !cbActive.Checked))
                {
                    Ui.Warn(this, "You cannot remove your own administrator access or deactivate your own account.");
                    return;
                }
                if (tbPass.Text.Length > 0 && tbPass.Text.Length < 6) { Ui.Warn(this, "The password must have at least 6 characters."); return; }
            }
            user.FullName = tbName.Text.Trim();
            user.Email = tbMail.Text.Trim();
            user.Role = role;
            user.Active = cbActive.Checked;
            if (tbPass.Text.Length > 0) user.PasswordHash = Store.Hash(tbPass.Text);
            Store.Log(Session.Me.Username, "Saved user " + user.Username + " (" + user.Role + ")");
            DialogResult = DialogResult.OK;
        }
    }
}
