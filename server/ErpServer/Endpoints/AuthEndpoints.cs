using System.Security.Claims;
using System.Security.Cryptography;
using ErpServer.Data;
using ErpServer.Dtos.Auth;
using ErpServer.Dtos.Users;
using ErpServer.Models;
using ErpServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ErpServer.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        // POST /auth/login
        group.MapPost("/login", async (
            LoginDto dto, 
            ErpDbContext dbContext, 
            IPasswordHasher<User> passwordHasher, 
            ITokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return Results.BadRequest(new { message = "Emailul și parola sunt obligatorii." });
            }

            var email = dto.Email.Trim().ToLower();
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                return Results.BadRequest(new { message = "Email sau parolă incorectă." });
            }

            if (!user.IsActive)
            {
                return Results.BadRequest(new { message = "Contul este dezactivat sau în așteptarea aprobării administratorului." });
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                return Results.BadRequest(new { 
                    message = "Contul nu are o parolă setată încă. Vă rugăm să folosiți link-ul primit pe email sau opțiunea de Setare / Resetare Parolă." 
                });
            }

            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Results.BadRequest(new { message = "Email sau parolă incorectă." });
            }

            var role = await dbContext.Roles.FindAsync(user.AppRoleId);
            var token = tokenService.GenerateToken(user, role?.Name);

            var userDto = new UserDetailsDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.AppRoleId,
                user.JobTitleId,
                user.IsActive
            );

            return Results.Ok(new AuthResponseDto(token, userDto));
        });

        // POST /auth/register
        group.MapPost("/register", async (
            RegisterDto dto, 
            ErpDbContext dbContext, 
            IPasswordHasher<User> passwordHasher, 
            ITokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return Results.BadRequest(new { message = "Emailul și parola sunt obligatorii." });
            }

            var email = dto.Email.Trim().ToLower();
            var existingUser = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (existingUser != null)
            {
                return Results.BadRequest(new { message = "Există deja un cont cu această adresă de email." });
            }

            var roleExists = await dbContext.Roles.AnyAsync(r => r.Id == dto.AppRoleId);
            if (!roleExists)
            {
                return Results.BadRequest(new { message = $"Rolul cu ID {dto.AppRoleId} nu există." });
            }

            var jobTitleExists = await dbContext.JobTitles.AnyAsync(j => j.Id == dto.JobTitleId);
            if (!jobTitleExists)
            {
                return Results.BadRequest(new { message = $"Funcția cu ID {dto.JobTitleId} nu există." });
            }

            var user = new User
            {
                Email = email,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                AppRoleId = dto.AppRoleId,
                JobTitleId = dto.JobTitleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = passwordHasher.HashPassword(user, dto.Password);

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var role = await dbContext.Roles.FindAsync(user.AppRoleId);
            var token = tokenService.GenerateToken(user, role?.Name);

            var userDto = new UserDetailsDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.AppRoleId,
                user.JobTitleId,
                user.IsActive
            );

            return Results.Ok(new AuthResponseDto(token, userDto));
        });

        // POST /auth/set-password
        group.MapPost("/set-password", async (
            SetPasswordDto dto, 
            ErpDbContext dbContext, 
            IPasswordHasher<User> passwordHasher,
            ITokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return Results.BadRequest(new { message = "Toate câmpurile sunt obligatorii." });
            }

            if (dto.NewPassword.Length < 6)
            {
                return Results.BadRequest(new { message = "Parola trebuie să aibă cel puțin 6 caractere." });
            }

            var email = dto.Email.Trim().ToLower();
            var user = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                return Results.NotFound(new { message = "Utilizatorul nu a fost găsit." });
            }

            // Verify token
            if (string.IsNullOrEmpty(user.PasswordResetToken) || user.PasswordResetToken != dto.Token)
            {
                return Results.BadRequest(new { message = "Token-ul de resetare este invalid sau a expirat." });
            }

            if (user.PasswordResetTokenExpiresAt.HasValue && user.PasswordResetTokenExpiresAt.Value < DateTime.UtcNow)
            {
                return Results.BadRequest(new { message = "Token-ul de resetare a expirat. Vă rugăm să solicitați un link nou." });
            }

            // Update password and clear reset token
            user.PasswordHash = passwordHasher.HashPassword(user, dto.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiresAt = null;
            user.IsActive = true;
            user.DeletedAt = null;

            await dbContext.SaveChangesAsync();

            var role = await dbContext.Roles.FindAsync(user.AppRoleId);
            var token = tokenService.GenerateToken(user, role?.Name);

            var userDto = new UserDetailsDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.AppRoleId,
                user.JobTitleId,
                user.IsActive
            );

            return Results.Ok(new {
                message = "Parola a fost setată cu succes! Contul a fost activat.",
                token,
                user = userDto
            });
        });

        // POST /auth/forgot-password
        group.MapPost("/forgot-password", async (
            ForgotPasswordDto dto, 
            ErpDbContext dbContext, 
            IEmailService emailService) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return Results.BadRequest(new { message = "Adresa de email este obligatorie." });
            }

            var email = dto.Email.Trim().ToLower();
            var user = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user != null && user.DeletedAt == null)
            {
                var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                user.PasswordResetToken = resetToken;
                user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(48);

                await dbContext.SaveChangesAsync();

                var redirectUrl = dto.RedirectUrl ?? "http://localhost:5173";
                var encodedEmail = Uri.EscapeDataString(user.Email);
                var encodedToken = Uri.EscapeDataString(resetToken);
                var setPasswordUrl = $"{redirectUrl}/#type=recovery&email={encodedEmail}&token={encodedToken}";

                var subject = "Setare / Resetare Parolă Cont ERP";
                var body = $@"
                    <div style=""font-family: Arial, sans-serif; padding: 24px; color: #1e293b; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 8px;"">
                        <h2 style=""color: #0f172a;"">Enterprise ERP - Resetare Parolă</h2>
                        <p style=""font-size: 1rem; line-height: 1.5;"">
                            Bună ziua <strong>{user.FirstName} {user.LastName}</strong>,
                        </p>
                        <p style=""font-size: 1rem; line-height: 1.5;"">
                            A fost solicitată setarea / resetarea parolei pentru contul dumneavoastră asociat adresei ({user.Email}).
                        </p>
                        <p style=""font-size: 1rem; line-height: 1.5;"">
                            Pentru a alege o nouă parolă securizată, apăsați pe butonul de mai jos (link valabil 48 de ore):
                        </p>
                        <div style=""margin: 30px 0; text-align: center;"">
                            <a href=""{setPasswordUrl}"" style=""background-color: #10b981; color: #ffffff; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block; font-size: 1rem;"">Setează Parola</a>
                        </div>
                        <p style=""font-size: 0.9rem; color: #64748b;"">
                            Dacă butonul nu funcționează, copiați și deschideți următorul link în browser: <br />
                            <a href=""{setPasswordUrl}"" style=""color: #2563eb;"">{setPasswordUrl}</a>
                        </p>
                    </div>";

                try
                {
                    await emailService.SendEmailAsync(user.Email, subject, body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending reset password email: {ex.Message}");
                }
            }

            return Results.Ok(new { 
                message = "Dacă adresa de email există în sistem, veți primi un link pentru setarea parolei." 
            });
        });

        // GET /auth/me
        group.MapGet("/me", async (ClaimsPrincipal claimsPrincipal, ErpDbContext dbContext) =>
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || !user.IsActive)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new UserDetailsDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.AppRoleId,
                user.JobTitleId,
                user.IsActive
            ));
        }).RequireAuthorization();
    }
}
