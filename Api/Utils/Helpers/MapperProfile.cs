using AutoMapper;
using ITValet.HelpingClasses;
using ITValet.Models;

namespace ITValet.Utils.Helpers;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<AddUpdateBlogViewModel, Blog>() // src , dest
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.EncId) ? 
                (int?)null : 
                StringCipher.DecryptionId(src.EncId)));

        CreateMap<BlogViewModel, Blog>().ReverseMap();
        CreateMap<Blog, BlogViewModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.EncId, opt => opt.MapFrom(src => StringCipher.EncryptId(src.Id)))
            .ForMember(dest => dest.PublishedDate, opt => opt.MapFrom(src =>
                src.PublishedDate.HasValue ?
                src.PublishedDate.Value.ToString("MMMM dd, yyyy") :
                ""))
            .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}
