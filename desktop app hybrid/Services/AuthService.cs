//using Accessibility;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics;

namespace desktop_app_hybrid.Services
{
    public class AuthService
    {
        private readonly string _cs;

        public AuthService(IConfiguration config)
        {
            _cs = config.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' not found");
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync();

                const string sql = """ select dbo.CheckLogin(@u,@p)""";

                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", password);

                return Convert.ToBoolean(await cmd.ExecuteScalarAsync()) ;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw; // important pour voir l’erreur exacte
            }
        }

        public async Task<bool> SignupAsync(string username, string mail, string password, int it, int ist,string noms,string prenoms, string role)
        {
            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("dbo.sp_createuser", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@mail", mail);
                cmd.Parameters.AddWithValue("@password", password);
                cmd.Parameters.AddWithValue("@it", it);
                cmd.Parameters.AddWithValue("@ist", ist);
                cmd.Parameters.AddWithValue("@nom", noms);
                cmd.Parameters.AddWithValue("@prenom", prenoms);
                cmd.Parameters.AddWithValue("@role", role);

                var returnparam = cmd.Parameters.Add("@ReturnVal",SqlDbType.Int);
                returnparam.Direction = ParameterDirection.ReturnValue;

                 await cmd.ExecuteNonQueryAsync();
                int result = (int)returnparam.Value;
                Debug.WriteLine($"success {result} {role}");
                return result==1;
            }
            catch (SqlException ex)
            {
                Debug.WriteLine("SQL ERROR: " + ex.Message);
                throw; // garde-le pour voir l'erreur exacte
            }
        }

        public async Task<string[]> Selector(string wts,string table, string condition ="")
        {
            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync();

                string sql = $"""
                   SELECT {wts} from {table}
                """;

                string add = !string.IsNullOrEmpty(condition)? $"  where {condition}" : "";
                sql += add;
                await using var cmd = new SqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                int counter = 0;
                string[] values = new string[100];
                while(await reader.ReadAsync())
                {
                    values[counter] = Convert.ToString(reader.GetValue(counter));
                    counter++;
                }
                return values;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw; // important pour voir l’erreur exacte
            }
        }

    }

}