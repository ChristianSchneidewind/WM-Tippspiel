using System.ComponentModel.DataAnnotations;

namespace TippSpiel.Models.Admin
{
    public class AdminGroupViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
