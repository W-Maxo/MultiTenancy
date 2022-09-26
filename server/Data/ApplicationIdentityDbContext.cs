using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MultiTenancy.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MultiTenancy.Data
{
    public partial class ApplicationIdentityDbContext : IdentityDbContext<ApplicationUser, MultiTenancy.Models.ApplicationRole, string>
    {
        public ApplicationIdentityDbContext(DbContextOptions<ApplicationIdentityDbContext> options) : base(options)
        {
        }

        public ApplicationIdentityDbContext()
        {
        }

        partial void OnModelBuilding(ModelBuilder builder);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<MultiTenancy.Models.ApplicationUser>()
                .HasOne(i => i.ApplicationTenant)
                .WithMany(i => i.Users)
                .HasForeignKey(i => i.TenantId)
                .HasPrincipalKey(i => i.Id);

            builder.Entity<MultiTenancy.Models.ApplicationRole>()
                .HasOne(i => i.ApplicationTenant)
                .WithMany(i => i.Roles)
                .HasForeignKey(i => i.TenantId)
                .HasPrincipalKey(i => i.Id);

            this.OnModelBuilding(builder);
        }

        public DbSet<MultiTenancy.Models.ApplicationTenant> Tenants
        {
            get;
            set;
        }

        public async Task SeedTenantsAdmin()
        {
            var user = new ApplicationUser
            {
                UserName = "tenantsadmin",
                NormalizedUserName = "TENANTSADMIN",
                Email = "tenantsadmin",
                NormalizedEmail = "TENANTSADMIN",
                EmailConfirmed = true,
                LockoutEnabled = false,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            if (!this.Users.Any(u => u.UserName == user.UserName))
            {
                var password = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
                var hashed = password.HashPassword(user, "tenantsadmin");
                user.PasswordHash = hashed;
                var userStore = new UserStore<ApplicationUser>(this);
                await userStore.CreateAsync(user);
            }

            await this.SaveChangesAsync();
        }
    }
}
