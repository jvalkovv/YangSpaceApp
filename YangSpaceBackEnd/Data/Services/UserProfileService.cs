using Microsoft.EntityFrameworkCore;
using YangSpaceBackEnd.Data.Models;
using YangSpaceBackEnd.Data.Services.Contracts;
using YangSpaceBackEnd.Data.ViewModel.AccountViewModel;

namespace YangSpaceBackEnd.Data.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly YangSpaceDbContext _context;

        public UserProfileService(YangSpaceDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserProfileAsync(string userId)
        {
            return await _context.Users
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync() as User;
        }

        public async Task<bool> UpdateUserProfileAsync(string userId, UserProfileModel model)
        {
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync() as User;

            if (user == null) return false;

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Models.Booking>> GetBookedServicesAsync(string userId, bool isServiceProvider)
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