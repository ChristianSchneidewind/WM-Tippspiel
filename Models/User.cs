using Microsoft.AspNetCore.Identity;

namespace TippSpiel.Models
{
    public class User : IdentityUser
    {
        public int Points {get; set;}
    }
}