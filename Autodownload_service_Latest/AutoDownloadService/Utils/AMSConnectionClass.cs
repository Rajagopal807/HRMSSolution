using AutodownloadService.Interface;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutodownloadService.Utils
{
    class AMSConnectionClass : IDisposable
    {
        public SqlConnection cnn;
        private IMessageLogger _logger;


        private SqlConnection conn = null;
        private List<SqlParameter> _parameters;

        public string ErrorNo { get; set; }
        public string ErrorMessage { get; set; }

        public AMSConnectionClass()
        {
            _logger = new Filelogger("AMSConnectionClass");
            _parameters = new List<SqlParameter>();
            if ((cnn = _cnn()) == null)
            {
                this.Dispose();
            }
        }

        private SqlConnection _cnn()
        {
            String cnnString = ConfigurationManager.ConnectionStrings["ams"].ConnectionString;

            try
            {
                conn = new SqlConnection();
                conn.ConnectionString = cnnString;
                return conn;
            }
            catch (Exception exception)
            {
                conn.Dispose();
                _logger.Error($"Error in DBConnection", exception);
                return null;
            }
        }

        public void ExecuteQueries(string Query_)
        {
            if (conn.State == ConnectionState.Open)
                conn.Close();
            conn.Open();
            SqlCommand cmd = new SqlCommand(Query_, conn);
            cmd.ExecuteNonQuery();
        }


        public SqlDataReader DataReader(string Query_)
        {
            if (conn.State == ConnectionState.Open)
                conn.Close();
            conn.Open();
            SqlCommand cmd = new SqlCommand(Query_, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            return dr;
        }

        public int ExecuteStoreProcedure(string ProcedureName, bool isResultExpected = true)
        {
            try
            {
                int result = 0;
                if (conn.State == ConnectionState.Open)
                    conn.Close();
                conn.Open();
                SqlCommand cmd = new SqlCommand(ProcedureName, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                foreach (SqlParameter parameter in _parameters)
                {
                    cmd.Parameters.Add(parameter);
                }
                cmd.ExecuteNonQuery();
                if (isResultExpected)
                    result = cmd.Parameters.OfType<SqlParameter>().Where(p => p.Direction == ParameterDirection.Output).Select(p => Convert.ToInt32(p.Value)).FirstOrDefault();
                return result;
            }
            catch (Exception exception)
            {
                throw new Exception($"Error executing stored procedure {ProcedureName}", exception);
            }
        }

        public void ClearParameters()
        {
            _parameters.Clear();
        }

        public void AddParameter(string name, object value, SqlDbType type, ParameterDirection direction)
        {
            SqlParameter parameter = new SqlParameter(name, type);

            if (direction == ParameterDirection.Output || direction == ParameterDirection.InputOutput)
            {
                parameter.Value = DBNull.Value; // Required for output
            }
            else
            {
                parameter.Value = value ?? DBNull.Value;
            }

            parameter.Direction = direction;

            // IMPORTANT: Set size for variable-length types
            if (type == SqlDbType.VarChar || type == SqlDbType.NVarChar)
            {
                parameter.Size = 500; // adjust as needed
            }

            _parameters.Add(parameter);
        }


        public object ShowDataInGridView(string Query_)
        {
            String cnnString = ConfigurationManager.ConnectionStrings["cn"].ConnectionString;

            SqlDataAdapter dr = new SqlDataAdapter(Query_, cnnString);
            DataSet ds = new DataSet();
            dr.Fill(ds);
            object dataum = ds.Tables[0];
            return dataum;
        }

        public bool _cnnStatus()
        {
            String cnnString = ConfigurationManager.ConnectionStrings["cn"].ConnectionString;
            bool connState = true;
            try
            {
                SqlConnection connStatus = null;

                connStatus = new SqlConnection();
                connStatus.ConnectionString = cnnString;
                connStatus.Open();
                connStatus.Close();
            }
            catch
            {
                connState = false;
            }
            return connState;
        }
        public void Dispose()
        {
            if (cnn != null)
            {
                cnn.Dispose();
            }
        }
    }
}
