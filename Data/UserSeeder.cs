using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TippSpiel.Models;

namespace TippSpiel.Data
{
    public static class UserSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var options = services.GetRequiredService<IOptions<SeedUsersOptions>>().Value;
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await EnsureRoleAsync(roleManager, "Admin");
            await EnsureRoleAsync(roleManager, "User");

            if (!string.IsNullOrWhiteSpace(options.AdminUserName) &&
                !string.IsNullOrWhiteSpace(options.AdminEmail) &&
                !string.IsNullOrWhiteSpace(options.AdminPassword))
            {
                var admin = await userManager.FindByNameAsync(options.AdminUserName);
                if (admin == null)
                {
                    admin = new User
                    {
                        UserName = options.AdminUserName,
                        Email = options.AdminEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(admin, options.AdminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, "Admin");
                    }
                }
                else if (!await userManager.IsInRoleAsync(admin, "Admin"))
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            if (!string.IsNullOrWhiteSpace(options.UserUserName) &&
                !string.IsNullOrWhiteSpace(options.UserEmail) &&
                !string.IsNullOrWhiteSpace(options.UserPassword))
            {
                var user = await userManager.FindByNameAsync(options.UserUserName);
                if (user == null)
                {
                    user = new User
                    {
                        UserName = options.UserUserName,
                        Email = options.UserEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, options.UserPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "User");
                    }
                }
                else if (!await userManager.IsInRoleAsync(user, "User"))
                {
                    await userManager.AddToRoleAsync(user, "User");
                }
            }
        }

        private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
}
