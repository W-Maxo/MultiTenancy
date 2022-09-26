using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MultiTenancy.Models;
using Radzen;

namespace MultiTenancy
{
    public partial class GlobalsService
    {
        public event Action<PropertyChangedEventArgs> PropertyChanged;


        ApplicationTenant _Tenant;
        public ApplicationTenant Tenant
        {
            get
            {
                return _Tenant;
            }
            set
            {
                if(!object.Equals(_Tenant, value))
                {
                    var args = new PropertyChangedEventArgs(){ Name = "Tenant", NewValue = value, OldValue = _Tenant, IsGlobal = true };
                    _Tenant = value;
                    PropertyChanged?.Invoke(args);
                }
            }
        }
    }

    public class PropertyChangedEventArgs
    {
        public string Name { get; set; }
        public object NewValue { get; set; }
        public object OldValue { get; set; }
        public bool IsGlobal { get; set; }
    }
}
