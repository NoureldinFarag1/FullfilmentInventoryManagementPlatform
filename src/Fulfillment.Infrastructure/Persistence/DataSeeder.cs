using Fulfillment.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Fulfillment.Infrastructure.Persistence;

public static class DataSeeder
{
    private const string DefaultPassword = "Passw0rd!2026";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));

                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        $"Failed to create role '{role}': {Describe(result)}");
            }
        }

        await CreateUserAsync(userManager, "admin@fulfillment.local", "System Administrator", Roles.Administrator);
        await CreateUserAsync(userManager, "operator@fulfillment.local", "Warehouse Operator", Roles.WareHouseOperator);
        await CreateUserAsync(userManager, "manager@fulfillment.local", "Operations Manager", Roles.Manager);
        await CreateUserAsync(userManager, "sales@fulfillment.local", "Sales Agent", Roles.SalesAgent);
    }

    private static async Task CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string role)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Fullname = fullName
        };

        var created = await userManager.CreateAsync(user, DefaultPassword);

        if (!created.Succeeded)
            throw new InvalidOperationException(
                $"Failed to create user '{email}': {Describe(created)}");

        var assigned = await userManager.AddToRoleAsync(user, role);

        if (!assigned.Succeeded)
            throw new InvalidOperationException(
                $"Failed to assign role '{role}' to '{email}': {Describe(assigned)}");
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => e.Description));
}