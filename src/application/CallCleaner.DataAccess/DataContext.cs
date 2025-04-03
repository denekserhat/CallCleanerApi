using CallCleaner.Entities.Concrete;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CallCleaner.DataAccess
{
    public class DataContext : IdentityDbContext<AppUser, AppRole, int>
    {
        private readonly Microsoft.AspNetCore.Hosting.IHostingEnvironment _env;
        public DataContext(DbContextOptions<DataContext> options, IHostingEnvironment env) : base(options)
        {
            _env = env;
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {

            base.OnModelCreating(builder);
            builder.HasDefaultSchema("application");
            #region Identity Tables Change Schema
            builder.Entity<AppUser>(entity => entity.ToTable(name: "Users", schema: "authentication"));
            builder.Entity<AppRole>(entity => entity.ToTable(name: "Roles", schema: "authentication"));
            builder.Entity<IdentityUserRole<int>>(entity => entity.ToTable(name: "UserRoles", schema: "authentication"));
            builder.Entity<IdentityUserLogin<int>>(entity => entity.ToTable(name: "UserLogins", schema: "authentication"));
            builder.Entity<IdentityUserToken<int>>(entity => entity.ToTable(name: "UserTokens", schema: "authentication"));
            builder.Entity<IdentityUserClaim<int>>(entity => entity.ToTable(name: "UserClaims", schema: "authentication"));
            builder.Entity<IdentityRoleClaim<int>>(entity => entity.ToTable(name: "RoleClaims", schema: "authentication"));
            #endregion

        }

    }
}
