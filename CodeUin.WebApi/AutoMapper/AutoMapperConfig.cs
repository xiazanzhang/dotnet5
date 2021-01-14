using AutoMapper;
using CodeUin.Dapper.Entities;
using CodeUin.WebApi.Models;

namespace CodeUin.WebApi.AutoMapper
{
    public class AutoMapperConfig : Profile
    {
        public AutoMapperConfig()
        {
            CreateMap<UserRegisterModel, Users>().ReverseMap();
            CreateMap<UserLoginModel, Users>().ReverseMap();
            CreateMap<UserLoginModel, UserModel>().ReverseMap();
            CreateMap<UserModel, Users>().ReverseMap();
        }
    }
}
