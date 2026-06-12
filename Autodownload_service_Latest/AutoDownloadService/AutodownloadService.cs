using AutodownloadService.Interface;
using AutodownloadService.Model;
using AutodownloadService.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace AutodownloadService
{
    public partial class AutodownloadService : ServiceBase
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

        private readonly IMessageLogger _logger;
        private readonly List<ManualResetEvent> _resetEvents;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;
        private readonly IAutodownload _autodownload;
        private readonly SemaphoreSlim _downloadSemaphore;

        public AutodownloadService()
        {
            log4net.GlobalContext.Properties["pid"] = Process.GetCurrentProcess().Id;

            _logger = new Filelogger("AutodownloadService");
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            _resetEvents = new List<ManualResetEvent>();
            _autodownload = new AutodownloadImpl();
            _downloadSemaphore = new SemaphoreSlim(1, 1);

            InitializeComponent();

            if (!EventLog.SourceExists("AutodownloadServiceSource"))
            {
                EventLog.CreateEventSource("AutodownloadServiceSource", "AutodownloadServiceLog");
            }

            eventLog1.Source = "AutodownloadServiceSource";
            eventLog1.Log = "AutodownloadServiceLog";

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.Log($"Unhandled Exception: {e.ExceptionObject}");
            eventLog1.WriteEntry($"Unhandled Exception: {e.ExceptionObject}", EventLogEntryType.Error);
        }

        protected override void OnStart(string[] args)
        {
#if DEBUG
            RequestAdditionalTime(30000);
            Debugger.Launch();
#endif

            eventLog1.WriteEntry("Service Starting...");
            UpdateServiceStatus(ServiceState.SERVICE_START_PENDING);

            int workersNumber = int.TryParse(ConfigurationManager.AppSettings["service.workersnumber"], out var wn) ? wn : 1;

            _logger.Log($"Starting {workersNumber} worker(s)");

            for (int i = 0; i < workersNumber; i++)
            {
                var resetEvent = new ManualResetEvent(false);
                _resetEvents.Add(resetEvent);
                StartWorker(i, resetEvent);
            }

            UpdateServiceStatus(ServiceState.SERVICE_RUNNING);
            _logger.Log("Service Started.");
        }

        private void StartWorker(int workerNumber, ManualResetEvent resetEvent)
        {
            string workerName = $"THREAD-{workerNumber + 1}";
            int intervalMinutes = int.TryParse(ConfigurationManager.AppSettings["service.intervalminutes"], out var im) && im > 0 ? im : 1;
            TimeSpan workerInterval = TimeSpan.FromMinutes(intervalMinutes);

            Task.Run(async () =>
            {
                _logger.Log($"Worker {workerName} started. Interval: {intervalMinutes} minute(s).");

                try
                {
                    while (!_cancellationToken.IsCancellationRequested)
                    {
                        var start = DateTime.Now;

                        try
                        {
                            _logger.Log($"[{workerName}] Cycle started at {start}");

                            if (workerNumber == 0)
                            {
                                ExecuteDownloadProcess();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Log($"[{workerName}] ERROR: {ex}");
                        }

                        var duration = DateTime.Now - start;

                        _logger.Log($"[{workerName}] Cycle completed in {duration}");

                        await Task.Delay(workerInterval, _cancellationToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.Log($"Worker {workerName} cancelled.");
                }
                finally
                {
                    resetEvent.Set();
                    _logger.Log($"Worker {workerName} stopped.");
                }

            }, _cancellationToken);
        }

        private void ExecuteDownloadProcess()
        {
            bool semaphoreAcquired = false;

            try
            {
                if (_downloadSemaphore.CurrentCount == 0)
                {
                    _logger.Log("Another process is running. Waiting for it to finish...");
                }

                _downloadSemaphore.Wait(_cancellationToken);
                semaphoreAcquired = true;

                var rawData = _autodownload.DownloadFromEpush();

                if (rawData?.Count > 0)
                {
                    _logger.Log("Creating ATND file...");
                    _autodownload.CreateAtndFile(rawData);

                    _logger.Log("Uploading to SAP...");
                    _autodownload.PostDataToDB();

                }
                else
                {
                    _logger.Log("No data found.");
                }
                _logger.Log("ComputeAttendace...");
                _autodownload.Compute();
            }
            catch (OperationCanceledException)
            {
                _logger.Log("Download process cancelled.");
            }
            catch (Exception ex)
            {
                _logger.Log($"Process error: {ex}");
            }
            finally
            {
                if (semaphoreAcquired)
                {
                    _downloadSemaphore.Release();
                }
            }
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("Service Stopping...");
            UpdateServiceStatus(ServiceState.SERVICE_STOP_PENDING);

            _logger.Log("Stopping service...");

            StopService();

            UpdateServiceStatus(ServiceState.SERVICE_STOPPED);
            _logger.Log("Service stopped.");
        }

        private void StopService()
        {
            int timeout = int.TryParse(ConfigurationManager.AppSettings["service.stoptimeout"], out var t) ? t : 5000;
            ManualResetEvent[] resetEvents = _resetEvents.ToArray();

            _cancellationTokenSource.Cancel();

            try
            {
                if (resetEvents.Length > 0)
                {
                    WaitHandle.WaitAll(resetEvents.Cast<WaitHandle>().ToArray(), timeout);
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Error while stopping workers: {ex}");
            }
            finally
            {
                foreach (ManualResetEvent resetEvent in resetEvents)
                {
                    resetEvent.Dispose();
                }

                _resetEvents.Clear();
            }
        }

        private void UpdateServiceStatus(ServiceState state)
        {
            ServiceStatus status = new ServiceStatus
            {
                dwCurrentState = state,
                dwWaitHint = 100000
            };

            SetServiceStatus(ServiceHandle, ref status);
        }

        public enum ServiceState
        {
            SERVICE_STOPPED = 1,
            SERVICE_START_PENDING = 2,
            SERVICE_STOP_PENDING = 3,
            SERVICE_RUNNING = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        }
    }
}
