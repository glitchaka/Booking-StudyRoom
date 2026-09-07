namespace BookingStudyRoom.Data;

public sealed class Room
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<Booking> Bookings { get; set; } = [];
}

public sealed class Booking
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room? Room { get; set; }
    public string Rut { get; set; } = string.Empty;
    public string PatronName { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool MarkerAndEraser { get; set; }
    public bool IsDelivered { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? ReturnComment { get; set; }
    public bool IsIncident { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? CreatedByStaffUserId { get; set; }
    public StaffUser? CreatedByStaffUser { get; set; }
}

public sealed class StaffUser
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public sealed record PatronSummary(
    string Rut,
    string LastKnownName,
    int TotalUses,
    int IncidentCount,
    DateTime? LastUse)
{
    public bool IsRepeatOffender => IncidentCount >= 2;
}
