using Microsoft.Data.SqlClient;
using System.Data;

namespace CRM_Api.Helpers
{
    public class SqlHelper
    {
        private static string GetConnectionString()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            return configuration.GetConnectionString("Connection");
        }

        public static SqlDataReader ExecuteReader(string query, SqlParameter[] parameters)
        {
            var conn = new SqlConnection(GetConnectionString());
            var cmd = new SqlCommand(query, conn);
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            conn.Open();
            return cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
        }

        public static int ExecuteNonQuery(string query, SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand(query, conn))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);

                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }
        public static int ExecuteNonQuery(string query, List<SqlParameter> parameters)
        {
            using SqlConnection conn = new SqlConnection(GetConnectionString());
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddRange(parameters.ToArray()); // ✅ Fix: Convert List to Array
            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        public static object ExecuteScalar(string query, SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand(query, conn))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);

                conn.Open();
                return cmd.ExecuteScalar();
            }
        }
        public static object ExecuteScalarWithParam(string query, List<SqlParameter> parameters)
        {
            using SqlConnection conn = new SqlConnection(GetConnectionString());
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddRange(parameters.ToArray());
            conn.Open();
            return cmd.ExecuteScalar();
        }

        public static DataSet ExecuteDataSet(string qry)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                using (SqlDataAdapter mySqlDA = new SqlDataAdapter(qry, con))
                {
                    DataSet dsControlData = new DataSet();
                    mySqlDA.Fill(dsControlData);
                    return dsControlData;
                }
            }
            catch (Exception ex)
            {
                // Log exception
                throw new InvalidOperationException("Error executing data set query", ex);
            }
        }

        public static DataSet ExecuteDataSetWithParams(string qry, SqlParameter[] param)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(GetConnectionString()))
                using (SqlCommand command = new SqlCommand(qry, con))
                using (SqlDataAdapter mySqlDA = new SqlDataAdapter(command))
                {
                    if (param != null)
                    {
                        command.Parameters.AddRange(param);
                    }

                    DataSet dsControlData = new DataSet();
                    mySqlDA.Fill(dsControlData);
                    return dsControlData;
                }
            }
            catch (Exception ex)
            {
                // Log exception
                throw new InvalidOperationException("Error executing parameterized data set query", ex);
            }
        }
        public static async Task<int> ExecuteNonQueryAsync(string query, SqlParameter[] parameters)
        {
            using SqlConnection conn = new SqlConnection(GetConnectionString());
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddRange(parameters);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }
        public static DataTable ExecuteDataTable(string query, SqlParameter[] parameters = null)
        {
            using SqlConnection conn = new(GetConnectionString());
            using SqlCommand cmd = new(query, conn);
            cmd.CommandType = CommandType.Text;
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            using SqlDataAdapter adapter = new(cmd);
            DataTable dt = new();
            adapter.Fill(dt);
            return dt;
        }
    }
}
