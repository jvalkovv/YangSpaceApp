using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YangSpaceApp.Server.Data;
using YangSpaceApp.Server.Data.Models;

namespace YangSpaceApp.Server.Data.SeedData
{
    public class Seed
    {
        private readonly YangSpaceDbContext dbContext;
        private readonly UserManager<User> userManager;
        private readonly IServiceProvider serviceProvider;
        public Seed(YangSpaceDbContext _dbContext, UserManager<User> _userManager, IServiceProvider _serviceProvider)
        {
            dbContext = _dbContext;
            userManager = _userManager;
            serviceProvider = _serviceProvider;
        }

        // Method to seed categories
        public async Task SeedCategories()
        {
            // Check if the Categories table is empty
            if (!dbContext.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category {Name = "Financial Planning", Description = "Comprehensive financial planning for individuals and families." },
                    new Category {Name = "Business Strategy", Description = "Strategy development for business growth." },
                    new Category {Name = "IT Support", Description = "Ongoing support and maintenance for IT systems." },
                    new Category {Name = "Childcare", Description = "Qualified nannies for childcare services." },
                    new Category {Name = "House Cleaning", Description = "General cleaning services for homes and offices." },
                    new Category {Name = "Personal Services", Description = "Various personal services for individuals." },
                    new Category {Name = "Car Services", Description = "Automotive repair and maintenance services." },
                    new Category {Name = "Home Construction", Description = "Building homes from start to finish." },
                    new Category {Name = "Commercial Construction", Description = "Design and construction of commercial buildings." },
                    new Category { Name = "Tutoring", Description = "Online and in-person tutoring for various subjects." },
                    new Category { Name = "Physical Therapy", Description = "Rehabilitation services for physical injuries." },
                    new Category { Name = "Health Insurance", Description = "Comprehensive health insurance services." },
                    new Category { Name = "Legal Consultation", Description = "Legal advice and consulting services." },
                    new Category { Name = "Contract Drafting", Description = "Drafting legal contracts and documents." },
                    new Category { Name = "Personal Training", Description = "Personalized fitness training programs." },
                    new Category { Name = "Group Fitness Classes", Description = "Join group sessions for fitness and wellness." },
                    new Category { Name = "Plumbing Services", Description = "Professional plumbing repair and installation." },
                    new Category { Name = "Electrical Services", Description = "Electrical repairs and installations." },
                    new Category { Name = "Marketing Campaigns", Description = "Campaign development and execution for businesses." }
                };

                // Add categories to the database and save changes
                dbContext.Categories.AddRange(categories);
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task SeedUsers()
        {

            if (!userManager.Users.Any())
            {
                using var scope = serviceProvider.CreateScope();

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                if (!await roleManager.RoleExistsAsync("ServiceProvider"))
                {
                    await roleManager.CreateAsync(new IdentityRole("ServiceProvider"));
                }

                if (!await roleManager.RoleExistsAsync("Client"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Client"));
                }
            }
            // Check if the Users table is empty
            if (!dbContext.Users.Any())
            {

                var usersData =
                    new List<(string Username, string Email, string Password, string FirstName, string LastName, string Role)>
                    {
                            ("spuser","spuser@spuser", "spuser1A", "Service", "Provider", "ServiceProvider"),
                            ("cuser","cuser@cuser", "cuser1A", "Client", "Client", "Client"),
                    };

                foreach (var userData in usersData)
                {

                    var user = new User
                    {
                        UserName = userData.Username,
                        FirstName = userData.FirstName,
                        LastName = userData.LastName,
                        Email = userData.Email,
                        Role = userData.Role
                    };

                    var result = await userManager.CreateAsync(user, userData.Password);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, userData.Role);
                    }


                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }
}
