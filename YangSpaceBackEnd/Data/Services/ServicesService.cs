
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YangSpaceBackEnd.Data.Models;
using YangSpaceBackEnd.Data.Services.Contracts;
using YangSpaceBackEnd.Data.ViewModel;

namespace YangSpaceBackEnd.Data.Services;

public class ServicesService : IServiceService
{
    private readonly YangSpaceDbContext _context;
    private readonly UserManager<User> _userManager;

    public ServicesService(YangSpaceDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<PagedResult<Service>> GetPagedServicesAsync(int page = 1, int pageSize = 10, string? category = null, string? search = null, decimal? minPrice = null, decimal? maxPrice = null, string? sortBy = null)
    {
        var query = _context.Services.Include(c => c.Category).Include(p => p.Provider).AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(s => s.Category.Name == category);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(s => s.Title.Contains(search) || s.Description.Contains(search));

        if (minPrice.HasValue)
            query = query.Where(s => s.Price >= minPrice);

        if (maxPrice.HasValue)
            query = query.Where(s => s.Price <= maxPrice);

        query = sortBy?.ToLower() switch
        {
            "price" => query.OrderBy(s => s.Price),
            "name" => query.OrderBy(s => s.Title),
            "recent" or null => query.OrderByDescending(s => s.CreatedAt),
            _ => query
        };

        var totalCount = await query.CountAsync();
        var services = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new Service
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Price = s.Price,
                Category = s.Category,
                Provider = s.Provider
            })
            .ToListAsync();

        return new PagedResult<Service>
        {
            TotalCount = totalCount,
            Items = services
        };
    }

    public async Task<Service> CreateServiceAsync(ServiceViewModel serviceModel, string? providerId)
    {
        var service = new Service
        {
            Title = serviceModel.Title,
            Description = serviceModel.Description,
            Price = serviceModel.Price,
            ProviderId = providerId,
            CategoryId = serviceModel.CategoryId
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return service;
    }

    public async Task<List<Service>> GetServicesByProviderAsync(string? providerId)
    {
        return await _context.Services.Where(s => s.ProviderId == providerId).ToListAsync();
    }

    public async Task<bool> UpdateServiceAsync(Service service)
    {
        _context.Entry(service).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteServiceAsync(int serviceId)
    {
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null) return false;

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Service?> GetServiceByIdAsync(int id)
    {
        return await _context.Services.FindAsync(id);
    }

    public async Task<bool> BookServiceAsync(User user, Service service)
    {
        var booking = new Models.Booking
        {
            UserId = user.Id,
            ServiceId = service.Id,
            BookingDate = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CheckUserAccessToServiceAsync(User user, int serviceId)
    {
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null) return false;

        var isProvider = service.ProviderId == user.Id;
        var hasBooking = await _context.Bookings.AnyAsync(b => b.UserId == user.Id && b.ServiceId == serviceId);

        return isProvider || hasBooking;
    }

    public async Task<List<string>> GetCategories()
    {
        return await _context.Categories.Select(c => c.Name).ToListAsync();
    }
}