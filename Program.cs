using BookingStudyRoom.Components;
using BookingStudyRoom.Data;
using BookingStudyRoom.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("BookingDb")
        ?? "Data Source=DataStore/booking-studyroom.db"));

builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "DataStore"));

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();

    if (!await db.Rooms.AnyAsync())
    {
        for (var i = 1; i <= 15; i++)
            db.Rooms.Add(new Room { Number = i, Name = $"Sala {i}", IsActive = true });

        await db.SaveChangesAsync();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
