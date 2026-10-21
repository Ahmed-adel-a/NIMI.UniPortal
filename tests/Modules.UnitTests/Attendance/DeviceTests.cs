// ACCEPTANCE TEST — written by the consultant for phase P06. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Attendance.Domain.Devices;

namespace NIMI.UniPortal.Modules.UnitTests.Attendance;

/// <summary>Decision DEVICE — one registered phone per student; changes go through Student Affairs.</summary>
public sealed class DeviceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 7, 0, 0, TimeSpan.Zero);
    private static readonly Guid Student = Guid.CreateVersion7();
    private static readonly Guid Admin = Guid.CreateVersion7();

    [Fact]
    public void A_device_is_registered_and_can_be_revoked()
    {
        var device = RegisteredDevice.Register(Student, "android-7f3a9c21", Now);

        Assert.Equal(Student, device.StudentId);
        Assert.Equal("android-7f3a9c21", device.Fingerprint);
        Assert.Null(device.RevokedAt);

        device.Revoke(Now.AddDays(3));

        Assert.Equal(Now.AddDays(3), device.RevokedAt);
    }

    [Fact]
    public void Fingerprints_have_sane_lengths()
    {
        Assert.Equal("deviceFingerprint", Assert.ThrowsAny<ArgumentException>(() => RegisteredDevice.Register(Student, "short", Now)).ParamName);
        Assert.Equal("deviceFingerprint", Assert.ThrowsAny<ArgumentException>(() => RegisteredDevice.Register(Student, new string('f', 201), Now)).ParamName);
    }

    [Fact]
    public void A_change_request_is_decided_once()
    {
        var approved = DeviceChangeRequest.Create(Student, "iphone-0b44e1aa", "Phone was stolen", Now);
        var rejected = DeviceChangeRequest.Create(Student, "iphone-0b44e1aa", "Want to use a tablet", Now);

        approved.Approve(Admin, Now.AddHours(1));
        rejected.Reject(Admin, Now.AddHours(1));

        Assert.Equal(DeviceChangeStatus.Approved, approved.Status);
        Assert.Equal(Admin, approved.DecidedBy);
        Assert.Equal(DeviceChangeStatus.Rejected, rejected.Status);
        Assert.Throws<InvalidOperationException>(() => approved.Reject(Admin, Now.AddHours(2)));
        Assert.Throws<InvalidOperationException>(() => rejected.Approve(Admin, Now.AddHours(2)));
    }

    [Fact]
    public void A_change_request_needs_a_reason()
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => DeviceChangeRequest.Create(Student, "iphone-0b44e1aa", " ", Now));

        Assert.Equal("reason", error.ParamName);
    }
}
