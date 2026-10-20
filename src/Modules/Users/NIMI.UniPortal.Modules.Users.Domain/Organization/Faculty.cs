namespace NIMI.UniPortal.Modules.Users.Domain.Organization;

/// <summary>A faculty / college (كلية) of the university (decision ORG). Deactivated, never deleted.</summary>
public sealed class Faculty
{
    private Faculty()
    {
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string NameAr { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Faculty Create(string code, string name, string nameAr, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Code = OrgCode.Normalize(code, nameof(code)),
            Name = OrgCode.Name(name, nameof(name)),
            NameAr = OrgCode.Name(nameAr, nameof(nameAr)),
            IsActive = true,
            CreatedAt = now,
        };

    public void Update(string code, string name, string nameAr, bool isActive, DateTimeOffset now)
    {
        Code = OrgCode.Normalize(code, nameof(code));
        Name = OrgCode.Name(name, nameof(name));
        NameAr = OrgCode.Name(nameAr, nameof(nameAr));
        IsActive = isActive;
        UpdatedAt = now;
    }
}
