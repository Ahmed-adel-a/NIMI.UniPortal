// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Library.Domain;

namespace NIMI.UniPortal.Modules.UnitTests.Library;

/// <summary>P11 Library domain: ISBNs, copies, loans and renewals, reservations and holds, loan rules.</summary>
public sealed class LibraryDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("0-262-04630-X", "9780262046305")]
    [InlineData("978-0-262-04630-5", "9780262046305")]
    [InlineData(" 9780078022159 ", "9780078022159")]
    public void Isbns_are_stored_as_isbn_13(string raw, string expected) => Assert.Equal(expected, Isbn.Normalize(raw));

    [Theory]
    [InlineData("978-0-00-000000-1")]
    [InlineData("0-262-04630-1")]
    [InlineData("12345")]
    public void Invalid_isbns_are_rejected(string raw) => Assert.Throws<ArgumentException>(() => Isbn.Normalize(raw));

    [Fact]
    public void A_missing_isbn_is_allowed() => Assert.Null(Isbn.Normalize("  "));

    [Fact]
    public void A_copy_lends_from_the_shelf_or_a_hold_and_comes_back()
    {
        var copy = BookCopy.Add(Guid.CreateVersion7(), "LIB-0001", "QA76", Now);
        Assert.Equal(CopyStatus.Available, copy.Status);

        copy.Lend();
        Assert.Equal(CopyStatus.OnLoan, copy.Status);
        Assert.Throws<InvalidOperationException>(() => copy.Lend());
        Assert.Throws<InvalidOperationException>(() => copy.SetCondition(CopyStatus.Lost));

        var reservationId = Guid.CreateVersion7();
        copy.HoldFor(reservationId);
        Assert.Equal(CopyStatus.OnHold, copy.Status);
        Assert.Equal(reservationId, copy.HeldForReservationId);

        copy.Lend();
        Assert.Null(copy.HeldForReservationId);
        copy.ReturnToShelf();
        Assert.Equal(CopyStatus.Available, copy.Status);

        copy.SetCondition(CopyStatus.Withdrawn);
        Assert.Throws<InvalidOperationException>(() => copy.Lend());
    }

    [Fact]
    public void A_loan_is_due_after_the_loan_period_and_overdue_after_that()
    {
        var loan = Loan.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Now, loanDays: 14);

        Assert.Equal(Now.AddDays(14), loan.DueAt);
        Assert.True(loan.IsOpen);
        Assert.False(loan.IsOverdue(Now.AddDays(14)));
        Assert.True(loan.IsOverdue(Now.AddDays(14).AddSeconds(1)));
    }

    [Fact]
    public void Renewal_restarts_the_period_before_the_due_date_within_the_limit()
    {
        var loan = Loan.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Now, loanDays: 14);

        loan.Renew(Now.AddDays(10), loanDays: 14, maxRenewals: 1);
        Assert.Equal(Now.AddDays(24), loan.DueAt);
        Assert.Equal(1, loan.Renewals);

        Assert.Throws<InvalidOperationException>(() => loan.Renew(Now.AddDays(11), 14, 1));             // limit
        var late = Loan.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Now, 14);
        Assert.Throws<InvalidOperationException>(() => late.Renew(Now.AddDays(15), 14, 1));            // overdue
    }

    [Fact]
    public void A_loan_is_returned_once()
    {
        var loan = Loan.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Now, 14);

        loan.Return(Now.AddDays(3));

        Assert.False(loan.IsOpen);
        Assert.False(loan.IsOverdue(Now.AddDays(30)));
        Assert.Throws<InvalidOperationException>(() => loan.Return(Now.AddDays(4)));
        Assert.Throws<InvalidOperationException>(() => loan.Renew(Now.AddDays(4), 14, 1));
    }

    [Fact]
    public void A_reservation_waits_becomes_ready_and_is_fulfilled_or_expires()
    {
        var reservation = Reservation.Place(Guid.CreateVersion7(), Guid.CreateVersion7(), Now);
        Assert.Equal(ReservationStatus.Waiting, reservation.Status);
        Assert.True(reservation.IsActive);

        var copyId = Guid.CreateVersion7();
        reservation.MarkReady(copyId, Now.AddDays(1), holdDays: 3);
        Assert.Equal(ReservationStatus.Ready, reservation.Status);
        Assert.Equal(Now.AddDays(4), reservation.ExpiresAt);
        Assert.False(reservation.HasExpired(Now.AddDays(4)));
        Assert.True(reservation.HasExpired(Now.AddDays(4).AddSeconds(1)));

        reservation.Fulfill(Now.AddDays(2));
        Assert.Equal(ReservationStatus.Fulfilled, reservation.Status);
        Assert.False(reservation.IsActive);
        Assert.Throws<InvalidOperationException>(() => reservation.Cancel(Now));

        var other = Reservation.Place(Guid.CreateVersion7(), Guid.CreateVersion7(), Now);
        other.MarkReady(copyId, Now, 3);
        other.Expire(Now.AddDays(5));
        Assert.Equal(ReservationStatus.Expired, other.Status);
    }

    [Fact]
    public void Loan_rules_have_defaults_per_role_and_sane_limits()
    {
        var student = LoanPolicy.Default("student", Now);
        Assert.Equal((14, 3, 1, 3), (student.LoanDays, student.MaxOpenLoans, student.MaxRenewals, student.HoldDays));
        var professor = LoanPolicy.Default("professor", Now);
        Assert.Equal((30, 10, 2, 3), (professor.LoanDays, professor.MaxOpenLoans, professor.MaxRenewals, professor.HoldDays));

        Assert.Throws<ArgumentOutOfRangeException>(() => student.Update(0, 3, 1, 3, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => student.Update(14, 0, 1, 3, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => student.Update(14, 3, -1, 3, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => student.Update(14, 3, 1, 0, Now));
        Assert.Throws<ArgumentException>(() => LoanPolicy.Default("alumni", Now));
    }
}
