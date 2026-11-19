using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace IncidentCommand
{
    public class User
    {
        public string Username;
        public string FullName;
        public string Email;
        public string Role;
        public string PasswordHash;
        public bool Active = true;
    }

    public class UpdateEntry
    {
        public DateTime Time;
        public string By;
        public string Text;
    }

    public class Incident
    {
        public int Id;
        public string Title;
        public string Description;
        public DateTime When;
        public string Location;
        public string Type;
        public string Priority;
        public string Status;
        public string TripCode;
        public string Travelers;
        public string Attachment;
        public string ReportedBy;
        public string Assignee;
        public List<UpdateEntry> Updates = new List<UpdateEntry>();
        public string Ref { get { return "INC-" + Id; } }
    }

    public class Trip
    {
        public string Code;
        public string Name;
        public DateTime StartDate;
        public string Guide;
        public string Travelers;
    }

    public class Note
    {
        public string To;
        public string Text;
        public DateTime Time;
        public bool Read;
    }

    public class AuditEntry
    {
        public DateTime Time;
        public string User;
        public string Action;
    }

    public class Db
    {
        public List<User> Users = new List<User>();
        public List<Incident> Incidents = new List<Incident>();
        public List<Trip> Trips = new List<Trip>();
        public List<Note> Notes = new List<Note>();
        public List<AuditEntry> Audit = new List<AuditEntry>();
        public int NextId = 1007;
    }

    public static class Roles
    {
        public const string Admin = "Administrator";
        public const string Mgr = "Manager";
        public const string Staff = "Staff";
        public const string View = "Viewer";
        public static readonly string[] All = { Admin, Mgr, Staff, View };
    }

    public static class Lists
    {
        public static readonly string[] Types = { "Accident", "Medical", "Security", "Vehicle breakdown", "Weather", "Wildlife", "Other" };
        public static readonly string[] Priorities = { "Low", "Medium", "High", "Urgent" };
        public static readonly string[] Statuses = { "Reported", "In progress", "Resolved", "Closed" };
    }

    public static class Session
    {
        public static User Me;
        public static bool IsAdmin { get { return Me != null && Me.Role == Roles.Admin; } }
        public static bool CanManage { get { return Me != null && (Me.Role == Roles.Admin || Me.Role == Roles.Mgr); } }
        public static bool CanReport { get { return Me != null && Me.Role != Roles.View; } }
    }

    public static class Store
    {
        public static Db D;
        static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IncidentCommand");
        static readonly string FilePath = Path.Combine(Dir, "data.xml");

        public static string Hash(string s)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(s ?? "")));
            }
        }

        public static void Load()
        {
            D = null;
            try
            {
                if (File.Exists(FilePath))
                {
                    using (FileStream fs = File.OpenRead(FilePath))
                    {
                        D = (Db)new XmlSerializer(typeof(Db)).Deserialize(fs);
                    }
                }
            }
            catch (Exception)
            {
                D = null;
            }
            if (D == null || D.Users == null || D.Users.Count == 0)
            {
                Seed();
                Save();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                using (FileStream fs = File.Create(FilePath))
                {
                    new XmlSerializer(typeof(Db)).Serialize(fs, D);
                }
            }
            catch (Exception)
            {
                // Saving is best effort so the app never crashes on a locked file.
            }
        }

        public static void Log(string user, string action)
        {
            D.Audit.Insert(0, new AuditEntry { Time = DateTime.Now, User = user, Action = action });
            Save();
        }

        public static void Notify(string to, string text)
        {
            if (string.IsNullOrEmpty(to)) return;
            D.Notes.Insert(0, new Note { To = to, Text = text, Time = DateTime.Now, Read = false });
        }

        public static void NotifyInvolved(Incident i, string text, bool includeManagers)
        {
            HashSet<string> set = new HashSet<string>();
            if (!string.IsNullOrEmpty(i.ReportedBy)) set.Add(i.ReportedBy);
            if (!string.IsNullOrEmpty(i.Assignee)) set.Add(i.Assignee);
            if (includeManagers)
            {
                foreach (User u in D.Users.Where(x => x.Active && x.Role == Roles.Mgr)) set.Add(u.Username);
            }
            if (Session.Me != null) set.Remove(Session.Me.Username);
            foreach (string name in set) Notify(name, text);
        }

        public static bool CanSee(Incident i)
        {
            User u = Session.Me;
            if (u == null) return false;
            if (u.Role == Roles.Staff) return i.ReportedBy == u.Username || i.Assignee == u.Username;
            return true;
        }

        public static string Name(string username)
        {
            if (string.IsNullOrEmpty(username)) return "Unassigned";
            User u = D.Users.FirstOrDefault(x => x.Username == username);
            return u == null ? username : u.FullName;
        }

        static User MkU(string user, string name, string email, string role, string pw)
        {
            return new User { Username = user, FullName = name, Email = email, Role = role, PasswordHash = Hash(pw), Active = true };
        }

        static Incident Inc(int id, string title, string desc, int minsAgo, string loc, string type, string pri, string status,
                            string trip, string trav, string by, string asg, params string[] ups)
        {
            Incident i = new Incident
            {
                Id = id, Title = title, Description = desc, When = DateTime.Now.AddMinutes(-minsAgo), Location = loc,
                Type = type, Priority = pri, Status = status, TripCode = trip, Travelers = trav, Attachment = "",
                ReportedBy = by, Assignee = asg
            };
            i.Updates.Add(new UpdateEntry { Time = i.When.AddMinutes(2), By = by, Text = "Incident reported." });
            for (int k = 0; k < ups.Length; k++)
            {
                DateTime t = i.When.AddMinutes(10 + k * 25);
                if (t > DateTime.Now) t = DateTime.Now;
                i.Updates.Add(new UpdateEntry { Time = t, By = asg ?? "manager", Text = ups[k] });
            }
            return i;
        }

        public static void Seed()
        {
            Db d = new Db();
            D = d;
            d.Users.Add(MkU("admin", "Aline Ishimwe", "aline.ishimwe@primatesafaris.example", Roles.Admin, "Admin@123"));
            d.Users.Add(MkU("manager", "Grace Mukamana", "grace.mukamana@primatesafaris.example", Roles.Mgr, "Manager@123"));
            d.Users.Add(MkU("guide", "Claudine Uwase", "claudine.uwase@primatesafaris.example", Roles.Staff, "Guide@123"));
            d.Users.Add(MkU("driver", "Patrick Niyonzima", "patrick.niyonzima@primatesafaris.example", Roles.Staff, "Driver@123"));
            d.Users.Add(MkU("viewer", "Eric Habimana", "eric.habimana@primatesafaris.example", Roles.View, "Viewer@123"));

            d.Trips.Add(new Trip { Code = "PS-GOR-014", Name = "Volcanoes gorilla trek", StartDate = DateTime.Today.AddDays(-2), Guide = "Claudine Uwase", Travelers = "Mr. Hartley, Mrs. Hartley, Ms. Moreau, Mr. Tanaka" });
            d.Trips.Add(new Trip { Code = "PS-NYU-007", Name = "Nyungwe chimpanzee trek", StartDate = DateTime.Today.AddDays(-1), Guide = "Claudine Uwase", Travelers = "Mr. Okafor, Ms. Lindqvist, Mr. Brown, Mrs. Brown, Ms. Patel, Mr. Rossi" });
            d.Trips.Add(new Trip { Code = "PS-AKG-021", Name = "Akagera 3-day safari", StartDate = DateTime.Today.AddDays(-1), Guide = "Patrick Niyonzima", Travelers = "Ms. Dubois, Mr. Schmidt, Mrs. Schmidt, Mr. Costa, Ms. Costa" });

            d.Incidents.Add(Inc(1005, "Minor collision with a motorbike", "One traveler reports neck pain. Police and a medic have been requested.", 35,
                "Kayonza to Akagera road", "Accident", "Urgent", "In progress", "PS-AKG-021", "Ms. Dubois", "guide", "manager",
                "Medic and police liaison contacted.", "Traveler is being taken to the nearest clinic."));
            d.Incidents.Add(Inc(1004, "Buffalo herd blocking the track", "Vehicle stopped at a safe distance. Nobody is outside the vehicle.", 75,
                "Akagera National Park", "Wildlife", "Medium", "In progress", "PS-AKG-021", "5 travelers", "driver", "driver",
                "Waiting for the herd to move. Ranger informed."));
            d.Incidents.Add(Inc(1003, "Heavy rain makes canopy trail unsafe", "Rangers closed the canopy walkway. The group is waiting at the visitor center.", 12,
                "Nyungwe Forest, Uwinka", "Weather", "High", "Reported", "PS-NYU-007", "6 travelers", "guide", null));
            d.Incidents.Add(Inc(1001, "Traveler with altitude sickness symptoms", "Traveler felt dizzy and nauseous on the descent. The guide stopped the group and called the office.", 190,
                "Volcanoes National Park, Musanze", "Medical", "High", "In progress", "PS-GOR-014", "Mr. and Mrs. Hartley", "guide", "manager",
                "Medic placed on standby at the park gate.", "Traveler is resting at the lodge. Symptoms are being monitored."));
            d.Incidents.Add(Inc(1002, "Safari vehicle overheated", "Vehicle overheated near Shyorongi. Group waited at a safe spot by the road.", 1500,
                "Kigali to Musanze road", "Vehicle breakdown", "Medium", "Closed", "PS-GOR-014", "All 4 travelers", "driver", "manager",
                "Replacement vehicle dispatched.", "Group resumed the trip. Vehicle sent to the garage.", "Closed after confirmation from the driver."));
            d.Incidents.Add(Inc(1006, "Lost passport at hotel", "Traveler could not find the passport after check-in.", 2900,
                "Rubavu hotel", "Security", "Low", "Closed", "PS-NYU-007", "Mr. Okafor", "guide", "manager",
                "Hotel searched. Passport found at reception.", "Closed. Passport returned to the traveler."));

            Notify("manager", "New Urgent Accident incident INC-1005: Minor collision with a motorbike");
            Notify("manager", "New High Weather incident INC-1003: Heavy rain makes canopy trail unsafe");
            Notify("guide", "INC-1005 is now In progress");
            d.Audit.Add(new AuditEntry { Time = DateTime.Now.AddMinutes(-35), User = "guide", Action = "Reported incident INC-1005" });
            d.Audit.Add(new AuditEntry { Time = DateTime.Now.AddMinutes(-12), User = "guide", Action = "Reported incident INC-1003" });
            d.NextId = 1007;
        }
    }
}
