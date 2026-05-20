using System.ComponentModel.DataAnnotations;

namespace TippSpiel.Models.Admin
{
    public class AdminLoginViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
