using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Identity;

namespace MultiTenancy.Models
{
    public partial class ApplicationUser : IdentityUser
    {
        [NotMapped]
        public IEnumerable<string> RoleNames { get; set; }

        [IgnoreDataMember]
        public override string PasswordHash { get; set; }

        [IgnoreDataMember, NotMapped]
        public string Password { get; set; }

        [IgnoreDataMember, NotMapped]
        public string ConfirmPassword { get; set; }

        [IgnoreDataMember, NotMapped]
        public string Name
        {
            get
            {
                return UserName;
            }
            set
            {
                UserName = value;
            }
        }
        public int? TenantId { get; set; }

        [ForeignKey("TenantId")]
        public ApplicationTenant ApplicationTenant { get; set; }
    }

    [Table("AspNetTenants")]
    public partial class ApplicationTenant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id  { get; set; }

        public ICollection<ApplicationUser> Users { get; set; }

        public ICollection<ApplicationRole> Roles { get; set; }

        public string Name { get; set; }

        public string Hosts { get; set; }
    }

    public partial class ApplicationRole : IdentityRole
    {
        public int? TenantId { get; set; }

        [ForeignKey("TenantId")]
        public ApplicationTenant ApplicationTenant { get; set; }
    }
}
