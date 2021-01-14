using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CodeUin.Dapper
{
    public class RepositoryBase<T> : IRepositoryBase<T>
    {
        public async Task Delete(int Id, string deleteSql)
        {
            using (IDbConnection conn = DataBaseConfig.GetMySqlConnection())
            {
                await conn.ExecuteAsync(deleteSql, new { Id });
            }
        }

        public async Task<T> Detail(int Id, string detailSql)
        {
            using (IDbConnection conn = DataBaseConfig.GetMySqlConnection())
            {
                return await conn.QueryFirstOrDefaultAsync<T>(detailSql, new { Id });
            }
        }

        public async Task<List<T>> ExecQuerySP(string SPName)
        {
            using (IDbConnection conn = DataBaseConfig.GetMySqlConnection())
            {
                return await Task.Run(() => conn.Query<T>(SPName, null, null, true, null, CommandType.StoredProcedure).ToList());
            }
        }

        public async Task<int> Insert(T entity, string insertSql)
        {
            using (IDbConnection conn = DataBaseConfig.GetMySqlConnection())
            {
                return await conn.ExecuteAsync(insertSql, entity);
            }
        }

        public async Task<List<T>> Select(string selectSql)
        {
            using (IDbConnection conn = DataBaseConfig.GetMySqlConnection())
            {
                return await Task.Run(() => conn.Query<T>(selectSql).ToList());
            }
        }

        public async Task Update(T entity, string updateSql)
        {
            using (IDbConnection conn = DataBaseConfig.GetMySqlConnection())
            {
                await conn.ExecuteAsync(updateSql, entity);
            }
        }
    }
}
