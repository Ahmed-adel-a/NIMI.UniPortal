// ACCEPTANCE TEST — written by the consultant for phase P07-announcements. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Announcements.Domain;

namespace NIMI.UniPortal.Modules.UnitTests.Announcements;

/// <summary>Subject announcements: content rules, expiry (R6) and soft delete.</summary>
public sealed class AnnouncementTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void An_announcement_keeps_its_content_priority_and_expiry()
    {
        var group = Guid.CreateVersion7();

        var announcement = Announcement.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), group, Guid.CreateVersion7(),
            " Midterm moved ", "الامتحان يوم الأحد في القاعة ٣٠٢", AnnouncementPriority.Urgent, true, Now.AddDays(7), Now);

        Assert.Equal(7, announcement.Id.Version);
        Assert.Equal("Midterm moved", announcement.Title);
        Assert.Equal("الامتحان يوم الأحد في القاعة ٣٠٢", announcement.Body);
        Assert.Equal(AnnouncementPriority.Urgent, announcement.Priority);
        Assert.True(announcement.IsPinned);
        Assert.Equal(group, announcement.SubjectGroupId);
        Assert.Equal(Now.AddDays(7), announcement.ExpiresAt);
        Assert.False(announcement.IsDeleted);
    }

    [Fact]
    public void Invalid_content_is_rejected_with_its_field_name()
    {
        Assert.Equal("title", Assert.ThrowsAny<ArgumentException>(() => New(title: " ")).ParamName);
        Assert.Equal("title", Assert.ThrowsAny<ArgumentException>(() => New(title: new string('t', 256))).ParamName);
        Assert.Equal("body", Assert.ThrowsAny<ArgumentException>(() => New(body: "")).ParamName);
        Assert.Equal("body", Assert.ThrowsAny<ArgumentException>(() => New(body: new string('b', 10001))).ParamName);
        Assert.Equal("expiresAt", Assert.ThrowsAny<ArgumentException>(() => New(expiresAt: Now)).ParamName);
        Assert.Equal("expiresAt", Assert.ThrowsAny<ArgumentException>(() => New(expiresAt: Now.AddMinutes(-1))).ParamName);
    }

    [Fact]
    public void An_announcement_expires_at_its_expiry_instant_and_never_without_one()
    {
        var expiring = New(expiresAt: Now.AddHours(1));
        var lasting = New();

        Assert.False(expiring.IsExpiredAt(Now.AddMinutes(59)));
        Assert.True(expiring.IsExpiredAt(Now.AddHours(1)));
        Assert.False(lasting.IsExpiredAt(Now.AddYears(1)));
    }

    [Fact]
    public void Editing_replaces_the_content_and_deleting_is_soft()
    {
        var announcement = New();

        announcement.Edit("Midterm moved to Monday", "Hall 302, 10:00", AnnouncementPriority.High, false, null, Now.AddHours(1));
        announcement.Delete(Now.AddHours(2));
        announcement.Delete(Now.AddHours(3));

        Assert.Equal("Midterm moved to Monday", announcement.Title);
        Assert.Equal(AnnouncementPriority.High, announcement.Priority);
        Assert.False(announcement.IsPinned);
        Assert.True(announcement.IsDeleted);
        Assert.Equal(Now.AddHours(2), announcement.UpdatedAt);
    }

    private static Announcement New(string title = "Midterm moved", string body = "Hall 302", DateTimeOffset? expiresAt = null) =>
        Announcement.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), null, Guid.CreateVersion7(),
            title, body, AnnouncementPriority.Normal, false, expiresAt, Now);
}
