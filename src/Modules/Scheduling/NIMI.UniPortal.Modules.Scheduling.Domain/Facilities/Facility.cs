namespace NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;

/// <summary>
/// A hall, lab or other teaching space (P3.4). <see cref="FacultyId"/> null means shared by the whole university;
/// otherwise the name is unique within that faculty. Removal is deactivation.
/// </summary>
public sealed class Facility
{
    private Facility()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Users faculty id — indexed, never a cross-schema FK. Null = university-shared.</summary>
    public Guid? FacultyId { get; private set; }

    public string Name { get; private set; } = null!;

    public FacilityType Type { get; private set; }

    /// <summary>Seats (1–5000).</summary>
    public int Capacity { get; private set; }

    public string? Building { get; private set; }

    public string? Floor { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Facility Create(
        Guid? facultyId,
        string name,
        FacilityType type,
        int capacity,
        string? building,
        string? floor,
        DateTimeOffset now)
    {
        var facility = new Facility
        {
            Id = Guid.CreateVersion7(),
            FacultyId = facultyId,
            IsActive = true,
            CreatedAt = now,
        };
        facility.Apply(name, type, capacity, building, floor);
        return facility;
    }

    public void Update(
        Guid? facultyId,
        string name,
        FacilityType type,
        int capacity,
        string? building,
        string? floor,
        bool isActive,
        DateTimeOffset now)
    {
        Apply(name, type, capacity, building, floor);
        FacultyId = facultyId;
        IsActive = isActive;
        UpdatedAt = now;
    }

    private void Apply(string name, FacilityType type, int capacity, string? building, string? floor)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Required.", nameof(name));
        if (trimmed.Length > 100)
            throw new ArgumentException("At most 100 characters.", nameof(name));
        Name = trimmed;

        if (!Enum.IsDefined(type))
            throw new ArgumentException("Unknown facility type.", nameof(type));

        if (capacity is < 1 or > 5000)
            throw new ArgumentException("Between 1 and 5000.", nameof(capacity));

        Type = type;
        Capacity = capacity;
        Building = Trimmed(building, 100, nameof(building));
        Floor = Trimmed(floor, 20, nameof(floor));
    }

    private static string? Trimmed(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"At most {maxLength} characters.", name);
        return trimmed;
    }
}
