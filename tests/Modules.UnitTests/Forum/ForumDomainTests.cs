// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.UnitTests.Forum;

/// <summary>P11 Forum domain rules: identity snapshots, the 15-minute edit window, locks, notify-once and reports.</summary>
public sealed class ForumDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly ForumAuthor Ahmed = new(Guid.CreateVersion7(), "Ahmed Adel", "student", "CS", "علوم الحاسب");
    private static readonly ForumAuthor Mona = new(Guid.CreateVersion7(), "Mona Ali", "student", "CS", "علوم الحاسب");

    [Fact]
    public void A_thread_keeps_its_authors_identity_and_trims_its_texts()
    {
        var thread = ForumThread.Start(Guid.CreateVersion7(), null, Ahmed, "  Study group  ", " Who is in? ", Now);

        Assert.Equal(Ahmed, thread.Author);
        Assert.Equal("Study group", thread.Title);
        Assert.Equal("Who is in?", thread.Body);
        Assert.Equal(Now, thread.LastActivityAt);
        Assert.Equal(0, thread.ReplyCount);
    }

    [Theory]
    [InlineData("", "body")]
    [InlineData("title", " ")]
    public void Title_and_body_are_required(string title, string body) =>
        Assert.Throws<ArgumentException>(() => ForumThread.Start(Guid.CreateVersion7(), null, Ahmed, title, body, Now));

    [Fact]
    public void Texts_have_maximum_lengths()
    {
        Assert.Throws<ArgumentException>(() => ForumThread.Start(Guid.CreateVersion7(), null, Ahmed, new string('t', 256), "b", Now));
        Assert.Throws<ArgumentException>(() => ForumThread.Start(Guid.CreateVersion7(), null, Ahmed, "t", new string('b', 10_001), Now));
        Assert.Throws<ArgumentException>(() => ForumPost.Write(Guid.CreateVersion7(), Ahmed, new string('b', 10_001), Now));
    }

    [Fact]
    public void Only_the_author_edits_and_only_for_15_minutes()
    {
        var post = ForumPost.Write(Guid.CreateVersion7(), Mona, "draft", Now);

        Assert.True(post.CanAuthorEdit(Mona.Id, Now.AddMinutes(15)));
        Assert.False(post.CanAuthorEdit(Mona.Id, Now.AddMinutes(15).AddSeconds(1)));
        Assert.False(post.CanAuthorEdit(Ahmed.Id, Now.AddMinutes(1)));
    }

    [Fact]
    public void A_reply_bumps_the_thread_and_a_locked_or_deleted_thread_refuses_it()
    {
        var thread = ForumThread.Start(Guid.CreateVersion7(), null, Ahmed, "t", "b", Now);

        thread.AcceptReply(Mona, Now.AddMinutes(5));
        Assert.Equal(1, thread.ReplyCount);
        Assert.Equal(Now.AddMinutes(5), thread.LastActivityAt);

        thread.SetLocked(true, Now);
        Assert.Throws<InvalidOperationException>(() => thread.AcceptReply(Mona, Now));
        thread.SetLocked(false, Now);
        thread.Delete(Now);
        Assert.Throws<InvalidOperationException>(() => thread.AcceptReply(Mona, Now));
    }

    [Fact]
    public void The_author_is_notified_once_until_they_open_the_thread()
    {
        var thread = ForumThread.Start(Guid.CreateVersion7(), null, Ahmed, "t", "b", Now);

        Assert.True(thread.AcceptReply(Mona, Now.AddMinutes(1)));
        Assert.False(thread.AcceptReply(Mona, Now.AddMinutes(2)));
        thread.MarkSeenBy(Mona.Id, Now.AddMinutes(3));                 // someone else reading changes nothing
        Assert.False(thread.AcceptReply(Mona, Now.AddMinutes(4)));
        thread.MarkSeenBy(Ahmed.Id, Now.AddMinutes(5));
        Assert.True(thread.AcceptReply(Mona, Now.AddMinutes(6)));
        Assert.False(thread.AcceptReply(Ahmed, Now.AddMinutes(7)));    // own reply never notifies
    }

    [Fact]
    public void A_report_targets_exactly_one_thing_and_is_resolved_once()
    {
        Assert.Throws<ArgumentException>(() => ForumReport.File(null, null, Ahmed.Id, "spam", Now));
        Assert.Throws<ArgumentException>(() => ForumReport.File(Guid.CreateVersion7(), Guid.CreateVersion7(), Ahmed.Id, "spam", Now));
        Assert.Throws<ArgumentException>(() => ForumReport.File(null, Guid.CreateVersion7(), Ahmed.Id, " ", Now));

        var report = ForumReport.File(null, Guid.CreateVersion7(), Ahmed.Id, "spam", Now);
        Assert.Equal(ForumReportStatus.Open, report.Status);

        report.Resolve(removeContent: true, Guid.CreateVersion7(), Now.AddHours(1));
        Assert.Equal(ForumReportStatus.Resolved, report.Status);
        Assert.Equal(ForumReportResolution.Removed, report.Resolution);
        Assert.Throws<InvalidOperationException>(() => report.Resolve(removeContent: false, Guid.CreateVersion7(), Now));
    }

    [Fact]
    public void A_category_needs_a_name_in_both_languages()
    {
        Assert.Throws<ArgumentException>(() => ForumCategory.Create(" ", "عام", null, 0, Now));
        Assert.Throws<ArgumentException>(() => ForumCategory.Create("General", "", null, 0, Now));
        Assert.True(ForumCategory.Create("General", "عام", null, 0, Now).IsActive);
    }
}
