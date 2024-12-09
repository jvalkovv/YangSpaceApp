
using YangSpaceApp.Server.Data.Models;
using YangSpaceApp.Server.Data.ViewModel;

namespace YangSpaceApp.Server.Data.Services.Contracts;

public interface IServiceService
{
    Task<PagedResult<ServiceViewModel>> GetPagedServicesAsync(int page = 1, int pageSize = 10, string? category = null,
        string? search = null, decimal? minPrice = null, decimal? maxPrice = null, string? sortBy = null);
    Task<List<Service>> GetServicesByProviderAsync(string? providerId);
    Task<Service?> GetServiceByIdAsync(int id);

    Task<Service?> GetServiceWithImageAsync(int serviceId);
    Task<Service> CreateServiceAsync(ServiceViewModel serviceModel, string? providerId);
    Task<bool> BookServiceAsync(User user, Service service);
    Task<bool> CheckUserAccessToServiceAsync(User user, int serviceId);
    Task<bool> UpdateServiceAsync(ServiceViewModel serviceModel);
    Task<bool> DeleteServiceAsync(int id);
    Task<List<Category>> GetCategories();
}

public class PagedResult<T>
{
    public int TotalCount { get; set; }
    public IEnumerable<T> Services { get; set; }
}
