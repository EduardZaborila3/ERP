using ErpServer.Data;
using ErpServer.Dtos.AppRoles;
using ErpServer.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpServer.Endpoints;

public static class RolesEndpoints
{
    const string GetRoleEndpointName = "GetRole";

    public static void MapRolesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/roles");

        // GET /roles
        group.MapGet("/", async (ErpDbContext dbContext) =>
            await dbContext.Roles
                           .Select(r => new AppRoleDto(r.Id, r.Name))
                           .AsNoTracking()
                           .ToListAsync()
        );

        // GET /roles/{id}
        group.MapGet("/{id:int}", async (int id, ErpDbContext dbContext) =>
        {
            var roleEntity = await dbContext.Roles
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync(r => r.Id == id);
            return roleEntity is null 
                ? Results.NotFound() 
                : Results.Ok(new AppRoleDto(roleEntity.Id, roleEntity.Name));
        })
        .WithName(GetRoleEndpointName);

        // POST /roles
        group.MapPost("/", async (CreateAppRoleDto createAppRoleDto, ErpDbContext dbContext) =>
        {
            var roleEntity = new AppRole
            {
                Name = createAppRoleDto.Name
            };

            dbContext.Roles.Add(roleEntity);
            await dbContext.SaveChangesAsync();

            var roleDto = new AppRoleDto(
                roleEntity.Id,
                roleEntity.Name
            );

            return Results.CreatedAtRoute(GetRoleEndpointName, new { id = roleEntity.Id }, roleDto);
        });

        // PUT /roles/{id}
        group.MapPut("/{id:int}", async (int id, UpdateAppRoleDto updateAppRoleDto, ErpDbContext dbContext) =>
        {
            var roleEntity = await dbContext.Roles.FindAsync(id);
            if (roleEntity is null)
            {
                return Results.NotFound();
            }

            roleEntity.Name = updateAppRoleDto.Name;

            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        // DELETE /roles/{id}
        group.MapDelete("/{id:int}", async (int id, ErpDbContext dbContext) =>
        {
            var roleEntity = await dbContext.Roles.FindAsync(id);
            if (roleEntity is null)
            {
                return Results.NotFound();
            }

            // Check if any users are assigned to this role before deleting
            var hasUsers = await dbContext.Users.AnyAsync(u => u.AppRoleId == id);
            if (hasUsers)
            {
                return Results.BadRequest($"Cannot delete role with ID {id} because it is assigned to one or more users.");
            }

            dbContext.Roles.Remove(roleEntity);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
