INCIDENT COMMAND - Real-Time Incident Command & Traveler Safety Module
Class project, AUCA. Windows desktop application (C# Windows Forms, .NET 8).

HOW TO RUN
1. Install Visual Studio 2022 (17.8 or newer) with the workload
   ".NET desktop development". Community edition is free.
2. Unzip this folder.
3. Double-click IncidentCommand.csproj (or in Visual Studio: File > Open > Project/Solution).
4. Press F5 (or the green Start button).
No packages to install and no database to set up.

DEMO ACCOUNTS (username / password)
  admin    / Admin@123     Administrator
  manager  / Manager@123   Manager or Coordinator
  guide    / Guide@123     Staff or Guide
  viewer   / Viewer@123    Viewer (read only)

WHAT EACH ROLE CAN DO
  Administrator: everything, plus user accounts and the audit log.
  Manager:       view all incidents, assign, change status, close, trips, reports, audit log.
  Staff:         report incidents, see only incidents they reported or are assigned to,
                 add updates, mark assigned incidents In progress or Resolved.
  Viewer:        read-only access to incidents, trips and reports.

SUGGESTED DEMO FLOW
  1. Sign in as guide: Report incident, submit it, note the reference number.
  2. Sign out, sign in as manager: Notifications shows the new incident.
     Open it, assign a responsible user, set status to In progress, add an update.
  3. Resolve it, then close it as manager. Open Reports and Export CSV.
  4. Sign in as admin: Users and Audit log.

DATA
  Saved automatically to %LocalAppData%\IncidentCommand\data.xml
  To start fresh: sign in as admin > Users > Reset demo data.

NOT INCLUDED (to confirm with the instructor): email/SMS sending, map-based location,
password recovery. Notifications are inside the system only. Attachments store the file path.
