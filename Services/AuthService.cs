using BookingStudyRoom.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookingStudyRoom.Services;

public sealed class AuthService(IDbContextFactory<AppDbContext> factory)
{
    private readonly PasswordHasher<StaffUser> _hasher = new();

    public StaffUser? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;

    public async Task<bool> HasUsersAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.StaffUsers.AnyAsync();
    }

    public async Task<(bool Ok, string? Error)> CreateFirstAdminAsync(string displayName, string username, string password)
    {
        if (await HasUsersAsync())
            return (false, "La configuración inicial ya fue realizada.");

        return await CreateUserInternalAsync(displayName, username, password, true, signInAfterCreate: true);
    }

    public async Task<(bool Ok, string? Error)> LoginAsync(string username, string password)
    {
        username = username.Trim().ToLowerInvariant();

        await using var db = await factory.CreateDbContextAsync();
        var user = await db.StaffUsers.FirstOrDefaultAsync(x => x.Username == username);

        if (user is null || !user.IsActive)
            return (false, "Usuario o contraseña incorrectos.");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            return (false, "Usuario o contraseña incorrectos.");

        CurrentUser = user;
        return (true, null);
    }

    public void Logout() => CurrentUser = null;

    public async Task<List<StaffUser>> GetUsersAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.StaffUsers.OrderBy(x => x.DisplayName).AsNoTracking().ToListAsync();
    }

    public Task<(bool Ok, string? Error)> CreateUserAsync(string displayName, string username, string password, bool isAdmin)
        => CreateUserInternalAsync(displayName, username, password, isAdmin, signInAfterCreate: false);

    private async Task<(bool Ok, string? Error)> CreateUserInternalAsync(
        string displayName,
        string username,
        string password,
        bool isAdmin,
        bool signInAfterCreate)
    {
        displayName = displayName.Trim();
        username = username.Trim().ToLowerInvariant();

        if (displayName.Length < 2)
            return (false, "Ingresa el nombre del funcionario.");
        if (username.Length < 3)
            return (false, "El usuario debe tener al menos 3 caracteres.");
        if (password.Length < 6)
            return (false, "La contraseña debe tener al menos 6 caracteres.");

        await using var db = await factory.CreateDbContextAsync();
        if (await db.StaffUsers.AnyAsync(x => x.Username == username))
            return (false, "Ese nombre de usuario ya existe.");

        var user = new StaffUser
        {
            DisplayName = displayName,
            Username = username,
            IsAdmin = isAdmin,
            IsActive = true
        };
        user.PasswordHash = _hasher.HashPassword(user, password);

        db.StaffUsers.Add(user);
        await db.SaveChangesAsync();

        if (signInAfterCreate)
            CurrentUser = user;

        return (true, null);
    }

    public async Task SetActiveAsync(int userId, bool isActive)
    {
        await using var db = await factory.CreateDbContextAsync();
        var user = await db.StaffUsers.FindAsync(userId);
        if (user is null) return;

        user.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    public async Task<(bool Ok, string? Error)> ChangePasswordAsync(int userId, string password)
    {
        if (password.Length < 6)
            return (false, "La contraseña debe tener al menos 6 caracteres.");

        await using var db = await factory.CreateDbContextAsync();
        var user = await db.StaffUsers.FindAsync(userId);
        if (user is null)
            return (false, "Usuario no encontrado.");

        user.PasswordHash = _hasher.HashPassword(user, password);
        await db.SaveChangesAsync();
        return (true, null);
    }
}
