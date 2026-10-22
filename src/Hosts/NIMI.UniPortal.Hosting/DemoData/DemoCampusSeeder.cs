using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Graduation;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Prerequisites.SetSubjectPrerequisites;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.RegisterForSubject;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.RegistrationWindows.CreateRegistrationWindow;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Enrollment;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Plans;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Plans;
using NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;
using Systems = NIMI.UniPortal.Modules.Platform.Contracts.AcademicSystems;
using System.Text;
using NIMI.UniPortal.Modules.Announcements.Application.Post;
using NIMI.UniPortal.Modules.Announcements.Domain;
using NIMI.UniPortal.Modules.Assignments.Application.Manage;
using NIMI.UniPortal.Modules.Materials.Application.Upload;
using NIMI.UniPortal.Modules.Payments.Application.Fees.CreateFeeItem;
using NIMI.UniPortal.Modules.Payments.Application.Ledger.RecordPayment;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateGroup;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateSubject;
using NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateTeachingAssignment;
using NIMI.UniPortal.Modules.Scheduling.Application.Facilities.CreateFacility;
using NIMI.UniPortal.Modules.Scheduling.Application.Terms.ActivateTerm;
using NIMI.UniPortal.Modules.Scheduling.Application.Terms.CreateTerm;
using NIMI.UniPortal.Modules.Scheduling.Application.Timetable.CreateScheduleSlot;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;
using NIMI.UniPortal.Modules.Users.Application.Accounts.CreateUser;
using NIMI.UniPortal.Modules.Users.Application.Auth.ChangePassword;
using NIMI.UniPortal.Modules.Users.Application.Organization.CreateDepartment;
using NIMI.UniPortal.Modules.Users.Application.Organization.CreateFaculty;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Persistence;
using AnnouncementViewer = NIMI.UniPortal.Modules.Announcements.Application.Common.Viewer;
using AssignmentViewer = NIMI.UniPortal.Modules.Assignments.Application.Common.Viewer;
using MaterialViewer = NIMI.UniPortal.Modules.Materials.Application.Common.Viewer;

namespace NIMI.UniPortal.Hosting.DemoData;

/// <summary>
/// Decision DEVDOCKER: a campus the frontend can show right away — two faculties, a running term with a timetable,
/// professors and students (one demo password, one student still on a forced change), registrations, fees and a
/// payment, announcements, an assignment and a material. Everything goes through the real commands, so every rule
/// holds and the usual notifications and audit entries appear. Runs only on an empty campus (no term, no students).
/// Decision LIFECYCLEDEPT: Engineering runs credit hours (students registered in an open window) and Commerce runs
/// year groups (a study plan and a generated term), so both academic systems are on show.
/// </summary>
public sealed class DemoCampusSeeder(
    ISender sender,
    IUsersApi users,
    ISchedulingApi scheduling,
    TimeProvider clock,
    DemoDataOptions options,
    DemoCampus campus,
    ILogger<DemoCampusSeeder> logger) : IModuleSeeder
{
    private const string Domain = "@demo.cu.edu.eg";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!options.Enabled)
            return;
        if (string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning("Demo data is enabled but Seed:DemoPassword is missing; nothing seeded.");
            return;
        }
        if (await scheduling.GetCurrentTermAsync(cancellationToken) is not null
            || await users.CountActiveStudentsAsync(null, null, cancellationToken) > 0)
        {
            logger.LogInformation("Demo data skipped: the campus already has a term or students.");
            return;
        }

        var ct = cancellationToken;
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        // ── Organization and people ────────────────────────────────────
        var admin = await AccountAsync(UserRole.Admin, "شؤون الطلاب (تجريبي)", DemoCampus.AdminEmail, null, null, null, ct);

        var engineering = await sender.Send(new CreateFacultyCommand("ENG", "Faculty of Engineering", "كلية الهندسة"), ct);
        var commerce = await sender.Send(new CreateFacultyCommand("COM", "Faculty of Commerce", "كلية التجارة"), ct);
        var computer = await sender.Send(new CreateDepartmentCommand(
            engineering.Id, "CSE", "Computer Engineering", "هندسة الحاسبات", "Software, hardware and networks."), ct);
        var power = await sender.Send(new CreateDepartmentCommand(
            engineering.Id, "EPE", "Electrical Power Engineering", "هندسة القوى الكهربية", null), ct);
        var accounting = await sender.Send(new CreateDepartmentCommand(
            commerce.Id, "ACC", "Accounting", "المحاسبة", null), ct);

        var hassan = await AccountAsync(UserRole.Professor, "د. حسن محمود", DemoCampus.ProfessorEmail, computer.Id, null, AcademicTitle.Professor, ct);
        var mona = await AccountAsync(UserRole.Professor, "د. منى عادل", "mona.adel" + Domain, computer.Id, null, AcademicTitle.Lecturer, ct);
        var karim = await AccountAsync(UserRole.Professor, "د. كريم فتحي", "karim.fathy" + Domain, power.Id, null, AcademicTitle.Lecturer, ct);
        var salma = await AccountAsync(UserRole.Professor, "د. سلمى نبيل", "salma.nabil" + Domain, accounting.Id, null, AcademicTitle.Lecturer, ct);

        var academicYear = today.Month >= 8 ? today.Year : today.Year - 1;
        var cohort = academicYear - 1;
        var omar = await AccountAsync(UserRole.Student, "عمر خالد", DemoCampus.StudentEmail, computer.Id, cohort, null, ct);
        Guid[] computerStudents =
        [
            omar,
            await AccountAsync(UserRole.Student, "مريم أحمد", "mariam.ahmed" + Domain, computer.Id, cohort, null, ct),
            await AccountAsync(UserRole.Student, "يوسف إبراهيم", "youssef.ibrahim" + Domain, computer.Id, cohort, null, ct),
            await AccountAsync(UserRole.Student, "نور حسين", "nour.hussein" + Domain, computer.Id, cohort, null, ct),
        ];
        Guid[] powerStudents =
        [
            await AccountAsync(UserRole.Student, "أحمد سمير", "ahmed.samir" + Domain, power.Id, cohort, null, ct),
            await AccountAsync(UserRole.Student, "هبة مصطفى", "heba.mostafa" + Domain, power.Id, cohort, null, ct),
        ];
        Guid[] accountingStudents =
        [
            await AccountAsync(UserRole.Student, "سارة علي", "sara.ali" + Domain, accounting.Id, cohort, null, ct),
            await AccountAsync(UserRole.Student, "محمد طارق", "mohamed.tarek" + Domain, accounting.Id, cohort, null, ct),
        ];
        // Created with the demo password but not switched over: shows the forced first-login change.
        await sender.Send(new CreateUserCommand(UserRole.Student, "طالب جديد", DemoCampus.NewStudentEmail, options.Password,
            computer.Id, academicYear, null, null, null, null, null), ct);

        // ── A running term with a timetable ────────────────────────────
        var startsOn = today.AddDays(-28);
        startsOn = startsOn.AddDays(-(((int)startsOn.DayOfWeek + 1) % 7));   // back to Saturday, the first product day
        var term = await sender.Send(new CreateTermCommand(academicYear, TermNumber.First, startsOn, startsOn.AddDays(16 * 7 - 1), 16), ct);
        await sender.Send(new ActivateTermCommand(term.Id), ct);

        var hallA1 = await sender.Send(new CreateFacilityCommand(engineering.Id, "Hall A1", FacilityType.LectureHall, 120, "A", "1"), ct);
        var labC3 = await sender.Send(new CreateFacilityCommand(engineering.Id, "Lab C3", FacilityType.Lab, 40, "C", "3"), ct);
        var hallB2 = await sender.Send(new CreateFacilityCommand(commerce.Id, "Hall B2", FacilityType.LectureHall, 150, "B", "2"), ct);
        await sender.Send(new CreateFacilityCommand(null, "Main Auditorium", FacilityType.Auditorium, 500, "Central", "G"), ct);

        var programming = await SubjectAsync(computer.Id, "CSE101", "Introduction to Programming", "مقدمة في البرمجة", ct);
        var logic = await SubjectAsync(computer.Id, "CSE211", "Digital Logic Design", "تصميم المنطق الرقمي", ct);
        var algorithms = await SubjectAsync(computer.Id, "CSE301", "Algorithms", "الخوارزميات", ct);
        var circuits = await SubjectAsync(power.Id, "EPE201", "Electric Circuits", "الدوائر الكهربية", ct);
        var principles = await SubjectAsync(accounting.Id, "ACC101", "Principles of Accounting", "مبادئ المحاسبة", ct);

        var programming1 = await GroupAsync(programming, term.Id, 1, ct);
        var programming2 = await GroupAsync(programming, term.Id, 2, ct);
        var logic1 = await GroupAsync(logic, term.Id, 1, ct);
        var algorithms1 = await GroupAsync(algorithms, term.Id, 1, ct);
        var circuits1 = await GroupAsync(circuits, term.Id, 1, ct);
        var principles1 = await GroupAsync(principles, term.Id, 1, ct);

        await TeachAsync(hassan, programming, term.Id, null, TeachingRole.Lecturer, admin, ct);
        await TeachAsync(mona, programming, term.Id, programming1, TeachingRole.Lab, admin, ct);
        await TeachAsync(mona, logic, term.Id, null, TeachingRole.Lecturer, admin, ct);
        await TeachAsync(hassan, algorithms, term.Id, null, TeachingRole.Lecturer, admin, ct);
        await TeachAsync(karim, circuits, term.Id, null, TeachingRole.Lecturer, admin, ct);
        await TeachAsync(salma, principles, term.Id, null, TeachingRole.Lecturer, admin, ct);

        // Product days: 0 = Saturday … 5 = Thursday. No two of a student's groups overlap.
        await SlotAsync(programming1, hallA1.Id, hassan, 1, "09:00", "10:30", Recurrence.Weekly, startsOn, admin, ct);
        await SlotAsync(programming1, labC3.Id, mona, 4, "11:00", "12:30", Recurrence.Biweekly, startsOn, admin, ct);
        await SlotAsync(programming2, hallA1.Id, hassan, 2, "09:00", "10:30", Recurrence.Weekly, startsOn, admin, ct);
        await SlotAsync(logic1, labC3.Id, mona, 1, "11:00", "12:30", Recurrence.Weekly, startsOn, admin, ct);
        await SlotAsync(algorithms1, hallA1.Id, hassan, 3, "09:00", "10:30", Recurrence.Weekly, startsOn, admin, ct);
        await SlotAsync(circuits1, hallA1.Id, karim, 2, "11:00", "12:30", Recurrence.Weekly, startsOn, admin, ct);
        await SlotAsync(principles1, hallB2.Id, salma, 1, "09:00", "10:30", Recurrence.Weekly, startsOn, admin, ct);

        // ── Academic systems: credit hours by default, year groups in Commerce ──
        await sender.Send(new SetAcademicSystemCommand(null, null, Systems.CreditHours), ct);
        await sender.Send(new SetAcademicSystemCommand(commerce.Id, null, Systems.YearGroups), ct);

        // Credit hours (Engineering): students registered themselves in a window that is still open. Nobody has passed
        // CSE101 yet, so CSE301 shows up with a "Missing prerequisite" reason.
        await sender.Send(new SetSubjectPrerequisitesCommand(algorithms, [programming]), ct);
        await sender.Send(new CreateRegistrationWindowCommand(term.Id, now.AddDays(-7), now.AddDays(30), 6, 18), ct);
        foreach (var student in computerStudents)
        {
            await RegisterAsync(student, programming1, ct);
            await RegisterAsync(student, logic1, ct);
        }
        foreach (var student in powerStudents)
        {
            await RegisterAsync(student, programming2, ct);
            await RegisterAsync(student, circuits1, ct);
        }
        await sender.Send(new SetGraduationRequirementCommand(computer.Id, 160, admin), ct);

        // Year groups (Commerce): the Accounting plan's first year, and the term generated from it.
        await sender.Send(new SaveStudyPlanCommand(accounting.Id, 4, [new StudyPlanEntry(1, PlanTerm.First, principles)]), ct);
        await sender.Send(new GenerateTermEnrollmentCommand(term.Id, accounting.Id), ct);

        // ── Fees: tuition for engineering, an activity fee for everyone; Omar paid his tuition ──
        var dueOn = startsOn.AddDays(30);
        var tuition = await sender.Send(new CreateFeeItemCommand("Engineering tuition", "مصروفات كلية الهندسة",
            "First-term tuition.", "tuition", 6500m, term.Id, engineering.Id, null, dueOn, admin), ct);
        await sender.Send(new CreateFeeItemCommand("Student activities", "رسوم الأنشطة الطلابية",
            null, "activity", 250m, term.Id, null, null, dueOn, admin), ct);
        await sender.Send(new RecordPaymentCommand(omar, tuition.Id, 6500m, "cash", null, now.AddDays(-10), admin), ct);

        // ── Teaching content ───────────────────────────────────────────
        await sender.Send(new PostAnnouncementCommand(programming, null, "Welcome to CSE101",
            "Lecture slides are posted every week under Materials. Office hours: Sunday 12:00–13:00.",
            AnnouncementPriority.Normal, IsPinned: true, null, new AnnouncementViewer(hassan, false, true, false)), ct);
        await sender.Send(new PostAnnouncementCommand(logic, null, "Quiz 1 moved",
            "Quiz 1 moves to next Sunday in Lab C3.", AnnouncementPriority.Urgent, IsPinned: false, now.AddDays(14),
            new AnnouncementViewer(mona, false, true, false)), ct);
        await sender.Send(new CreateAssignmentCommand(programming, null, "Assignment 1: Variables and loops",
            "Solve the five exercises and upload one PDF or ZIP.", now.AddDays(7), 10m, "pdf,zip", 10,
            new AssignmentViewer(hassan, false, true, false)), ct);
        await using (var slides = new MemoryStream(DemoPdf))
        {
            await sender.Send(new UploadMaterialCommand(programming, null, "Lecture 1 — Introduction", "Course outline and tools.",
                1, "lecture-01.pdf", "application/pdf", slides.Length, slides, new MaterialViewer(hassan, false, true, false)), ct);
        }

        campus.Seeded = true;
        campus.AdminId = admin;
        campus.TermId = term.Id;
        campus.EngineeringFacultyId = engineering.Id;
        campus.ProfessorId = hassan;
        campus.StudentId = omar;
        campus.StudentIds = [.. computerStudents, .. powerStudents, .. accountingStudents];
        campus.ProgrammingSubjectId = programming;
        logger.LogInformation("Demo campus seeded: sign in as {Admin}, {Professor} or {Student}.",
            DemoCampus.AdminEmail, DemoCampus.ProfessorEmail, DemoCampus.StudentEmail);
    }

    /// <summary>A ready account: created with a temporary password, then switched to the demo password (no forced change).</summary>
    private async Task<Guid> AccountAsync(
        UserRole role, string fullName, string email, Guid? departmentId, int? enrollmentYear, AcademicTitle? title,
        CancellationToken ct)
    {
        var created = await sender.Send(new CreateUserCommand(
            role, fullName, email, null, departmentId, enrollmentYear, title, null, null, null, null), ct);
        await sender.Send(new ChangePasswordCommand(created.User.Id, created.TemporaryPassword!, options.Password!), ct);
        return created.User.Id;
    }

    private Task RegisterAsync(Guid studentId, Guid groupId, CancellationToken ct) =>
        sender.Send(new RegisterForSubjectCommand(studentId, IsStudent: true, groupId), ct);

    private async Task<Guid> SubjectAsync(Guid departmentId, string code, string name, string nameAr, CancellationToken ct) =>
        (await sender.Send(new CreateSubjectCommand(departmentId, code, name, nameAr, null, 3, SubjectCategory.Regular, true), ct)).Id;

    private async Task<Guid> GroupAsync(Guid subjectId, Guid termId, int number, CancellationToken ct) =>
        (await sender.Send(new CreateGroupCommand(subjectId, termId, number, null, 60), ct)).Id;

    private Task TeachAsync(
        Guid staffId, Guid subjectId, Guid termId, Guid? groupId, TeachingRole role, Guid adminId, CancellationToken ct) =>
        sender.Send(new CreateTeachingAssignmentCommand(staffId, subjectId, termId, groupId, role, adminId), ct);

    private Task SlotAsync(
        Guid groupId, Guid facilityId, Guid staffId, int day, string start, string end, Recurrence recurrence,
        DateOnly validFrom, Guid adminId, CancellationToken ct) =>
        sender.Send(new CreateScheduleSlotCommand(groupId, facilityId, staffId, day, TimeOnly.Parse(start),
            TimeOnly.Parse(end), recurrence, validFrom, null, adminId), ct);

    /// <summary>A one-page PDF ("Lecture 1"), enough for a download to open.</summary>
    private static readonly byte[] DemoPdf = Encoding.ASCII.GetBytes(
        "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
        "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 595 842]/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj\n" +
        "4 0 obj<</Length 44>>stream\nBT /F1 24 Tf 72 760 Td (Lecture 1) Tj ET\nendstream endobj\n" +
        "5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF\n");
}
