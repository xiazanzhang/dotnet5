using MySql.Data.MySqlClient;
using System.Data;

namespace CodeUin.Dapper
{
    public class DataBaseConfig
    {
        private static string MySqlConnectionString = @"Data Source=118.89.22.105;Initial Catalog=codeuin;Charset=utf8mb4;User ID=root;Password=Xzz306556182;";
        public static IDbConnection GetMySqlConnection(string sqlConnectionString = null)
        {
            if (string.IsNullOrWhiteSpace(sqlConnectionString))
            {
                sqlConnectionString = MySqlConnectionString;
            }
            IDbConnection conn = new MySqlConnection(sqlConnectionString);
            conn.Open();
            return conn;
        }
    }
}
