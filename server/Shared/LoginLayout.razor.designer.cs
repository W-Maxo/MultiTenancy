﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;
using Microsoft.AspNetCore.Identity;
using MultiTenancy.Models;
using MultiTenancy.Pages;

namespace MultiTenancy.Layouts
{
    public partial class LoginLayoutComponent : LayoutComponentBase
    {
        [Inject]
        protected GlobalsService Globals { get; set; }

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
        protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        [Inject]
        protected SecurityService Security { get; set; }

        [Inject]
        protected MtService Mt { get; set; }

        protected RadzenBody body0;

        private void Authenticated()
        {
             StateHasChanged();
        }

        protected override async System.Threading.Tasks.Task OnInitializedAsync()
        {
             if (Security != null)
             {
                  Security.Authenticated += Authenticated;

                  await Security.InitializeAsync(AuthenticationStateProvider);
             }
        }
    }
}
