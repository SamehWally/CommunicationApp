using Microsoft.AspNetCore.Identity;

namespace CommunicationAppApi.Models
{
    public class UserRole : IdentityUserRole<int>
    {
        public User User { get; set; }
        public Role Role { get; set; }
    }
}
