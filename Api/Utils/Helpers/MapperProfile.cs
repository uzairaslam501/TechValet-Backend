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

        CreateMap<BlogViewModel, Blog>();
        CreateMap<Blog, BlogViewModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.EncId, opt => opt.MapFrom(src => StringCipher.EncryptId(src.Id)));
    }
}
