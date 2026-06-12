using AutodownloadService.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutodownloadService.Interface.Unit
{
    public class Computation
    {
        private IMessageLogger _logger;
        private AMSConnectionClass _amsConn = new AMSConnectionClass();

        public Computation()
        {
            _logger = new Filelogger("Computation");
        }

        public void ComputeAttendance()
        {
            _logger.Log($"Compute Attendance Started");
            try
            {
                if (_amsConn.cnn.State == ConnectionState.Closed)
                    _amsConn.cnn.Open();
                SqlCommand compute = new SqlCommand("CreateMusterServiceProc", _amsConn.cnn);
                compute.CommandTimeout = 0;
                DateTime fromDate = DateTime.Parse($"{DateTime.Now.Year.ToString()}-{DateTime.Now.Month.ToString()}-01");
                DateTime toDate = DateTime.Now;
                compute.Parameters.AddWithValue("@fromDate", SqlDbType.DateTime).Value = fromDate;
                compute.Parameters.AddWithValue("@empid ", SqlDbType.DateTime).Value = string.Empty;
                compute.CommandType = CommandType.StoredProcedure;
                compute.ExecuteNonQuery();
                _amsConn.cnn.Close();
                _logger.Log($"Compute Attendance Done for {DateTime.Now.AddDays(-1).ToString("dd-MMM-yyyy")} and {DateTime.Now.ToString("dd-MMM-yyyy")}");
            }
            catch (Exception exception)
            {
                _logger.Error("Error while inserting the Data", exception);
            }
        }
    }
}
