using NIMI.UniPortal.Modules.Users.Domain.Accounts;

namespace NIMI.UniPortal.Modules.UnitTests.Users;

/// <summary>Student-code formats (settings) → codes and counter keys.</summary>
public sealed class StudentCodeFormatTests
{
    [Theory]
    [InlineData("{DEPT}-{YEAR}-{SEQ:0000}", 1, "CS-2026-0001")]
    [InlineData("{DEPT}-{YEAR}-{SEQ:0000}", 42, "CS-2026-0042")]
    [InlineData("{DEPT}-{YEAR}-{SEQ:0000}", 12345, "CS-2026-12345")]   // grows past the padding, never truncates
    [InlineData("{FAC}{YEAR}{SEQ:000}", 7, "ENG2026007")]
    [InlineData("{FAC}/{DEPT}/{SEQ}", 7, "ENG/CS/7")]
    public void Codes_are_rendered_from_the_format(string format, int sequence, string expected) =>
        Assert.Equal(expected, StudentCodeFormat.Render(format, "ENG", "CS", 2026, sequence));

    [Fact]
    public void Each_prefix_has_its_own_counter()
    {
        const string format = "{DEPT}-{YEAR}-{SEQ:0000}";

        Assert.Equal("CS-2026-#", StudentCodeFormat.CounterKey(format, "ENG", "CS", 2026));
        Assert.NotEqual(
            StudentCodeFormat.CounterKey(format, "ENG", "CS", 2026),
            StudentCodeFormat.CounterKey(format, "ENG", "CIV", 2026));
        Assert.NotEqual(
            StudentCodeFormat.CounterKey(format, "ENG", "CS", 2026),
            StudentCodeFormat.CounterKey(format, "ENG", "CS", 2025));
    }

    [Theory]
    [InlineData("{DEPT}-{YEAR}")]
    [InlineData("{SEQ}-{SEQ}")]
    public void A_format_needs_exactly_one_sequence_token(string format) =>
        Assert.Throws<ArgumentException>(() => StudentCodeFormat.CounterKey(format, "ENG", "CS", 2026));

    [Fact]
    public void Sequences_start_at_one() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => StudentCodeFormat.Render("{SEQ}", "ENG", "CS", 2026, 0));
}
