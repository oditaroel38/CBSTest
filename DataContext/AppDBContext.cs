using CBS.Data.Entities;
using CBS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ACDI.DataContext
{
    public class AppDBContext : DbContext
    {

        public AppDBContext(DbContextOptions<AppDBContext> options)
            : base(options)
        {
            ChangeTracker.LazyLoadingEnabled = false; // Disable lazy loading
        }
        public DbSet<MEMBERS_PENSIONER_INFO> MEMBERS_PENSIONER_INFO { get; set; }
        public DbSet<CONTROLN_MEMBERSHIP_INFO> CONTROLN_MEMBERSHIP_INFO { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {      
            modelBuilder.Entity<CONTROLN_MEMBERSHIP_INFO>().HasKey(c => new { c.Id });
            modelBuilder.Entity<MEMBERS_PENSIONER_INFO>().HasKey(c => new { c.CONTROLN });
            modelBuilder.Entity<RemittanceUploadReportsModel>().HasNoKey();
        }

        public static AppDBContext CreateNewInstance(DbContextOptions<AppDBContext> options)
        {
            return new AppDBContext(options);
        }
    }
}
