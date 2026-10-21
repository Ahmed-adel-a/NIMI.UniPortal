// ACCEPTANCE TEST — written by the consultant for phase P04A. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

namespace NIMI.UniPortal.Modules.UnitTests.Lifecycle;

public sealed class CourseRegistrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Student = Guid.CreateVersion7();
    private static readonly Guid Term = Guid.CreateVersion7();
    private static readonly Guid Subject = Guid.CreateVersion7();
    private static readonly Guid Group = Guid.CreateVersion7();

    [Fact]
    public void Registering_records_the_student_subject_group_and_credit_snapshot()
    {
        var registration = CourseRegistration.Register(Student, Term, Subject, Group, 3, Now);

        Assert.Equal(7, registration.Id.Version);
        Assert.Equal(Student, registration.StudentId);
        Assert.Equal(Term, registration.TermId);
        Assert.Equal(Subject, registration.SubjectId);
        Assert.Equal(Group, registration.SubjectGroupId);
        Assert.Equal(3, registration.CreditHours);
        Assert.Equal(RegistrationStatus.Registered, registration.Status);
        Assert.Equal(Now, registration.RegisteredAt);
        Assert.Null(registration.DroppedAt);
    }

    [Fact]
    public void Dropping_a_registration_stamps_the_time_and_cannot_happen_twice()
    {
        var registration = CourseRegistration.Register(Student, Term, Subject, Group, 3, Now);

        registration.Drop(Now.AddDays(1));

        Assert.Equal(RegistrationStatus.Dropped, registration.Status);
        Assert.Equal(Now.AddDays(1), registration.DroppedAt);
        Assert.Throws<InvalidOperationException>(() => registration.Drop(Now.AddDays(2)));
    }

    [Fact]
    public void A_pending_payment_registration_becomes_registered_when_the_fee_is_paid()
    {
        var registration = CourseRegistration.CreatePendingPayment(Student, Term, Subject, Group, 0, Now);
        Assert.Equal(RegistrationStatus.PendingPayment, registration.Status);

        registration.ConfirmPayment(Now.AddDays(3));

        Assert.Equal(RegistrationStatus.Registered, registration.Status);
        Assert.Equal(Now.AddDays(3), registration.ConfirmedAt);
    }

    [Fact]
    public void Only_pending_payment_registrations_can_be_confirmed_and_they_cannot_be_dropped()
    {
        var registered = CourseRegistration.Register(Student, Term, Subject, Group, 3, Now);
        var pending = CourseRegistration.CreatePendingPayment(Student, Term, Subject, Group, 0, Now);

        Assert.Throws<InvalidOperationException>(() => registered.ConfirmPayment(Now));
        Assert.Throws<InvalidOperationException>(() => pending.Drop(Now));
    }

    [Fact]
    public void Credit_hours_cannot_be_negative()
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => CourseRegistration.Register(Student, Term, Subject, Group, -1, Now));

        Assert.Equal("creditHours", error.ParamName);
    }
}
