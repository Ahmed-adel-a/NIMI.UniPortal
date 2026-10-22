using System.Globalization;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Grades.Application.Mine;
using NIMI.UniPortal.Modules.Payments.Application.Ledger.GetReceipt;
using NIMI.UniPortal.Modules.Platform.Application.Settings.GetPublicSettings;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Documents;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;
using GradesViewer = NIMI.UniPortal.Modules.Grades.Application.Common.Viewer;

namespace NIMI.UniPortal.Hosting.Documents;

/// <summary>
/// Decision PDFDOCS: the printable English documents, composed in the host from the modules' own queries and read
/// contracts (each module keeps its access rules). Text the standard PDF fonts can't draw, such as Arabic, prints
/// as '?'; the documents use the English fields.
/// </summary>
internal sealed class DocumentComposer(
    ISender sender, IUsersApi users, ISchedulingApi scheduling, IAcademicLifecycle lifecycle, TimeProvider clock)
{
    private static readonly string[] Days = ["Saturday", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];

    public async Task<byte[]> ReceiptAsync(Guid paymentId, Guid callerId, bool isAdmin, CancellationToken cancellationToken)
    {
        var receipt = await sender.Send(new GetPaymentReceiptQuery(paymentId, callerId, isAdmin), cancellationToken);
        return (await DocumentAsync("Payment receipt", cancellationToken))
            .Fields(
                ("Receipt number", receipt.ReceiptNumber),
                ("Date", receipt.PaidAt.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)),
                ("Student", receipt.StudentName),
                ("Student code", receipt.StudentCode),
                ("Fee", receipt.FeeName),
                ("Amount paid", Money(receipt.Amount)),
                ("Method", Words(receipt.Method)),
                ("Reference", receipt.Reference))
            .Paragraph("This receipt confirms the payment above. Keep it for your records.")
            .ToPdf();
    }

    /// <summary>The student's published grades, newest term first, with term and cumulative GPA.</summary>
    public async Task<byte[]> TranscriptAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var student = await StudentAsync(studentId, cancellationToken);
        var grades = await sender.Send(new MyGradesQuery(null, new GradesViewer(studentId, false, false, true)), cancellationToken);

        var document = (await DocumentAsync("Grade report", cancellationToken))
            .Fields(
                ("Student", student.User.FullName),
                ("Student code", student.User.StudentCode),
                ("Department", student.Department?.Name),
                ("Cumulative GPA", Gpa(grades.CumulativeGpa)));

        if (grades.Terms.Count == 0)
            return document.Paragraph("No grades have been published yet.").ToPdf();

        foreach (var term in grades.Terms)
        {
            document
                .Heading($"{TermName(term.AcademicYear, term.Number)} (term GPA {Gpa(term.TermGpa)})")
                .Table(
                    [
                        new TableColumn("Code", 70),
                        new TableColumn("Subject", 235),
                        new TableColumn("Credits", 50, AlignRight: true),
                        new TableColumn("Score", 50, AlignRight: true),
                        new TableColumn("Grade", 45, AlignRight: true),
                        new TableColumn("Points", 45, AlignRight: true),
                    ],
                    term.Grades.Select(g => (IReadOnlyList<string?>)
                    [
                        g.SubjectCode, g.SubjectName, Number(g.CreditHours), Number(g.Score), g.Grade, Number(g.GradePoints),
                    ]));
        }

        return document.ToPdf();
    }

    /// <summary>إفادة قيد: proof that an active student is enrolled, for the current term when there is one.</summary>
    public async Task<byte[]> CertificateAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var student = await StudentAsync(studentId, cancellationToken);
        if (!student.User.IsActive)
            throw new ConflictException("The student's account is not active; no enrollment certificate can be issued.");

        var university = (await sender.Send(new GetPublicSettingsQuery(), cancellationToken)).UniversityName;
        var term = await scheduling.GetCurrentTermAsync(cancellationToken);
        var period = term is null ? "the current academic year" : $"the {TermName(term.AcademicYear, term.Number)}";
        var issued = clock.GetUtcNow();

        return new PrintedDocument(university, "Enrollment certificate", issued)
            .Fields(
                ("Certificate number", $"EC-{issued:yyyyMMdd}-{student.User.StudentCode ?? studentId.ToString("N")[..8]}"),
                ("Date of issue", issued.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
            .Space(6)
            .Paragraph("To whom it may concern,")
            .Paragraph(
                $"This is to certify that {student.User.FullName}, student code {student.User.StudentCode ?? "-"}, is a " +
                $"registered student of the {student.Department?.Name ?? "university"}" +
                $"{(student.Faculty is null ? string.Empty : $", {student.Faculty.Name}")}, {university}, " +
                $"enrolled since {student.Placement?.EnrollmentYear.ToString(CultureInfo.InvariantCulture) ?? "-"}, " +
                $"and is enrolled for {period}.")
            .Paragraph("This certificate is issued at the student's request, without any responsibility on the university towards third parties.")
            .Space(30)
            .Paragraph("Student Affairs", bold: true)
            .ToPdf();
    }

    /// <summary>The student's classes between two dates (at most 31 days), as the schedule screen shows them.</summary>
    public async Task<byte[]> ScheduleAsync(Guid studentId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (from is not { } start)
            throw ValidationErrors.For("from", "Required.");
        if (to is not { } end)
            throw ValidationErrors.For("to", "Required.");
        if (end < start)
            throw ValidationErrors.For("to", "Must be on or after 'from'.");
        if (end.DayNumber - start.DayNumber > 30)
            throw ValidationErrors.For("to", "The range is at most 31 calendar days.");

        var student = await StudentAsync(studentId, cancellationToken);
        var term = await scheduling.GetCurrentTermAsync(cancellationToken);
        var groups = term is null ? [] : await lifecycle.GetGroupsForStudentAsync(studentId, term.Id, cancellationToken);
        var occurrences = groups.Count == 0 ? [] : await scheduling.GetOccurrencesAsync(groups, start, end, cancellationToken);

        var document = (await DocumentAsync("Class schedule", cancellationToken))
            .Fields(
                ("Student", student.User.FullName),
                ("Student code", student.User.StudentCode),
                ("Period", $"{start:yyyy-MM-dd} to {end:yyyy-MM-dd}"));
        if (occurrences.Count == 0)
            return document.Paragraph("No classes in this period.").ToPdf();

        return document
            .Table(
                [
                    new TableColumn("Date", 70),
                    new TableColumn("Day", 65),
                    new TableColumn("Time", 75),
                    new TableColumn("Code", 60),
                    new TableColumn("Subject", 145),
                    new TableColumn("Group", 40, AlignRight: true),
                    new TableColumn("Hall", 40),
                ],
                occurrences
                    .OrderBy(o => o.Date).ThenBy(o => o.StartTime)
                    .Select(o => (IReadOnlyList<string?>)
                    [
                        o.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        o.DayOfWeek is >= 0 and < 7 ? Days[o.DayOfWeek] : string.Empty,
                        $"{o.StartTime:HH:mm}-{o.EndTime:HH:mm}",
                        o.SubjectCode,
                        o.SubjectName,
                        Number(o.GroupNumber),
                        o.FacilityName,
                    ]))
            .ToPdf();
    }

    private async Task<PrintedDocument> DocumentAsync(string title, CancellationToken cancellationToken)
    {
        var settings = await sender.Send(new GetPublicSettingsQuery(), cancellationToken);
        return new PrintedDocument(settings.UniversityName, title, clock.GetUtcNow());
    }

    private async Task<DocumentStudent> StudentAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(studentId, cancellationToken);
        if (user is null || user.Role != "student")
            throw new NotFoundException($"No student with id '{studentId}'.");

        var placement = await users.GetStudentPlacementAsync(studentId, cancellationToken);
        var department = user.DepartmentId is { } departmentId ? await users.GetDepartmentAsync(departmentId, cancellationToken) : null;
        var faculty = department is null ? null : await users.GetFacultyAsync(department.FacultyId, cancellationToken);
        return new DocumentStudent(user, placement, department, faculty);
    }

    private static string TermName(int academicYear, string number) =>
        $"{Words(number)} term {academicYear}/{academicYear + 1}";

    private static string Words(string snakeCase) =>
        snakeCase.Length == 0 ? snakeCase : char.ToUpperInvariant(snakeCase[0]) + snakeCase[1..].Replace('_', ' ');

    private static string Money(decimal amount) => amount.ToString("#,0.00", CultureInfo.InvariantCulture);

    private static string Gpa(decimal? gpa) => gpa?.ToString("0.00", CultureInfo.InvariantCulture) ?? "-";

    private static string Number(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
