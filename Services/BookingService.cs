using BookingStudyRoom.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingStudyRoom.Services;

public sealed class BookingService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<Room>> GetRoomsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Rooms.Where(x => x.IsActive).OrderBy(x => x.Number).AsNoTracking().ToListAsync();
    }

    public async Task<Room> AddRoomAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var next = (await db.Rooms.MaxAsync(x => (int?)x.Number) ?? 0) + 1;
        var room = new Room { Number = next, Name = $"Sala {next}", IsActive = true };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    public async Task<List<Booking>> GetBookingsForDayAsync(DateTime day)
    {
        var from = day.Date;
        var to = from.AddDays(1);

        await using var db = await factory.CreateDbContextAsync();
        return await db.Bookings
            .Include(x => x.Room)
            .Where(x => !x.IsCancelled && x.StartAt < to && x.EndAt > from)
            .OrderBy(x => x.StartAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<(bool Ok, string? Error, Booking? Booking)> CreateBookingAsync(
        int roomId,
        DateTime start,
        DateTime end,
        string rut,
        string name,
        bool markerAndEraser,
        int? staffUserId)
    {
        rut = NormalizeRut(rut);
        name = name.Trim();

        var validation = ValidateBookingInput(start, end, rut, name);
        if (validation is not null)
            return (false, validation, null);

        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Rooms.AnyAsync(x => x.Id == roomId && x.IsActive))
            return (false, "La sala no está disponible.", null);

        var overlaps = await db.Bookings.AnyAsync(x =>
            x.RoomId == roomId && !x.IsCancelled && start < x.EndAt && end > x.StartAt);

        if (overlaps)
            return (false, "Ese horario ya fue reservado para esta sala.", null);

        var booking = new Booking
        {
            RoomId = roomId,
            StartAt = start,
            EndAt = end,
            Rut = rut,
            PatronName = name,
            MarkerAndEraser = markerAndEraser,
            CreatedByStaffUserId = staffUserId
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return (true, null, booking);
    }

    public async Task<(bool Ok, string? Error)> UpdateBookingAsync(
        int bookingId,
        int roomId,
        DateTime start,
        DateTime end,
        string rut,
        string name,
        bool markerAndEraser)
    {
        rut = NormalizeRut(rut);
        name = name.Trim();
        var validation = ValidateBookingInput(start, end, rut, name);
        if (validation is not null)
            return (false, validation);

        await using var db = await factory.CreateDbContextAsync();
        var booking = await db.Bookings.FindAsync(bookingId);
        if (booking is null || booking.IsCancelled)
            return (false, "Reserva no encontrada.");

        var overlaps = await db.Bookings.AnyAsync(x =>
            x.Id != bookingId && x.RoomId == roomId && !x.IsCancelled && start < x.EndAt && end > x.StartAt);

        if (overlaps)
            return (false, "Ese horario ya fue reservado para esta sala.");

        booking.RoomId = roomId;
        booking.StartAt = start;
        booking.EndAt = end;
        booking.Rut = rut;
        booking.PatronName = name;
        booking.MarkerAndEraser = markerAndEraser;
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task DeliverAsync(int bookingId, string? comment, bool incident)
    {
        await using var db = await factory.CreateDbContextAsync();
        var booking = await db.Bookings.FindAsync(bookingId);
        if (booking is null) return;

        booking.IsDelivered = true;
        booking.DeliveredAt = DateTime.Now;
        booking.ReturnComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        booking.IsIncident = incident;
        await db.SaveChangesAsync();
    }

    public async Task CancelAsync(int bookingId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var booking = await db.Bookings.FindAsync(bookingId);
        if (booking is null) return;
        booking.IsCancelled = true;
        await db.SaveChangesAsync();
    }

    public async Task<PatronSummary?> GetPatronSummaryAsync(string rut)
    {
        rut = NormalizeRut(rut);
        if (rut.Length < 2) return null;

        await using var db = await factory.CreateDbContextAsync();
        var history = await db.Bookings
            .Where(x => !x.IsCancelled && x.Rut == rut)
            .OrderByDescending(x => x.StartAt)
            .AsNoTracking()
            .ToListAsync();

        if (history.Count == 0) return null;
        return new PatronSummary(
            rut,
            history[0].PatronName,
            history.Count,
            history.Count(x => x.IsIncident),
            history.Max(x => (DateTime?)x.StartAt));
    }

    public async Task<List<Booking>> GetPatronHistoryAsync(string rut)
    {
        rut = NormalizeRut(rut);
        await using var db = await factory.CreateDbContextAsync();
        return await db.Bookings
            .Include(x => x.Room)
            .Where(x => !x.IsCancelled && x.Rut == rut)
            .OrderByDescending(x => x.StartAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public static string NormalizeRut(string rut)
        => new(rut.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    public static bool IsValidChileanRut(string rut)
    {
        rut = NormalizeRut(rut);
        if (rut.Length < 2) return false;

        var body = rut[..^1];
        var dv = rut[^1];
        if (!body.All(char.IsDigit)) return false;

        var sum = 0;
        var multiplier = 2;
        for (var i = body.Length - 1; i >= 0; i--)
        {
            sum += (body[i] - '0') * multiplier;
            multiplier = multiplier == 7 ? 2 : multiplier + 1;
        }

        var result = 11 - (sum % 11);
        var expected = result switch
        {
            11 => '0',
            10 => 'K',
            _ => (char)('0' + result)
        };
        return dv == expected;
    }

    public static string FormatRut(string rut)
    {
        rut = NormalizeRut(rut);
        if (rut.Length < 2) return rut;
        var body = rut[..^1];
        var dv = rut[^1];
        if (!long.TryParse(body, out var number)) return rut;
        return $"{number:N0}-{dv}".Replace(',', '.');
    }

    private static string? ValidateBookingInput(DateTime start, DateTime end, string rut, string name)
    {
        if (!IsValidChileanRut(rut))
            return "El RUT ingresado no es válido.";
        if (name.Length < 2)
            return "Ingresa el nombre de la persona.";
        if (end <= start)
            return "La hora de término debe ser posterior a la de inicio.";
        if (start.Date != end.AddSeconds(-1).Date)
            return "La reserva debe comenzar y terminar el mismo día.";
        return null;
    }
}
