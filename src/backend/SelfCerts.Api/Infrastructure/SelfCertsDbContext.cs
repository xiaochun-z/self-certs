using Microsoft.EntityFrameworkCore;
using SelfCerts.Api.Models;

namespace SelfCerts.Api.Infrastructure;

public class SelfCertsDbContext : DbContext
{
    public SelfCertsDbContext(DbContextOptions<SelfCertsDbContext> options) : base(options)
    {
    }

    public DbSet<CertRecord> CertRecords { get; set; }
    public DbSet<CaConfig> CaConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<CertRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ServerReqCnf).IsRequired();
            entity.Property(e => e.ServerKey).IsRequired();
            entity.Property(e => e.ServerCrt).IsRequired();
            entity.HasOne<CaConfig>().WithMany().HasForeignKey(e => e.CaConfigId);
        });
modelBuilder.Entity<CaConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}