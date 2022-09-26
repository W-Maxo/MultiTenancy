using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MultiTenancy.Models;
using MultiTenancy.Data;

namespace MultiTenancy.Authentication
{
    public partial class ApplicationPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, MultiTenancy.Models.ApplicationRole>
    {
        private ApplicationIdentityDbContext identityContext;

        public ApplicationPrincipalFactory(ApplicationIdentityDbContext identityContext, UserManager<ApplicationUser> userManager, RoleManager<MultiTenancy.Models.ApplicationRole> roleManager, IOptions<IdentityOptions> optionsAccessor) : base(userManager, roleManager, optionsAccessor)
        {
            this.identityContext = identityContext;
        }
        partial void OnCreatePrincipal(ClaimsPrincipal principal, ApplicationUser user);

        public async override Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
        {
            var principal = await base.CreateAsync(user);

            this.OnCreatePrincipal(principal, user);

            return principal;
        }
    }
}
