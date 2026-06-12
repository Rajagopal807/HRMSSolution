using AutodownloadService.Interface;
using log4net.Appender;
using log4net.Config;
using log4net.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutodownloadService.Utils
{
    class Filelogger : MarshalByRefObject, IMessageLogger
    {
        private log4net.ILog Logger = null;
        private Dictionary<string, string> _appenderList = new Dictionary<string, string>();
        private static bool _isConfigured = false;

        public Filelogger(string name)
        {
            if (!_isConfigured)
            {
                XmlConfigurator.Configure();
                _isConfigured = true;
            }

            Logger = log4net.LogManager.GetLogger(name);
            //var asm = Assembly.GetExecutingAssembly();
            //get the current logging repository for this application 
            ILoggerRepository repository = Logger.Logger.Repository;
            //get all of the appenders for the repository 
            IAppender[] appenders = repository.GetAppenders();
            //only change the file path on the 'FileAppenders' 
            foreach (IAppender appender in (from iAppender in appenders
                                            where iAppender is FileAppender
                                            select iAppender))
            {
                FileAppender fileAppender = appender as FileAppender;
                string fileName = string.Empty;
                if (_appenderList.ContainsKey(fileAppender.Name))
                {
                    fileName = _appenderList[fileAppender.Name];
                    if (!fileName.StartsWith("ComputeService"))
                    {
                        _appenderList[fileAppender.Name] = fileName;
                    }
                }
                else
                {
                    FileInfo fileInfo = new FileInfo(fileAppender.File);
                    fileName = fileInfo.Name;
                    if (!fileName.StartsWith("ComputeService"))
                        _appenderList.Add(fileAppender.Name, fileName);
                }

                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServiceLogs");
                if (!Directory.Exists(logPath))
                    Directory.CreateDirectory(logPath);

                //set the path to your logDirectory using the original file name defined 
                //in configuration 
                fileAppender.File = Path.Combine(logPath, Path.GetFileName(fileName));
                //make sure to call fileAppender.ActivateOptions() to notify the logging 
                //sub system that the configuration for this appender has changed. 
                fileAppender.ActivateOptions();
            }
        }
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void ConfigureLogger(string name)
        {
            Logger = log4net.LogManager.GetLogger(name);
        }

        public bool EnableDebug { get; set; }

        public void Error(string message, System.Exception exception)
        {
            Logger.Error(message, exception);
        }

        public void Log(string message)
        {
            Logger.Info(message);
        }

        public void Debug(string message)
        {
            if (EnableDebug)
                Logger.Debug(message);
        }

        public void WriteError(string message)
        {
            Logger.Error(message);
        }

        public void WriteErrorMessage(string message)
        {
            Logger.Error(message);
        }
    }
}
