using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BookingStudyRoom.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.AddAuthorization();
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=Data/booking-studyroom.db"));
builder.Services.AddIdentityCore<StaffUser>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>()
.AddSignInManager();
builder.Services.AddScoped<BookingService>();

var app = builder.Build();

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "Data"));
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();

    if (!await db.Rooms.AnyAsync())
    {
        for (var i = 1; i <= 15; i++) db.Rooms.Add(new StudyRoom { Name = $"Sala {i}", SortOrder = i });
        await db.SaveChangesAsync();
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<StaffUser>>();
    if (!userManager.Users.Any())
    {
        var admin = new StaffUser { UserName = "admin", DisplayName = "Administrador", IsActive = true };
        await userManager.CreateAsync(admin, "biblioteca");
    }
}

if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error", createScopeForErrors: true);
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/account/login", async (HttpContext context, SignInManager<StaffUser> signInManager, UserManager<StaffUser> userManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var userName = form["username"].ToString();
    var password = form["password"].ToString();
    var user = await userManager.FindByNameAsync(userName);
    if (user is null || !user.IsActive) return Results.Redirect("/login?error=1");
    var result = await signInManager.PasswordSignInAsync(user, password, true, lockoutOnFailure: false);
    return Results.Redirect(result.Succeeded ? "/" : "/login?error=1");
}).DisableAntiforgery();

app.MapPost("/account/logout", async (SignInManager<StaffUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
}).DisableAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<StaffUser>(options)
{
    public DbSet<StudyRoom> Rooms => Set<StudyRoom>();
    public DbSet<Patron> Patrons => Set<Patron>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Incident> Incidents => Set<Incident>();
}

public sealed class StaffUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class StudyRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Patron
{
    public int Id { get; set; }
    public string Rut { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime LastSeenAt { get; set; } = DateTime.Now;
}

public enum BookingStatus { Reserved, InUse, Completed, Cancelled }

public sealed class Booking
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public StudyRoom? Room { get; set; }
    public int PatronId { get; set; }
    public Patron? Patron { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool MarkerAndEraser { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Reserved;
    public string? CheckoutComment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public sealed class Incident
{
    public int Id { get; set; }
    public int PatronId { get; set; }
    public Patron? Patron { get; set; }
    public int? BookingId { get; set; }
    public Booking? Booking { get; set; }
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public sealed record PatronHistory(Patron Patron, int TotalBookings, IReadOnlyList<Incident> Incidents)
{
    public bool IsRepeatOffender => Incidents.Count >= 2;
}

public sealed class BookingService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<StudyRoom>> RoomsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Rooms.Where(r => r.IsActive).OrderBy(r => r.SortOrder).ToListAsync();
    }

    public async Task<StudyRoom> AddRoomAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var next = (await db.Rooms.MaxAsync(r => (int?)r.SortOrder) ?? 0) + 1;
        var room = new StudyRoom { Name = $"Sala {next}", SortOrder = next };
        db.Rooms.Add(room); await db.SaveChangesAsync(); return room;
    }

    public async Task<List<Booking>> DayBookingsAsync(DateTime day)
    {
        var from = day.Date; var to = from.AddDays(1);
        await using var db = await factory.CreateDbContextAsync();
        return await db.Bookings.Include(b => b.Room).Include(b => b.Patron)
            .Where(b => b.StartAt >= from && b.StartAt < to && b.Status != BookingStatus.Cancelled)
            .OrderBy(b => b.StartAt).ToListAsync();
    }

    public async Task<(Booking? Booking, string? Error)> CreateAsync(int roomId, string rut, string name, DateTime start, DateTime end, bool marker)
    {
        rut = NormalizeRut(rut);
        if (!IsValidRut(rut)) return (null, "El RUT ingresado no es válido.");
        if (end <= start) return (null, "La hora de término debe ser posterior al inicio.");
        await using var db = await factory.CreateDbContextAsync();
        var overlap = await db.Bookings.AnyAsync(b => b.RoomId == roomId && b.Status != BookingStatus.Cancelled && b.StartAt < end && start < b.EndAt);
        if (overlap) return (null, "La sala ya tiene una reserva en ese horario.");
        var patron = await db.Patrons.FirstOrDefaultAsync(p => p.Rut == rut);
        if (patron is null) { patron = new Patron { Rut = rut, Name = name.Trim() }; db.Patrons.Add(patron); }
        else { patron.Name = name.Trim(); patron.LastSeenAt = DateTime.Now; }
        var booking = new Booking { RoomId = roomId, Patron = patron, StartAt = start, EndAt = end, MarkerAndEraser = marker };
        db.Bookings.Add(booking); await db.SaveChangesAsync(); return (booking, null);
    }

    public async Task<PatronHistory?> HistoryByRutAsync(string rut)
    {
        rut = NormalizeRut(rut);
        await using var db = await factory.CreateDbContextAsync();
        var patron = await db.Patrons.FirstOrDefaultAsync(p => p.Rut == rut);
        if (patron is null) return null;
        var count = await db.Bookings.CountAsync(b => b.PatronId == patron.Id && b.Status != BookingStatus.Cancelled);
        var incidents = await db.Incidents.Where(i => i.PatronId == patron.Id).OrderByDescending(i => i.CreatedAt).ToListAsync();
        return new PatronHistory(patron, count, incidents);
    }

    public async Task CheckoutAsync(int bookingId, string comment, bool incident)
    {
        await using var db = await factory.CreateDbContextAsync();
        var booking = await db.Bookings.FindAsync(bookingId); if (booking is null) return;
        booking.Status = BookingStatus.Completed; booking.CheckoutComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (incident && !string.IsNullOrWhiteSpace(comment)) db.Incidents.Add(new Incident { PatronId = booking.PatronId, BookingId = booking.Id, Comment = comment.Trim() });
        await db.SaveChangesAsync();
    }

    public async Task CancelAsync(int bookingId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var booking = await db.Bookings.FindAsync(bookingId); if (booking is null) return;
        booking.Status = BookingStatus.Cancelled; await db.SaveChangesAsync();
    }

    public static string NormalizeRut(string rut) => rut.Replace(".", "").Replace("-", "").Trim().ToUpperInvariant();
    public static bool IsValidRut(string rut)
    {
        rut = NormalizeRut(rut); if (rut.Length < 2) return false;
        var body = rut[..^1]; var dv = rut[^1]; if (!body.All(char.IsDigit)) return false;
        var sum = 0; var factor = 2;
        for (var i = body.Length - 1; i >= 0; i--) { sum += (body[i] - '0') * factor; factor = factor == 7 ? 2 : factor + 1; }
        var calc = 11 - (sum % 11); var expected = calc == 11 ? '0' : calc == 10 ? 'K' : (char)('0' + calc);
        return dv == expected;
    }
}
