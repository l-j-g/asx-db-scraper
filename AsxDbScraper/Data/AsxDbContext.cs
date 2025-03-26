using Microsoft.EntityFrameworkCore;
using AsxDbScraper.Models;

namespace AsxDbScraper.Data;

public class AsxDbContext : DbContext
{
    public AsxDbContext(DbContextOptions<AsxDbContext> options) : base(options)
    {
    }

    public DbSet<AsxCompany> AsxCompanies { get; set; } = null!;
    public DbSet<BalanceSheet> BalanceSheets { get; set; } = null!;
    public DbSet<IncomeStatement> IncomeStatements { get; set; } = null!;
    public DbSet<CashFlowStatement> CashFlowStatements { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Cosmos DB container names
        modelBuilder.Entity<AsxCompany>().ToContainer("Companies");
        modelBuilder.Entity<BalanceSheet>().ToContainer("BalanceSheets");
        modelBuilder.Entity<IncomeStatement>().ToContainer("IncomeStatements");
        modelBuilder.Entity<CashFlowStatement>().ToContainer("CashFlowStatements");

        // Configure partition keys
        modelBuilder.Entity<AsxCompany>()
            .HasPartitionKey(c => c.Code);

        modelBuilder.Entity<BalanceSheet>()
            .HasPartitionKey(b => b.CompanyCode);

        modelBuilder.Entity<IncomeStatement>()
            .HasPartitionKey(i => i.CompanyCode);

        modelBuilder.Entity<CashFlowStatement>()
            .HasPartitionKey(c => c.CompanyCode);

        // Configure unique constraints
        modelBuilder.Entity<AsxCompany>()
            .HasUniqueIndex(c => c.Code);

        modelBuilder.Entity<BalanceSheet>()
            .HasUniqueIndex(b => new { b.CompanyCode, b.StatementDate });

        modelBuilder.Entity<IncomeStatement>()
            .HasUniqueIndex(i => new { i.CompanyCode, i.StatementDate });

        modelBuilder.Entity<CashFlowStatement>()
            .HasUniqueIndex(c => new { c.CompanyCode, c.StatementDate });
    }
}