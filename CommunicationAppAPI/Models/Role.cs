using Microsoft.AspNetCore.Identity;

namespace CommunicationAppApi.Models
{
    public class Role : IdentityRole<int>
    {
        public ICollection<UserRole> UserRoles { get; set; }
    }
}
