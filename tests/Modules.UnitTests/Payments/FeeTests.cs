// ACCEPTANCE TEST — written by the consultant for phase P05-payments. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Payments.Domain.Fees;

namespace NIMI.UniPortal.Modules.UnitTests.Payments;

/// <summary>Fee items, who they apply to, and a student's balance against one (rules R1, R2, R4).</summary>
public sealed class FeeTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Due = new(2026, 10, 15);
    private static readonly Guid Eng = Guid.CreateVersion7();
    private static readonly Guid Com = Guid.CreateVersion7();
    private static readonly Guid Cs = Guid.CreateVersion7();
    private static readonly Guid Civ = Guid.CreateVersion7();
    private static readonly Guid Admin = Guid.CreateVersion7();

    [Fact]
    public void A_fee_item_keeps_its_details()
    {
        var term = Guid.CreateVersion7();

        var fee = FeeItem.Create("Tuition", "المصروفات الدراسية", "First term", FeeCategory.Tuition, 5000.50m, term, null, null, Due, Admin, Now);

        Assert.Equal(7, fee.Id.Version);
        Assert.Equal("Tuition", fee.Name);
        Assert.Equal("المصروفات الدراسية", fee.NameAr);
        Assert.Equal(FeeCategory.Tuition, fee.Category);
        Assert.Equal(5000.50m, fee.Amount);
        Assert.Equal(term, fee.TermId);
        Assert.Equal(Due, fee.DueOn);
        Assert.True(fee.IsActive);
    }

    [Fact]
    public void Invalid_fee_items_are_rejected_with_their_field_name()
    {
        AssertRejected("amount", () => Item(amount: 0m));
        AssertRejected("amount", () => Item(amount: -10m));
        AssertRejected("amount", () => Item(amount: 10.555m));
        AssertRejected("name", () => FeeItem.Create(" ", "أ", null, FeeCategory.Other, 10m, null, null, null, Due, Admin, Now));
        AssertRejected("nameAr", () => FeeItem.Create("A", "", null, FeeCategory.Other, 10m, null, null, null, Due, Admin, Now));
    }

    [Fact]
    public void A_fee_applies_to_the_whole_university_a_faculty_or_a_department()
    {
        var university = Item();
        var engineering = Item(facultyId: Eng);
        var computerScience = Item(facultyId: Eng, departmentId: Cs);

        Assert.True(university.AppliesTo(Com, Guid.CreateVersion7()));
        Assert.True(engineering.AppliesTo(Eng, Civ));
        Assert.False(engineering.AppliesTo(Com, Guid.CreateVersion7()));
        Assert.True(computerScience.AppliesTo(Eng, Cs));
        Assert.False(computerScience.AppliesTo(Eng, Civ));
    }

    [Fact]
    public void Inactive_and_military_education_fees_never_apply_by_targeting()
    {
        var inactive = Item();
        inactive.Update("Tuition", "المصروفات", null, 5000m, Due, isActive: false, Now);
        var military = Item(category: FeeCategory.MilitaryEducation);

        Assert.False(inactive.AppliesTo(Eng, Cs));
        Assert.False(military.AppliesTo(Eng, Cs));
    }

    [Fact]
    public void A_student_fee_snapshots_the_amount_and_tracks_partial_and_full_payment()
    {
        var item = Item(amount: 500m);
        var fee = StudentFee.Create(item, Guid.CreateVersion7(), Now);
        item.Update("Tuition", "المصروفات", null, 900m, Due, isActive: true, Now);   // later price change

        Assert.Equal(500m, fee.AmountDue);
        Assert.Equal(StudentFeeStatus.Pending, fee.Status);

        fee.ApplyPayment(200m, Now);
        Assert.Equal(StudentFeeStatus.PartiallyPaid, fee.Status);
        Assert.Equal(300m, fee.Balance);
        Assert.False(fee.IsFullyPaid);

        fee.ApplyPayment(300m, Now);
        Assert.Equal(StudentFeeStatus.Paid, fee.Status);
        Assert.Equal(500m, fee.AmountPaid);
        Assert.Equal(0m, fee.Balance);
        Assert.True(fee.IsFullyPaid);
    }

    [Fact]
    public void A_payment_can_never_exceed_the_balance_or_be_zero()
    {
        var fee = StudentFee.Create(Item(amount: 500m), Guid.CreateVersion7(), Now);
        fee.ApplyPayment(400m, Now);

        AssertRejected("amount", () => fee.ApplyPayment(100.01m, Now));
        AssertRejected("amount", () => fee.ApplyPayment(0m, Now));
        Assert.Equal(400m, fee.AmountPaid);   // unchanged by the rejected calls
    }

    private static FeeItem Item(
        decimal amount = 5000m, Guid? facultyId = null, Guid? departmentId = null, FeeCategory category = FeeCategory.Tuition) =>
        FeeItem.Create("Tuition", "المصروفات", null, category, amount, null, facultyId, departmentId, Due, Admin, Now);

    private static void AssertRejected(string field, Action act) =>
        Assert.Equal(field, Assert.ThrowsAny<ArgumentException>(act).ParamName);
}
