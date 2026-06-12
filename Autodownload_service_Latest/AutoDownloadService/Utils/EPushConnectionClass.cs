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
    class EPushConnectionClass : IDisposable
    {
        public SqlConnection cnn;
        private IMessageLogger _logger;
        SqlConnection conn = null;

        public EPushConnectionClass()
        {
            _logger = new Filelogger("epushConnectionClass");
            if ((cnn = _cnn()) == null)
            {
                this.Dispose();
            }
        }

        private SqlConnection _cnn()
        {
            String cnnString = ConfigurationManager.ConnectionStrings["epush"].ConnectionString;

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
            conn.Close();
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
