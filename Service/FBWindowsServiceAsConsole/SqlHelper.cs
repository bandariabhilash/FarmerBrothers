using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace WOScheduleEventService
{
    public class SqlHelper
    {
        private string connectionString = string.Empty;
        private SqlConnection sqlConn;
        private SqlDataAdapter sqlAdapter;
        private SqlCommand sqlCommand;

        public SqlHelper()
        {
            connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ToString();
        }

        public DataTable GetDatatable(string commandText)
        {
            return ExecuteReader(commandText, CommandType.Text);
        }

        public DataTable GetDatatable(string commandText, bool isProcedure)
        {
            return ExecuteReader(commandText, CommandType.StoredProcedure);
        }

        private DataTable ExecuteReader(string commandText, CommandType commandType)
        {
            DataTable dataset = new DataTable();
            using (sqlConn = new SqlConnection(connectionString))
            {
                using (sqlCommand = new SqlCommand(commandText, sqlConn))
                {
                    try
                    {
                        sqlCommand.CommandType = commandType;
                        using (sqlAdapter = new SqlDataAdapter())
                        {
                            sqlAdapter.SelectCommand = sqlCommand;
                            sqlAdapter.Fill(dataset);
                            return dataset;
                        }
                    }
                    finally
                    {
                        sqlConn.Close();
                    }
                }
            }
        }

        public DataTable GetDatatable(SqlCommand sqlCommand)
        {
            DataTable dataset = new DataTable();
            using (sqlConn = new SqlConnection(connectionString))
            {
                try
                {
                    sqlCommand.Connection = sqlConn;
                    using (sqlCommand)
                    {
                        using (sqlAdapter = new SqlDataAdapter())
                        {
                            sqlAdapter.SelectCommand = sqlCommand;
                            sqlAdapter.Fill(dataset);
                        }
                    }
                }
                catch { }
                return dataset;
            }
        }

        public int UpdateCommand(string commandText)
        {
            using (sqlConn = new SqlConnection(connectionString))
            {
                using (sqlCommand = new SqlCommand(commandText, sqlConn))
                {
                    try
                    {
                        sqlConn.Open();
                        sqlCommand.CommandType = CommandType.Text;
                        return sqlCommand.ExecuteNonQuery();
                    }
                    finally
                    {
                        sqlConn.Close();
                    }
                }
            }
        }
        
    }
}
