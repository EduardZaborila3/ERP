using ErpServer.Data;
using ErpServer.Dtos.JobTitles;
using ErpServer.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpServer.Endpoints;

public static class JobTitlesEndpoints
{
    const string GetJobTitleEndpointName = "GetJobTitle";

    public static void MapJobTitlesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/job-titles");

        // GET /job-titles
        group.MapGet("/", async (ErpDbContext dbContext) =>
            await dbContext.JobTitles
                           .Select(jt => new JobTitleDto(jt.Id, jt.Name))
                           .AsNoTracking()
                           .ToListAsync()
        );

        // GET /job-titles/{id}
        group.MapGet("/{id:int}", async (int id, ErpDbContext dbContext) =>
        {
            var jobTitle = await dbContext.JobTitles
                                          .AsNoTracking()
                                          .FirstOrDefaultAsync(jt => jt.Id == id);
            return jobTitle is null 
                ? Results.NotFound() 
                : Results.Ok(new JobTitleDto(jobTitle.Id, jobTitle.Name));
        })
        .WithName(GetJobTitleEndpointName);

        // POST /job-titles
        group.MapPost("/", async (CreateJobTitleDto dto, ErpDbContext dbContext) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return Results.BadRequest("Job title name cannot be empty.");
            }

            var jobTitle = new JobTitle
            {
                Name = dto.Name
            };

            dbContext.JobTitles.Add(jobTitle);
            await dbContext.SaveChangesAsync();

            var resultDto = new JobTitleDto(jobTitle.Id, jobTitle.Name);

            return Results.CreatedAtRoute(GetJobTitleEndpointName, new { id = jobTitle.Id }, resultDto);
        });

        // PUT /job-titles/{id}
        group.MapPut("/{id:int}", async (int id, UpdateJobTitleDto dto, ErpDbContext dbContext) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return Results.BadRequest("Job title name cannot be empty.");
            }

            var jobTitle = await dbContext.JobTitles.FindAsync(id);
            if (jobTitle is null)
            {
                return Results.NotFound();
            }

            jobTitle.Name = dto.Name;
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        // DELETE /job-titles/{id}
        group.MapDelete("/{id:int}", async (int id, ErpDbContext dbContext) =>
        {
            var jobTitle = await dbContext.JobTitles.FindAsync(id);
            if (jobTitle is null)
            {
                return Results.NotFound();
            }

            dbContext.JobTitles.Remove(jobTitle);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
