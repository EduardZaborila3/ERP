using ErpServer.Data;
using ErpServer.Dtos.Users;
using ErpServer.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpServer.Endpoints;

public static class UsersEndpoints
{
    const string GetUserEndpointName = "GetUser";

    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users");

        // GET /users
        group.MapGet("/", async (ErpDbContext dbContext)
            => await dbContext.Users
                              .Where(u => u.DeletedAt == null)
                              .Select(user => new UserDetailsDto(
                                user.Id,
                                user.Email,
                                user.FirstName,
                                user.LastName,
                                user.AppRoleId,
                                user.JobTitleId,
                                user.IsActive
                              ))
                              .AsNoTracking()
                              .ToListAsync()
        );

        // GET /users/{id}
        group.MapGet("/{id:int}", async (int id, ErpDbContext dbContext) =>
        {
            var user = await dbContext.Users
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(u => u.Id == id);
            return user is null 
                ? Results.NotFound() 
                : Results.Ok(new UserDetailsDto(
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.AppRoleId,
                    user.JobTitleId,
                    user.IsActive
                  ));
        })
        .WithName(GetUserEndpointName);

        // POST /users
        group.MapPost("/", async (CreateUserDto userDto, ErpDbContext dbContext) =>
        {
            // Validate the required fields
            if (string.IsNullOrWhiteSpace(userDto.Email))
            {
                return Results.BadRequest("Email field is empty.");
            }
            var email = userDto.Email.Trim();
            var firstName = string.IsNullOrWhiteSpace(userDto.FirstName) ? email.Split('@')[0] : userDto.FirstName;
            var lastName = string.IsNullOrWhiteSpace(userDto.LastName) ? "Utilizator" : userDto.LastName;

            // Validate AppRoleId exists
            var roleExists = await dbContext.Roles.AnyAsync(r => r.Id == userDto.AppRoleId);
            if (!roleExists)
            {
                return Results.BadRequest($"AppRole with ID {userDto.AppRoleId} does not exist.");
            }

            // Validate JobTitleId exists
            var jobTitleExists = await dbContext.JobTitles.AnyAsync(jt => jt.Id == userDto.JobTitleId);
            if (!jobTitleExists)
            {
                return Results.BadRequest($"JobTitle with ID {userDto.JobTitleId} does not exist.");
            }

            var existingUser = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (existingUser != null)
            {
                if (existingUser.DeletedAt == null)
                {
                    return Results.BadRequest(new { message = "Există deja un cont cu această adresă de email." });
                }

                // Reactivate soft-deleted user
                existingUser.FirstName = firstName;
                existingUser.LastName = lastName;
                existingUser.AppRoleId = userDto.AppRoleId;
                existingUser.JobTitleId = userDto.JobTitleId;
                existingUser.IsActive = userDto.IsActive;
                existingUser.DeletedAt = null;
                await dbContext.SaveChangesAsync();

                var updatedDto = new UserDetailsDto(
                    existingUser.Id,
                    existingUser.Email,
                    existingUser.FirstName,
                    existingUser.LastName,
                    existingUser.AppRoleId,
                    existingUser.JobTitleId,
                    existingUser.IsActive
                );
                return Results.Ok(updatedDto);
            }

            var user = new User
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                AppRoleId = userDto.AppRoleId,
                JobTitleId = userDto.JobTitleId,
                IsActive = userDto.IsActive,
                CreatedAt = DateTime.UtcNow,
                DeletedAt = null
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var userDetailsDto = new UserDetailsDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.AppRoleId,
                user.JobTitleId,
                user.IsActive
            );

            return Results.CreatedAtRoute(GetUserEndpointName, new { id = user.Id }, userDetailsDto);
        });

        // PUT /users/{id}
        group.MapPut("/{id:int}", async (int id, UpdateUserDto userDto, ErpDbContext dbContext) =>
        {
            var user = await dbContext.Users.FindAsync(id);
            if (user is null)
            {
                return Results.NotFound();
            }

            // Validate AppRoleId exists
            var roleExists = await dbContext.Roles.AnyAsync(r => r.Id == userDto.AppRoleId);
            if (!roleExists)
            {
                return Results.BadRequest($"AppRole with ID {userDto.AppRoleId} does not exist.");
            }

            // Validate JobTitleId exists
            var jobTitleExists = await dbContext.JobTitles.AnyAsync(jt => jt.Id == userDto.JobTitleId);
            if (!jobTitleExists)
            {
                return Results.BadRequest($"JobTitle with ID {userDto.JobTitleId} does not exist.");
            }

            user.Email = userDto.Email;
            user.FirstName = userDto.FirstName;
            user.LastName = userDto.LastName;
            user.AppRoleId = userDto.AppRoleId;
            user.JobTitleId = userDto.JobTitleId;
            user.IsActive = userDto.IsActive;

            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        // DELETE /users/{id}
        group.MapDelete("/{id:int}", async (int id, ErpDbContext dbContext) =>
        {
            var user = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
            {
                return Results.NotFound();
            }

            user.DeletedAt = DateTime.UtcNow;
            user.IsActive = false;
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiresAt = null;
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        // POST /users/invite-email
        group.MapPost("/invite-email", async (InviteEmailDto dto, ErpDbContext dbContext, ErpServer.Services.IEmailService emailService) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return Results.BadRequest("Email field is empty.");
            }

            var email = dto.Email.Trim().ToLower();
            var user = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            var resetToken = System.Security.Cryptography.RandomNumberGenerator.GetHexString(32);

            if (user != null)
            {
                user.PasswordResetToken = resetToken;
                user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(48);
                await dbContext.SaveChangesAsync();
            }

            var redirectUrl = dto.RedirectUrl ?? "http://localhost:5173";
            var encodedEmail = Uri.EscapeDataString(dto.Email.Trim());
            var encodedToken = Uri.EscapeDataString(resetToken);
            var setPasswordUrl = $"{redirectUrl}/#type=recovery&email={encodedEmail}&token={encodedToken}";

            var subject = "Setare Parolă Cont ERP";
            var body = $@"
                <div style=""font-family: Arial, sans-serif; padding: 24px; color: #1e293b; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 8px;"">
                    <h2 style=""color: #0f172a;"">Bun venit în sistemul Enterprise ERP!</h2>
                    <p style=""font-size: 1rem; line-height: 1.5;"">
                        A fost generat un cont de utilizator pentru <strong>{dto.FirstName} {dto.LastName}</strong> ({dto.Email}).
                    </p>
                    <p style=""font-size: 1rem; line-height: 1.5;"">
                        Pentru a vă seta parola personală și a vă activa accesul în aplicație, apăsați pe butonul de mai jos (link valabil 48 ore):
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
                await emailService.SendEmailAsync(dto.Email, subject, body);
                return Results.Ok(new { message = "Invitation email sent successfully." });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to send email: {ex.Message}");
            }
        });
    }
}
