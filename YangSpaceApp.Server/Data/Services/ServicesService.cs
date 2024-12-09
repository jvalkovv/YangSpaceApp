using Microsoft.EntityFrameworkCore;
using YangSpaceApp.Server.Data.Models;
using YangSpaceApp.Server.Data.Services.Contracts;
using YangSpaceApp.Server.Data.ViewModel;

namespace YangSpaceApp.Server.Data.Services;

public class ServicesService : IServiceService
{
    private readonly IImageService _imageService;
    private readonly YangSpaceDbContext _context;
    public ServicesService(YangSpaceDbContext context, IImageService imageService)
    {
        _context = context;
        _imageService = imageService;
    }

    public async Task<PagedResult<ServiceViewModel>> GetPagedServicesAsync(int page = 1, int pageSize = 10,
        string? category = null, string? search = null, decimal? minPrice = null, decimal? maxPrice = null,
        string? sortBy = null)
    {
        var query = _context.Services.Include(c => c.Category)
            .Include(p => p.Provider)
            .Include(i => i.ServiceImages)
            .AsQueryable();

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
            .Select(s => new ServiceViewModel()
            {
                ServiceId = s.Id,
                CategoryId = s.CategoryId,
                Title = s.Title,
                Description = s.Description,
                Price = s.Price,
                CategoryName = s.Category.Name,
                ProviderName = $"{s.Provider.FirstName} {s.Provider.LastName}",
                ImageUrl = s.ServiceImages.FirstOrDefault()!.ImageUrl,
                Email = s.Provider.Email,
                PhoneNumber = s.Provider.PhoneNumber

            })
            .ToListAsync();



        return new PagedResult<ServiceViewModel>
        {
            TotalCount = totalCount,
            Services = services
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
            CategoryId = serviceModel.CategoryId,
            CreatedAt = DateTime.Now
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var imageFile = serviceModel.ImageFile;
        string? imageUrl = null;

        if (imageFile != null)
        {

            imageUrl = await _imageService.SaveImageAsync(imageFile, service.Id);
        }

        if (imageUrl != null)
        {
            var serviceImage = new ServiceImage
            {
                ImageUrl = imageUrl,
                ServiceId = service.Id
            };
            _context.ServiceImages.Add(serviceImage);
            await _context.SaveChangesAsync();
        }

        return service;
    }

    public async Task<List<Service>> GetServicesByProviderAsync(string? providerId)
    {
        return await _context.Services.Where(s => s.ProviderId == providerId).ToListAsync();
    }

    public async Task<bool> UpdateServiceAsync(ServiceViewModel serviceModel)
    {
        // Find the existing service in the database
        var service = await _context.Services
            .Include(s => s.ServiceImages) // Include related ServiceImages
            .FirstOrDefaultAsync(s => s.Id == serviceModel.ServiceId);

        if (service == null)
            return false; // If no service is found, return false

        // Update service properties from the ViewModel
        service.Title = serviceModel.Title;
        service.Description = serviceModel.Description;
        service.Price = serviceModel.Price;
        service.CategoryId = serviceModel.CategoryId;

        var imageFile = serviceModel.ImageFile;
        string? imageUrl = null;

        if (imageFile != null)
        {
            // Clear existing images
            _context.ServiceImages.RemoveRange(service.ServiceImages);

            imageUrl = await _imageService.SaveImageAsync(imageFile, service.Id);
        }

        if (imageUrl != null)
        {
            var serviceImage = new ServiceImage
            {
                ImageUrl = imageUrl,
                ServiceId = service.Id
            };
            _context.ServiceImages.Add(serviceImage);
            await _context.SaveChangesAsync();
        }


        // Mark service entity as modified
        _context.Entry(service).State = EntityState.Modified;

        // Save changes to the database
        await _context.SaveChangesAsync();
        return true;
    }
    // Helper Method for Saving Image File
    private async Task<string> SaveImageFile(IFormFile imageFile)
    {
        var uploadsFolder = Path.Combine("wwwroot", "images");
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(fileStream);
        }

        return "/images/" + uniqueFileName; // Return relative URL
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
        var service = await _context.Services.Where(s => s.Id == id).FirstOrDefaultAsync();


        return service;
    }

    public async Task<Service?> GetServiceWithImageAsync(int serviceId)
    {
        var service = await _context.Services
            .Where(s => s.Id == serviceId)
            .Include(s => s.ServiceImages)
            .FirstOrDefaultAsync();


        if (service != null && service.ServiceImages.Any())
        {

            var imageUrl = service.ServiceImages.FirstOrDefault()?.ImageUrl;

            if (!string.IsNullOrEmpty(imageUrl))
            {
                service.ServiceImages.Add(new ServiceImage { ImageUrl = imageUrl });
            }
        }

        return service;
    }


    public async Task<bool> BookServiceAsync(User user, Service service)
    {
        var booking = new Booking
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

    public async Task<List<Category>> GetCategories()
    {
        return await _context.Categories.ToListAsync();
    }
}