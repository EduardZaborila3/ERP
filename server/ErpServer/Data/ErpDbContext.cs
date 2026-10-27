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
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<CarDocument> CarDocuments => Set<CarDocument>();
    public DbSet<ConstructionSite> Sites => Set<ConstructionSite>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);

        modelBuilder.Entity<CarDocument>()
            .HasIndex(cd => new { cd.CarId, cd.DocumentType })
            .IsUnique();

        modelBuilder.Entity<Car>()
            .HasOne(c => c.ResponsibleUser)
            .WithMany()
            .HasForeignKey(c => c.ResponsibleUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Stock>()
            .HasOne(s => s.Site)
            .WithMany(cs => cs.Stocks)
            .HasForeignKey(s => s.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne(sm => sm.Stock)
            .WithMany(s => s.Movements)
            .HasForeignKey(sm => sm.StockId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockMovement>()
            .HasOne(sm => sm.FromSite)
            .WithMany()
            .HasForeignKey(sm => sm.FromSiteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne(sm => sm.ToSite)
            .WithMany()
            .HasForeignKey(sm => sm.ToSiteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne(sm => sm.Driver)
            .WithMany()
            .HasForeignKey(sm => sm.DriverId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
