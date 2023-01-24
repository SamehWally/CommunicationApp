using CommunicationAppApi.Models;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace CommunicationAppApi.Data
{
    public static class TrialData
    {
        public static void TrialUsers(UserManager<User> _userManager, RoleManager<Role> _roleManager)
        {
            if (!_userManager.Users.Any())
            {
                var userData = System.IO.File.ReadAllText("Data/UserTrialData.json");
                var users = JsonConvert.DeserializeObject<List<User>>(userData);
                var roles = new List<Role>{
                    new Role{Name="Admin"},
                    new Role{Name="Moderator"},
                    new Role{Name="Member"},
                    new Role{Name="VIP"}
                };
                foreach (var role in roles)
                {
                    _roleManager.CreateAsync(role).Wait();
                }
                foreach (var user in users)
                {
                    user.Photos.ToList().ForEach(p => p.IsApproved = true);
                    _userManager.CreateAsync(user, "password").Wait();
                    _userManager.AddToRoleAsync(user, "Member").Wait();
                }
                var admin = _userManager.FindByNameAsync("Admin").Result;
                _userManager.AddToRolesAsync(admin, new[] { "Admin", "Moderator" }).Wait();
            }
        }
    }
}
