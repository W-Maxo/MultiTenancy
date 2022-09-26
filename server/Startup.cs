using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MultiTenancy.Data;
using MultiTenancy.Models;
using MultiTenancy.Authentication;
using Radzen;
namespace MultiTenancy
{
    public partial class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        partial void OnConfigureServices(IServiceCollection services);

        partial void OnConfiguringServices(IServiceCollection services);

        public void ConfigureServices(IServiceCollection services)
        {
            OnConfiguringServices(services);

            services.AddHttpContextAccessor();
            services.AddScoped<HttpClient>(serviceProvider =>
            {

              var uriHelper = serviceProvider.GetRequiredService<NavigationManager>();

              return new HttpClient
              {
                BaseAddress = new Uri(uriHelper.BaseUri)
              };
            });

            services.AddHttpClient();
            services.AddAuthentication();
            services.AddAuthorization();
            services.AddDbContext<ApplicationIdentityDbContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("MTConnection"));
            }, ServiceLifetime.Transient);

            services.AddIdentity<ApplicationUser, MultiTenancy.Models.ApplicationRole>()
                  .AddEntityFrameworkStores<ApplicationIdentityDbContext>();

            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>,
                  ApplicationPrincipalFactory>();
            services.AddScoped<SecurityService>();
            services.AddScoped<MtService>();

            services.AddDbContext<MultiTenancy.Data.MtContext>(options =>
            {
              options.UseNpgsql(Configuration.GetConnectionString("MTConnection"));
            });

            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddServerSideBlazor().AddHubOptions(o =>
            {
                o.MaximumReceiveMessageSize = 10 * 1024 * 1024;
            });

            services.AddScoped<DialogService>();
            services.AddScoped<NotificationService>();
            services.AddScoped<TooltipService>();
            services.AddScoped<ContextMenuService>();
            services.AddScoped<GlobalsService>();

            services.AddTransient<IUserStore<ApplicationUser>, MultiTenancyUserStore>();
            OnConfigureServices(services);
        }

        partial void OnConfigure(IApplicationBuilder app, IWebHostEnvironment env);
        partial void OnConfiguring(IApplicationBuilder app, IWebHostEnvironment env);

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ApplicationIdentityDbContext identityDbContext)
        {
            OnConfiguring(app, env);
            if (env.IsDevelopment())
            {
                Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.Use((ctx, next) =>
                {
                    return next();
                });
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                  name: "default",
                  pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            identityDbContext.Database.Migrate();

            identityDbContext.SeedTenantsAdmin().Wait();

            OnConfigure(app, env);
        }
    }



    public class MultiTenancyUserStore : UserStore<ApplicationUser, ApplicationRole, ApplicationIdentityDbContext>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly GlobalsService globals;
        public MultiTenancyUserStore(GlobalsService globals, IHttpContextAccessor httpContextAccessor, ApplicationIdentityDbContext context, IdentityErrorDescriber describer = null) : base(context, describer)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.globals = globals;
        }

        private ApplicationTenant GetTenant()
        {
            var tenants = Context.Set<ApplicationTenant>().ToList();

            var host = httpContextAccessor.HttpContext.Request.Host.Value;

            return tenants.Where(t => t.Hosts.Split(',').Where(h => h.Contains(host)).Any()).FirstOrDefault();
        }

        protected override async Task<ApplicationRole> FindRoleAsync(string normalizedRoleName, System.Threading.CancellationToken cancellationToken)
        {
            var tenant = globals.Tenant ?? GetTenant();
            ApplicationRole role = null;

            if (tenant != null)
            {
                role = await Context.Set<ApplicationRole>().SingleOrDefaultAsync(r => r.NormalizedName == normalizedRoleName && r.TenantId == tenant.Id, cancellationToken);
            }

            return role;
        }
    }
}
