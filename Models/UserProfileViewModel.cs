using System.ComponentModel.DataAnnotations;

namespace e_commerce.Models
{
    public class UserProfileViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Username")]
        public string Username { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
        public List<ProfileAddress> Addresses { get; set; } = new List<ProfileAddress>();
    }
}
