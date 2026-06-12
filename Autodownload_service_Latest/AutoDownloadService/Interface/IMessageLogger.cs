using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutodownloadService.Interface
{
    public interface IMessageLogger
    {
        void ConfigureLogger(string name);
        void Log(string message);
        void Debug(string message);
        void Error(string message, Exception exception);
        bool EnableDebug { get; set; }

        void WriteError(String message);
        void WriteErrorMessage(String message);
    }
}
