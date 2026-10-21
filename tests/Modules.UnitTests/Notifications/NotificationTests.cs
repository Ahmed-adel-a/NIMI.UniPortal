// ACCEPTANCE TEST — written by the consultant for phase P02. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Notifications.Domain;

namespace NIMI.UniPortal.Modules.UnitTests.Notifications;

public sealed class NotificationTests
{
    private static readonly DateTimeOffset Created = new(2026, 10, 21, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Student = Guid.CreateVersion7();
    private static readonly Guid SourceEvent = Guid.CreateVersion7();

    [Fact]
    public void A_new_notification_is_unread_and_keeps_every_text()
    {
        var notification = Create();

        Assert.Equal(7, notification.Id.Version);
        Assert.Equal(Student, notification.UserId);
        Assert.Equal(NotificationType.Grade, notification.Type);
        Assert.Equal("Grades published", notification.Title);
        Assert.Equal("تم نشر الدرجات", notification.TitleAr);
        Assert.Equal("Your CS301 grade is available.", notification.Message);
        Assert.Equal("درجة مادة CS301 متاحة الآن.", notification.MessageAr);
        Assert.Equal("/grades", notification.ActionUrl);
        Assert.Equal(SourceEvent, notification.SourceEventId);
        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAt);
        Assert.Equal(Created, notification.CreatedAt);
    }

    [Fact]
    public void Marking_read_twice_keeps_the_first_read_time()
    {
        var notification = Create();

        notification.MarkRead(Created.AddMinutes(5));
        notification.MarkRead(Created.AddMinutes(30));

        Assert.True(notification.IsRead);
        Assert.Equal(Created.AddMinutes(5), notification.ReadAt);
    }

    [Fact]
    public void The_action_url_is_optional()
    {
        Assert.Null(Create(actionUrl: null).ActionUrl);
    }

    [Fact]
    public void Each_invalid_value_is_rejected_with_its_field_name()
    {
        AssertRejected("userId", () => Create(userId: Guid.Empty));
        AssertRejected("title", () => Create(title: " "));
        AssertRejected("title", () => Create(title: new string('t', 201)));
        AssertRejected("titleAr", () => Create(titleAr: ""));
        AssertRejected("message", () => Create(message: ""));
        AssertRejected("message", () => Create(message: new string('m', 2001)));
        AssertRejected("messageAr", () => Create(messageAr: " "));
        AssertRejected("actionUrl", () => Create(actionUrl: "https://evil.example/grades"));
        AssertRejected("actionUrl", () => Create(actionUrl: "/" + new string('a', 500)));
    }

    [Fact]
    public void Texts_at_the_maximum_length_are_accepted()
    {
        var notification = Create(title: new string('t', 200), message: new string('m', 2000), actionUrl: "/" + new string('a', 499));

        Assert.Equal(200, notification.Title.Length);
        Assert.Equal(2000, notification.Message.Length);
    }

    private static void AssertRejected(string field, Action act)
    {
        var error = Assert.ThrowsAny<ArgumentException>(act);
        Assert.Equal(field, error.ParamName);
    }

    private static Notification Create(
        Guid? userId = null,
        string title = "Grades published",
        string titleAr = "تم نشر الدرجات",
        string message = "Your CS301 grade is available.",
        string messageAr = "درجة مادة CS301 متاحة الآن.",
        string? actionUrl = "/grades") =>
        Notification.Create(userId ?? Student, NotificationType.Grade, title, titleAr, message, messageAr, actionUrl, SourceEvent, Created);
}
