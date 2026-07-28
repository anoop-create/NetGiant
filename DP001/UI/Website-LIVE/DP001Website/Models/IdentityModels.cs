using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections.Generic;
using System.Linq;
using DP001DataAccess.Entities;
using DP001BusinessLogic;

namespace DP001Website.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public int TenantID { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DP001Membership", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }

    public class UserViewModel
    {
        public UserViewModel()
        {
            GetAllUsers();
        }

        private ApplicationDbContext Context
        {
            get
            {
                return new ApplicationDbContext();
            }
        }

        public List<AllTheUsers> AllUsers { get; set; }

        private void GetAllUsers()
        {
            AllUsers = new List<AllTheUsers>();
            List<ApplicationUser> allUsers = Context.Users.ToList();
            CrudTenant crud = new CrudTenant();
            foreach (ApplicationUser user in allUsers)
            {

                AllUsers.Add(new AllTheUsers()
                {
                    AppUser = user,
                    Tenant = crud.Read(user.TenantID)
                });
            }
            AllUsers = AllUsers.OrderBy(x => x.AppUser.Email).ToList();
        }

        public class AllTheUsers
        {
            public ApplicationUser AppUser { get; set; }
            public TenantSetting Tenant { get; set; }
        }
    }

    public class UserRoleViewModel
    {
        public UserRoleViewModel(string email)
        {
            AllUserRoles = new List<IdentityRole>();
            AllRoles = new List<IdentityRole>();
            Email = email;
            GetAllRoles();
            GetAllUserRoles();
        }

        public UserRoleViewModel()
        {
            AllUserRoles = new List<IdentityRole>();
            AllRoles = new List<IdentityRole>();
            Email = "";
            GetAllRoles();
        }

        public List<IdentityRole> AllUserRoles { get; set; }
        public List<IdentityRole> AllRoles { get; set; }
        public string[] PostedRoles { get; set; }
        public string Email { get; set; }

        public ApplicationDbContext Context
        {
            get
            {
                return new ApplicationDbContext();
            }
        }

        public void GetAllUserRoles()
        {
            List<IdentityUserRole> roles = Context.Users.Where(a => a.Email == Email).FirstOrDefault().Roles.ToList();
            foreach (IdentityUserRole role in roles)
            {
                IdentityRole userRole = Context.Roles.Where(x => x.Id == role.RoleId).First();
                AllUserRoles.Add(userRole);
            }
            AllUserRoles.Sort((x, y) => x.Name.CompareTo(y.Name));
        }

        public void GetAllRoles()
        {
            AllRoles = Context.Roles.OrderBy(x => x.Name).ToList();
        }
    }

    //public class RegisterViewModel
    //{
    //    [Required]
    //    [EmailAddress]
    //    [Display(Name = "Email")]
    //    public string Email { get; set; }

    //    [Required]
    //    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    //    [DataType(DataType.Password)]
    //    [Display(Name = "Password")]
    //    public string Password { get; set; }

    //    [DataType(DataType.Password)]
    //    [Display(Name = "Confirm password")]
    //    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    //    public string ConfirmPassword { get; set; }

    //    public bool? IsNew { get; set; }
    //}

}