using AutoMapper;
using YangSpaceBackEnd.Data.Models;
using YangSpaceBackEnd.Data.ViewModel;

namespace YangSpaceBackEnd.Data.Extension
{
    public class BookingProfile : Profile
    {
        public BookingProfile()
        {
            CreateMap<Booking, BookingViewModel>()
                .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service.Title))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FirstName + " " + src.User.LastName));
        }
    }
}
