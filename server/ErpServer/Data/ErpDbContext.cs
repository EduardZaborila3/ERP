using ErpServer.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpServer.Data;

public class ErpDbContext(DbContextOptions<ErpDbContext> options) 
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<AppTask> Tasks => Set<AppTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
    }

}
