using Microsoft.AspNetCore.Identity;

namespace webdulich.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string HoTen { get; set; }
    }
}
