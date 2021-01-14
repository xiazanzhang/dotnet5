using CodeUin.Dapper.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeUin.Dapper.IRepository
{
    public interface IUserRepository : IRepositoryBase<Users>
    {
        Task<List<Users>> GetUsers();

        Task<int> AddUser(Users entity);

        Task DeleteUser(int d);

        Task<Users> GetUserDetail(int id);

        Task<Users> GetUserDetailByEmail(string email);
    }
}
