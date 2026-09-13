using System.Security.Claims;
using ErpServer.Data;
using ErpServer.Dtos.Tasks;
using ErpServer.Models;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ErpServer.Models.TaskStatus;

namespace ErpServer.Endpoints;

public static class TasksEndpoints
{
    const string GetTaskEndpointName = "GetTask";

    public static void MapTasksEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tasks");

        // Helper to get current user ID from JWT claims
        static int? GetCurrentUserId(ClaimsPrincipal principal)
        {
            var claimValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claimValue, out var id) ? id : null;
        }

        // Helper to convert AppTask + Owner to TaskDto
        static TaskDto MapToDto(AppTask task, TaskOwnerDto? owner)
        {
            return new TaskDto(
                task.Id,
                task.OwnerId,
                owner,
                task.Title,
                task.Description,
                task.Status,
                task.CreatedAt,
                task.Deadline,
                task.Assignees.Select(a => new TaskAssigneeDto(a.Id, a.Email, a.FirstName, a.LastName)).ToList()
            );
        }

        // GET /tasks
        group.MapGet("/", async (
            ErpDbContext dbContext,
            TaskStatus? status,
            int? assigneeId,
            int? ownerId,
            string? search) =>
        {
            var query = dbContext.Tasks
                .Include(t => t.Assignees)
                .AsNoTracking()
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }
            else
            {
                query = query.Where(t => t.Status == TaskStatus.Pending || 
                                         t.Status == TaskStatus.InProgress || 
                                         t.Status == TaskStatus.Completed);
            }

            if (assigneeId.HasValue)
            {
                query = query.Where(t => t.Assignees.Any(a => a.Id == assigneeId.Value));
            }

            if (ownerId.HasValue)
            {
                query = query.Where(t => t.OwnerId == ownerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(lowerSearch) || 
                                         t.Description.ToLower().Contains(lowerSearch));
            }

            var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            var ownerIds = tasks.Select(t => t.OwnerId).Distinct().ToList();
            var owners = await dbContext.Users
                .IgnoreQueryFilters()
                .Where(u => ownerIds.Contains(u.Id))
                .Select(u => new TaskOwnerDto(u.Id, u.Email, u.FirstName, u.LastName))
                .ToDictionaryAsync(u => u.Id);

            var result = tasks.Select(t => MapToDto(t, owners.GetValueOrDefault(t.OwnerId))).ToList();
            return Results.Ok(result);
        });

        // GET /tasks/summary
        group.MapGet("/summary", async (ErpDbContext dbContext) =>
        {
            var now = DateTime.UtcNow;
            var pending = await dbContext.Tasks.CountAsync(t => t.Status == TaskStatus.Pending);
            var inProgress = await dbContext.Tasks.CountAsync(t => t.Status == TaskStatus.InProgress);
            var completed = await dbContext.Tasks.CountAsync(t => t.Status == TaskStatus.Completed);
            var total = pending + inProgress + completed;
            var overdue = await dbContext.Tasks.CountAsync(t => 
                t.Deadline != null && 
                t.Deadline < now && 
                t.Status != TaskStatus.Completed);

            return Results.Ok(new TaskSummaryDto(
                total,
                pending,
                inProgress,
                completed,
                overdue
            ));
        });

        // GET /tasks/{id}
        group.MapGet("/{id:int}", async (int id, ErpDbContext dbContext) =>
        {
            var task = await dbContext.Tasks
                .Include(t => t.Assignees)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task is null)
            {
                return Results.NotFound();
            }

            var ownerUser = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == task.OwnerId);

            var ownerDto = ownerUser != null 
                ? new TaskOwnerDto(ownerUser.Id, ownerUser.Email, ownerUser.FirstName, ownerUser.LastName) 
                : null;

            return Results.Ok(MapToDto(task, ownerDto));
        })
        .WithName(GetTaskEndpointName);

        // POST /tasks
        group.MapPost("/", async (
            CreateTaskDto dto, 
            ErpDbContext dbContext, 
            ClaimsPrincipal claimsPrincipal) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return Results.BadRequest(new { message = "Titlul task-ului este obligatoriu." });
            }

            if (string.IsNullOrWhiteSpace(dto.Description))
            {
                return Results.BadRequest(new { message = "Descrierea task-ului este obligatorie." });
            }

            var currentUserId = GetCurrentUserId(claimsPrincipal);
            int ownerId;

            if (currentUserId.HasValue)
            {
                ownerId = currentUserId.Value;
            }
            else
            {
                // Fallback to the first active user if not authenticated
                var fallbackUser = await dbContext.Users.FirstOrDefaultAsync();
                ownerId = fallbackUser?.Id ?? 1;
            }

            var assignees = new List<User>();
            if (dto.AssigneeIds != null && dto.AssigneeIds.Count > 0)
            {
                assignees = await dbContext.Users
                    .Where(u => dto.AssigneeIds.Contains(u.Id))
                    .ToListAsync();
            }

            DateTime? deadlineUtc = dto.Deadline.HasValue
                ? DateTime.SpecifyKind(dto.Deadline.Value, DateTimeKind.Utc)
                : null;

            var task = new AppTask
            {
                OwnerId = ownerId,
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Status = dto.Status,
                CreatedAt = DateTime.UtcNow,
                Deadline = deadlineUtc,
                Assignees = assignees
            };

            dbContext.Tasks.Add(task);
            await dbContext.SaveChangesAsync();

            var ownerUser = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == ownerId);

            var ownerDto = ownerUser != null
                ? new TaskOwnerDto(ownerUser.Id, ownerUser.Email, ownerUser.FirstName, ownerUser.LastName)
                : null;

            var taskDto = MapToDto(task, ownerDto);
            return Results.CreatedAtRoute(GetTaskEndpointName, new { id = task.Id }, taskDto);
        });

        // PUT /tasks/{id}
        group.MapPut("/{id:int}", async (
            int id, 
            UpdateTaskDto dto, 
            ErpDbContext dbContext) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return Results.BadRequest(new { message = "Titlul task-ului este obligatoriu." });
            }

            if (string.IsNullOrWhiteSpace(dto.Description))
            {
                return Results.BadRequest(new { message = "Descrierea task-ului este obligatorie." });
            }

            var task = await dbContext.Tasks
                .Include(t => t.Assignees)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task is null)
            {
                return Results.NotFound(new { message = $"Task-ul cu ID {id} nu a fost găsit." });
            }

            DateTime? deadlineUtc = dto.Deadline.HasValue
                ? DateTime.SpecifyKind(dto.Deadline.Value, DateTimeKind.Utc)
                : null;

            task.Title = dto.Title.Trim();
            task.Description = dto.Description.Trim();
            task.Status = dto.Status;
            task.Deadline = deadlineUtc;

            if (dto.AssigneeIds != null)
            {
                task.Assignees.Clear();
                if (dto.AssigneeIds.Count > 0)
                {
                    var updatedAssignees = await dbContext.Users
                        .Where(u => dto.AssigneeIds.Contains(u.Id))
                        .ToListAsync();
                    
                    foreach (var user in updatedAssignees)
                    {
                        task.Assignees.Add(user);
                    }
                }
            }

            await dbContext.SaveChangesAsync();

            var ownerUser = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == task.OwnerId);

            var ownerDto = ownerUser != null
                ? new TaskOwnerDto(ownerUser.Id, ownerUser.Email, ownerUser.FirstName, ownerUser.LastName)
                : null;

            var taskDto = MapToDto(task, ownerDto);
            return Results.Ok(taskDto);
        });

        // PATCH /tasks/{id}/status
        group.MapPatch("/{id:int}/status", async (
            int id, 
            UpdateTaskStatusDto dto, 
            ErpDbContext dbContext) =>
        {
            var task = await dbContext.Tasks
                .Include(t => t.Assignees)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task is null)
            {
                return Results.NotFound(new { message = $"Task-ul cu ID {id} nu a fost găsit." });
            }

            task.Status = dto.Status;
            await dbContext.SaveChangesAsync();

            var ownerUser = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == task.OwnerId);

            var ownerDto = ownerUser != null
                ? new TaskOwnerDto(ownerUser.Id, ownerUser.Email, ownerUser.FirstName, ownerUser.LastName)
                : null;

            var taskDto = MapToDto(task, ownerDto);
            return Results.Ok(taskDto);
        });

        // DELETE /tasks/{id}
        group.MapDelete("/{id:int}", async (int id, ErpDbContext dbContext) =>
        {
            var task = await dbContext.Tasks
                .Include(t => t.Assignees)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task is null)
            {
                return Results.NotFound(new { message = $"Task-ul cu ID {id} nu a fost găsit." });
            }

            dbContext.Tasks.Remove(task);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
