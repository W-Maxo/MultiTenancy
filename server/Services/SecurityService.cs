using System;
using System.Web;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components.Authorization;
using MultiTenancy.Models;
using MultiTenancy.Data;

namespace MultiTenancy
{
    public class MultiTenancyRoleValidator : RoleValidator<ApplicationRole>
    {
        public override Task<IdentityResult> ValidateAsync(RoleManager<ApplicationRole> manager, ApplicationRole role)
        {
            return Task.FromResult(IdentityResult.Success);
        }
    }

    public partial class SecurityService
    {
        public event Action Authenticated;

        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<MultiTenancy.Models.ApplicationRole> roleManager;
        private readonly IWebHostEnvironment env;
        private readonly NavigationManager uriHelper;
        private readonly GlobalsService globals;

        public SecurityService(ApplicationIdentityDbContext context,
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager,
            RoleManager<MultiTenancy.Models.ApplicationRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            NavigationManager uriHelper,
            GlobalsService globals)
        {
            this.context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.signInManager = signInManager;
            this.env = env;
            this.uriHelper = uriHelper;
            this.globals = globals;

            this.roleManager.RoleValidators.Clear();
            this.roleManager.RoleValidators.Add(new MultiTenancyRoleValidator());

            if (User != null)
            {
                var tenant = context.Tenants.Where(t => t.Id == User.TenantId).FirstOrDefault();
                if (tenant != null && tenant.Hosts != null && tenant.Hosts.Split(',').Where(h => h.Contains(new Uri(uriHelper.BaseUri).Host)).Any())
                {
                    globals.Tenant = tenant;
                    User.ApplicationTenant = tenant;
                }
                else if ((env.EnvironmentName == "Development" && User.Name == "admin" || User.Name == "tenantsadmin") &&
                    globals.Tenant == null && context.Tenants.Any())
                {
                    globals.Tenant = context.Tenants.FirstOrDefault();
                    User.ApplicationTenant = tenant;
                }
            }
        }

        public ApplicationIdentityDbContext context { get; set; }

        ApplicationUser user;
        public ApplicationUser User
        {
            get
            {
                if(user == null)
                {
                    return new ApplicationUser() { Name = "Anonymous" };
                }

                return user;
            }
        }

        static System.Threading.SemaphoreSlim semaphoreSlim = new System.Threading.SemaphoreSlim(1, 1);
        public async Task<bool> InitializeAsync(AuthenticationStateProvider authenticationStateProvider)
        {
            var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
            Principal = authenticationState.User;

            var name = Principal.Identity.Name;

            if (env.EnvironmentName == "Development" && name == "admin")
            {
                user = new ApplicationUser { UserName = name };
            }

            if (user == null && name != null)
            {
                await semaphoreSlim.WaitAsync();
                try
                {
                    user = await userManager.FindByEmailAsync(name);

                    if(user == null)
                    {
                        user = await userManager.FindByNameAsync(name);
                    }

                    if(user != null)
                    {
                        user.RoleNames = await userManager.GetRolesAsync(user);
                    }
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }

            var result = IsAuthenticated();
            if(result)
            {
                if ((env.EnvironmentName == "Development" && User.Name == "admin" || User.Name == "tenantsadmin") &&
                globals.Tenant == null && context.Tenants.Any())
                {
                    globals.Tenant = context.Tenants.FirstOrDefault();
                }

                if (user.TenantId != null)
                {
                    user.ApplicationTenant = await GetTenantById(user.TenantId.Value);  
                }
                Authenticated?.Invoke();
            }

            return result;
        }

        public ClaimsPrincipal Principal { get; set; }

        public bool IsInRole(params string[] roles)
        {
            if (roles.Contains("Everybody"))
            {
                return true;
            }

            if (!IsAuthenticated())
            {
                return false;
            }

            if (roles.Contains("Authenticated"))
            {
                return true;
            }

            return roles.Any(role => Principal.IsInRole(role));
        }

        public bool IsAuthenticated()
        {
            return Principal != null ? Principal.Identity.IsAuthenticated : false;
        }

        public async Task Logout()
        {
            uriHelper.NavigateTo("Account/Logout", true);
        }

        public async Task<bool> Login(string userName, string password)
        {
            uriHelper.NavigateTo("Login", true);

            return true;
        }

        public async Task<IEnumerable<MultiTenancy.Models.ApplicationRole>> GetRoles()
        {
            var tenant = globals.Tenant;
            if(tenant != null)
            {
                return await Task.FromResult(roleManager.Roles.Where(i => i.TenantId == tenant.Id));
            }

            return await Task.FromResult(Enumerable.Empty<MultiTenancy.Models.ApplicationRole>());
        }

        public async Task<MultiTenancy.Models.ApplicationRole> CreateRole(MultiTenancy.Models.ApplicationRole role)
        {
            var result = await roleManager.CreateAsync(role);

            EnsureSucceeded(result);

            var item = context.Roles
                    .Where(i => i.Id == role.Id)
                    .FirstOrDefault();

            if(item != null)
            {
                var tenant = globals.Tenant;
                if(tenant != null)
                {
                    item.TenantId = tenant.Id;
                    role.TenantId = tenant.Id;
                    context.SaveChanges();
                }
            }

            return role;
        }

        public async Task<MultiTenancy.Models.ApplicationRole> DeleteRole(string id)
        {
            var item = context.Roles
                .Where(i => i.Id == id)
                .FirstOrDefault();

            context.Roles.Remove(item);
            context.SaveChanges();

            return item;
        }

        public async Task<MultiTenancy.Models.ApplicationRole> GetRoleById(string id)
        {
            var tenant = globals.Tenant;
            if(globals.Tenant != null)
            {
                return await Task.FromResult(context.Roles.Where(i => i.TenantId == tenant.Id && i.Id == id).FirstOrDefault());
            }

            return null;
        }

        public async Task<IEnumerable<MultiTenancy.Models.ApplicationTenant>> GetTenants()
        {
            return await Task.FromResult(context.Tenants);
        }

        public async Task<MultiTenancy.Models.ApplicationTenant> CreateTenant(MultiTenancy.Models.ApplicationTenant tenant)
        {
            context.Tenants.Add(tenant);
            context.SaveChanges();

            return tenant;
        }

        public async Task<MultiTenancy.Models.ApplicationTenant> DeleteTenant(int id)
        {
            var item = context.Tenants
                .Where(i => i.Id == id)
                .FirstOrDefault();

            context.Tenants.Remove(item);
            context.SaveChanges();

            return item;
        }

        public async Task<MultiTenancy.Models.ApplicationTenant> GetTenantById(int id)
        {
            return await Task.FromResult(context.Tenants.Find(id));
        }

        public async Task<MultiTenancy.Models.ApplicationTenant> UpdateTenant(int? id, MultiTenancy.Models.ApplicationTenant tenant)
        {
            var item = context.Tenants
                                .Where(i => i.Id == id)
                                .FirstOrDefault();

            if (item == null)
            {
                throw new Exception("Item no longer available");
            }

            var entry = context.Entry(item);
            entry.CurrentValues.SetValues(tenant);
            entry.State = EntityState.Modified;
            context.SaveChanges();

            return tenant;
        }
        public async Task<IEnumerable<ApplicationUser>> GetUsers()
        {
            var tenant = globals.Tenant;
            if(tenant != null)
            {
                return await Task.FromResult(context.Users.AsNoTracking().AsQueryable().Where(i => i.TenantId == tenant.Id).Include(i => i.ApplicationTenant));
            }
            return await Task.FromResult(Enumerable.Empty<MultiTenancy.Models.ApplicationUser>());
        }

        public async Task<ApplicationUser> CreateUser(ApplicationUser user)
        {
            user.UserName = user.Email;

            var result = await userManager.CreateAsync(user, user.Password);

            EnsureSucceeded(result);

            var roles = user.RoleNames;

            if (roles != null && roles.Any())
            {
                result = await userManager.AddToRolesAsync(user, roles);
                EnsureSucceeded(result);
            }

            user.RoleNames = roles;

            var item = context.Users
                    .Where(i => i.Id == user.Id)
                    .FirstOrDefault();

            if(item != null)
            {
                var tenant = globals.Tenant;
                if(tenant != null)
                {
                    item.TenantId = tenant.Id;
                    user.TenantId = tenant.Id;
                    context.SaveChanges();
                }
            }

            return user;
        }

        public void Reset() => context.ChangeTracker.Entries().Where(e => e.Entity != null).ToList().ForEach(e => e.State = EntityState.Detached);

        public async Task<ApplicationUser> DeleteUser(string id)
        {
            Reset();

            var item = context.Users
              .Where(i => i.Id == id)
              .FirstOrDefault();

            context.Users.Remove(item);
            context.SaveChanges();

            return item;
        }

        public async Task<ApplicationUser> GetUserById(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            
            if (user != null)
            {
                context.Entry(user).Reload();
                user.RoleNames = await userManager.GetRolesAsync(user);
                if (user.TenantId.HasValue)
                {
                    user.ApplicationTenant = await GetTenantById(user.TenantId.Value);
                }
            }

            return await Task.FromResult(user);
        }

        public async Task<ApplicationUser> UpdateUser(string id, ApplicationUser user)
        {
            var roles = user.RoleNames != null ? user.RoleNames.ToArray() : Array.Empty<string>();

            var result = await userManager.RemoveFromRolesAsync(user, await userManager.GetRolesAsync(user));

            EnsureSucceeded(result);

            if (roles.Any())
            {
                result = await userManager.AddToRolesAsync(user, roles);

                EnsureSucceeded(result);
            }

            result = await userManager.UpdateAsync(user);

            EnsureSucceeded(result);

            if (!String.IsNullOrEmpty(user.Password) && user.Password == user.ConfirmPassword)
            {
                result = await userManager.RemovePasswordAsync(user);

                EnsureSucceeded(result);

                result = await userManager.AddPasswordAsync(user, user.Password);

                EnsureSucceeded(result);
            }

            return user;
        }

        private void EnsureSucceeded(IdentityResult result)
        {
            if (!result.Succeeded)
            {
                var message = string.Join(", ", result.Errors.Select(error => error.Description));

                throw new ApplicationException(message);
            }
        }
    }
}
