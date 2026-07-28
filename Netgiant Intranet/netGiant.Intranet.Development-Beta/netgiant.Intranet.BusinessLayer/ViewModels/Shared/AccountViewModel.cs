using netGiant.Intranet.BusinessLayer.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections.Generic;
using System.Linq;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using netGiant.Intranet.DataLayer.NetgiantMembership;

namespace netGiant.Intranet.ViewModels
{
    public class ExternalLoginConfirmationViewModel : CommonViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class ExternalLoginListViewModel : CommonViewModel
    {
        public string Action { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class ManageUserViewModel : CommonViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ApplicationUserViewModel : CommonViewModel
    {
        public ApplicationUserViewModel()
        {
            _ctx = new membershipEntities();
        }
        private membershipEntities _ctx;


        public List<ApplicationUser> ApplicationUsers { get; set; }
        public List<IdentityRole> UserRoles { get; set; }
        public IdentityRole Role { get; set; }




        public class TelerikUser
        {
            public string UserName { get; set; }
            public string Email { get; set; }
            public string ID { get; set; }
        }

        public IQueryable<TelerikUser> UserList { get; set; }

        public ApplicationUserViewModel GetUsers()
        {
            UserList = _ctx.AspNetUsers
                .Select(x => new TelerikUser
                {
                    UserName = x.UserName,
                    Email = x.Email,
                    ID = x.Id
                })
                .AsQueryable();
            return this;
        }





        public class TelerikRole
        {
            public string Name { get; set; }
            public string ID { get; set; }
        }

        public IQueryable<TelerikRole> RoleList { get; set; }

        public ApplicationUserViewModel GetRoles()
        {
            RoleList = _ctx.AspNetRoles
                .Select(x => new TelerikRole
                {
                    Name = x.Name,
                    ID = x.Id
                })
                .AsQueryable();
            return this;
        }
    }

    public class LoginViewModel : CommonViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel : CommonViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public bool? IsNew { get; set; }
    }

    public class ResetPasswordViewModel : CommonViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class CreateRoleViewModel : CommonViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class UserRoleViewModel : CommonViewModel
    {
        public UserRoleViewModel(string email)
        {
            AllUserRoles =  new List<IdentityRole>();
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
    
    public class CreateUserRoleViewModel : CommonViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel : CommonViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}
