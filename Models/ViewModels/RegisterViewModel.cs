using System.ComponentModel.DataAnnotations;

namespace TippSpiel.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Benutzername")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "E-Mail")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Passwort")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        [Display(Name = "Passwort bestätigen")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Als Admin registrieren")]
        public bool IsAdmin { get; set; }

        [Display(Name = "Admin-Code")]
        public string? AdminCode { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
