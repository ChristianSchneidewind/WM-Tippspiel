using System.ComponentModel.DataAnnotations;

namespace TippSpiel.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Benutzername oder E-Mail")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Passwort")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Angemeldet bleiben")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
