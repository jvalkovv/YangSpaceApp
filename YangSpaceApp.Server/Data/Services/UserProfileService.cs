using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using YangSpaceApp.Server.Data;
using YangSpaceApp.Server.Data.Extension;
using YangSpaceApp.Server.Data.Models;
using YangSpaceApp.Server.Data.Services.Contracts;
using YangSpaceApp.Server.Data.ViewModel.AccountViewModel;

namespace YangSpaceApp.Server.Data.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly YangSpaceDbContext _context;
        private readonly IConfiguration _configuration;

        public UserProfileService(YangSpaceDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<User?> GetUserProfileAsync(string userId)
        {
            return await _context.Users
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync() as User;
        }

        public ClaimsPrincipal GetUserProfileAsyncByToken(string token)
        {
            return JwtHelper.GetPrincipalFromToken(token, _configuration["Jwt:SecretKey"]!);
        }


        public async Task<bool> UpdateUserProfileAsync(string userId, UserProfileModel model)
        {
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync() as User;
            if (user == null) return false;

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.ProfilePictureUrl = model.ProfilePictureUrl;
            user.Bio = model.Bio;
            user.PhoneNumber = model.PhoneNumber;
            user.Location = model.Location;


            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Booking>> GetBookedServicesAsync(string userId, bool isServiceProvider)
        {
            if (isServiceProvider)
            {
                return await _context.Bookings
                    .Where(b => b.Service.ProviderId == userId) // Assuming 'Provider' refers to a user who is the provider
                    .ToListAsync();
            }

            return await _context.Bookings
                .Where(b => b.UserId == userId) // Client bookings
                .ToListAsync();
        }
    }
}