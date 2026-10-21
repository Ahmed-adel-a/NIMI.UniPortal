// ACCEPTANCE TEST — written by the consultant for phase P04A. Do not modify; report problems instead.
namespace NIMI.UniPortal.Modules.IntegrationTests.Lifecycle;

/// <summary>
/// The campus <see cref="RegistrationSetup.BuildCampusAsync"/> creates (active 2026/2027 first term, window open,
/// min 12 / max 21 credits). Timetable (product days: 1 = Sunday, 2 = Monday, 3 = Tuesday, 4 = Wednesday, 5 = Thursday):
/// <list type="bullet">
/// <item>CS301 Algorithms 3 cr — group 1 Sun 09:00–10:30 Hall A · group 2 Mon 11:00–12:30 Hall B (Hany teaches the subject)</item>
/// <item>CS302 Databases 3 cr — group 1 Sun 10:00–11:00 Hall B (clashes with CS301 group 1)</item>
/// <item>CS303 Networks 3 cr — group 1 Tue 09:00–10:30 Hall A, capacity 1</item>
/// <item>CIV101 Statics 3 cr (Civil) — group 1 Wed 09:00–10:30 Hall A, reserved for Civil</item>
/// <item>MIL100 Military education 0 cr, graduation requirement — group 1 Thu 09:00–10:30 Hall B</item>
/// </list>
/// Students Ahmed and Mona are in Computer Science.
/// </summary>
internal sealed record CampusWorld(
    Guid FacultyId,
    Guid CsDepartmentId,
    Guid CivDepartmentId,
    Guid TermId,
    Guid WindowId,
    Guid Cs301,
    Guid Cs302,
    Guid Cs303,
    Guid Civ101,
    Guid Mil100,
    Guid Cs301Group1,
    Guid Cs301Group2,
    Guid Cs302Group1,
    Guid Cs303Group1,
    Guid Civ101Group1,
    Guid Mil100Group1,
    Guid Hany,
    HttpClient HanyClient,
    Guid Ahmed,
    HttpClient AhmedClient,
    Guid Mona,
    HttpClient MonaClient);
