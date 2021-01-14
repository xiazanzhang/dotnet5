using Autofac;
using CodeUin.Dapper.IRepository;
using CodeUin.Dapper.Repository;

namespace CodeUin.WebApi.Autofac
{
    public class CustomAutofacModule:Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<UserRepository>().As<IUserRepository>();
        }
    }
}
