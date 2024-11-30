using System.ComponentModel.DataAnnotations;

namespace e_commerce.Models
{

    public class UserLogin
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }     
        public bool RememberMe { get; set; }
    }
}