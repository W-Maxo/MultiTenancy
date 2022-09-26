using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MultiTenancy.Models;

namespace MultiTenancy.Pages
{
    public partial class ApplicationTenantsComponent : ComponentBase, IDisposable
    {
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, dynamic> Attributes { get; set; }

        [Inject]
        protected GlobalsService Globals { get; set; }

        partial void OnDispose();

        public void Dispose()
        {
            Globals.PropertyChanged -= OnPropertyChanged;
            OnDispose();
        }

        public void Reload()
        {
            InvokeAsync(StateHasChanged);
        }

        public void OnPropertyChanged(PropertyChangedEventArgs args)
        {
        }

        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected NavigationManager UriHelper { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        protected SecurityService Security { get; set; }

        [Inject]
        protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        [Inject]
        protected MtService Mt { get; set; }
        protected RadzenDataGrid<ApplicationTenant> grid0;

        IEnumerable<ApplicationTenant> _tenants;
        protected IEnumerable<ApplicationTenant> tenants
        {
            get
            {
                return _tenants;
            }
            set
            {
                if (!object.Equals(_tenants, value))
                {
                    var args = new PropertyChangedEventArgs(){ Name = "tenants", NewValue = value, OldValue = _tenants };
                    _tenants = value;
                    OnPropertyChanged(args);
                    Reload();
                }
            }
        }

        protected override async System.Threading.Tasks.Task OnInitializedAsync()
        {
            Globals.PropertyChanged += OnPropertyChanged;
            await Security.InitializeAsync(AuthenticationStateProvider);
            if (!Security.IsAuthenticated())
            {
                UriHelper.NavigateTo("Login", true);
            }
            else
            {
                await Load();
            }
        }
        protected async System.Threading.Tasks.Task Load()
        {
            var securityGetTenantsResult = await Security.GetTenants();
            tenants = securityGetTenantsResult;

            if (Globals.Tenant == null && tenants != null && tenants.Any()) {
                Globals.Tenant = tenants.FirstOrDefault();
            }
        }

        protected async System.Threading.Tasks.Task Button0Click(MouseEventArgs args)
        {
            var dialogResult = await DialogService.OpenAsync<AddApplicationTenant>("Add Application Tenant", null);
            await Load();

            await grid0.Reload();
        }

        protected async void Grid0RowRender(RowRenderEventArgs<ApplicationTenant> args)
        {
            args.Attributes.Add("style", $"font-weight: {(args.Data.Id == Globals.Tenant?.Id ? "bold" : "normal")};");
        }

        protected async System.Threading.Tasks.Task Grid0RowSelect(ApplicationTenant args)
        {
            var dialogResult = await DialogService.OpenAsync<EditApplicationTenant>("Edit Application Tenant", new Dictionary<string, object>() { {"Id", args.Id} });
            await Load();

            await grid0.Reload();
        }

        protected async System.Threading.Tasks.Task Button1Click(MouseEventArgs args, dynamic data)
        {
            Globals.Tenant = data;
        }

        protected async System.Threading.Tasks.Task GridDeleteButtonClick(MouseEventArgs args, ApplicationTenant data)
        {
            try
            {
                var securityDeleteTenantResult = await Security.DeleteTenant(data.Id);
                await Load();

                if (securityDeleteTenantResult != null)
                {
                    await grid0.Reload();
                }
            }
            catch (System.Exception securityDeleteTenantException)
            {
                NotificationService.Notify(new NotificationMessage(){ Severity = NotificationSeverity.Error,Summary = $"Error",Detail = $"Unable to delete tenant" });
            }
        }
    }
}
