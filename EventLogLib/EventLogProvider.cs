using IotechiCore.Mediator.Colleague;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventLogLib
{
    public class SystemLogProvider : IotechiCore.Base.CsSingleInstance<SystemLogProvider>, IDisposable
    {
        EventLogProvider eventLogPv = null;
        public EventLogProvider EventLogPv { get => eventLogPv; set => eventLogPv = value; }
        public string LogName { get => eventLogPv.LogName; }
        public void InitSystemLog(string LogName,string rootPath)
        {
            eventLogPv = new EventLogProvider(LogName, rootPath);
        }

        public void Dispose()
        {
            EventLogPv.Dispose();
        }
    }

    public class EventLogProvider : IDisposable
    {
        //private Timer receiveCheckTimer = null;
        private string LOG_FILE_PATH = string.Empty;

        public string LogName { get; private set; }

        private RoutedIotechiCoreColleague<EventLogParam> _logDistributedColleague = null;
        private string fileRootPath = "";
        public EventLogProvider(string logName, string rootPath, bool useMediator = true)
        {
            fileRootPath = rootPath;
            LogName = logName;
            if (useMediator)
            {
                _logDistributedColleague = new RoutedIotechiCoreColleague<EventLogParam>(LogName, "EventLogProvider", EventLogMediator.Instance(), WriteLog);
            }
            LOG_FILE_PATH = string.Format(@"{0}\EVENT_LOG\{1}\{1}_{2}.csv", rootPath, LogName, DateTime.Now.ToString("yyyyMMdd"));

            FileInfo info = new FileInfo(LOG_FILE_PATH);
            if (info.Directory.Exists == false) info.Directory.Create();

        }

        private List<EventLogParam> logListBuffer = new List<EventLogParam>();
        private ReaderWriterLockSlim _logLock = new ReaderWriterLockSlim();

        public void AsyncWriteLog(EventLogParam param)
        {
            Task.Factory.StartNew(new Action<object>((data) =>
            {
                WriteLog(data as EventLogParam);
            }), param);
        }        

        private void WriteLog(EventLogParam param)
        {
            _logLock.EnterWriteLock();
            logListBuffer.Add(param);
            if(logListBuffer.Count > 100)
            {
                WriteDataToFile();
            }
            _logLock.ExitWriteLock();
        }

        private void WriteDataToFile()
        {
            try
            {
                FileInfo info = new FileInfo(LOG_FILE_PATH);
                if (info.Exists)
                {
                    using (FileStream fs = new FileStream(LOG_FILE_PATH, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                        {
                            long nCurrentPosition = sw.BaseStream.Seek(0, SeekOrigin.End);
                            if (nCurrentPosition == 0)
                            {
                                sw.WriteLine("Date, Run Type, Level, Contents");
                                sw.Flush();
                            }
                            foreach (var x in logListBuffer)
                            {
                                if (x != null)
                                {
                                    string msg = string.Format("{0},{1},{2},{3}", x.EventTime.ToString("yyyy-MM-dd HH:mm:ss"), x.RunType, x.LogLevel.ToString(), x.Message);
                                    sw.WriteLine(msg);
                                }
                            }
                            logListBuffer.Clear();
                        }
                    }
                }
                else
                {
                    using (FileStream fs = new FileStream(LOG_FILE_PATH, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                        {
                            long nCurrentPosition = sw.BaseStream.Seek(0, SeekOrigin.End);

                            if (nCurrentPosition == 0)
                            {
                                sw.WriteLine("Date, Equipment, Level, Contents");
                                sw.Flush();
                            }
                            foreach (var x in logListBuffer)
                            {
                                string msg = string.Format("{0},{1},{2},{3}", x.EventTime.ToString("yyyy-MM-dd HH:mm:ss"), x.RunType, x.LogLevel.ToString(), x.Message);
                                sw.WriteLine(msg);
                            }
                            logListBuffer.Clear();
                        }
                    }
                }
                if (info.Exists && info.Length > 1000000)
                {
                    string moveFile = string.Format(@"{0}\EVENT_LOG\{1}\{1}_{2}.csv", fileRootPath, LogName, DateTime.Now.ToString("yyyyMMddHHmmss"));
                    info.MoveTo(moveFile);
                }

            }
            catch { }
        }

        public void Dispose()
        {
            if (logListBuffer.Count > 0)
            {
                _logDistributedColleague.Mediator.UnRegister(LogName, "EventLogProvider");
                using (FileStream fs = new FileStream(LOG_FILE_PATH, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        long nCurrentPosition = sw.BaseStream.Seek(0, SeekOrigin.End);
                        if (nCurrentPosition == 0)
                        {
                            sw.WriteLine("Date, Equipment, Level, Contents");
                            sw.Flush();
                        }
                        foreach (var x in logListBuffer)
                        {
                            if(x!= null)
                            {
                                string msg = string.Format("{0},{1},{2},{3}", x.EventTime.ToString("yyyy-MM-dd HH:mm:ss"), x.RunType, x.LogLevel.ToString(), x.Message);
                                sw.WriteLine(msg);
                            }
                           
                        }
                        logListBuffer.Clear();
                    }
                }
            }
        }


    }
}
