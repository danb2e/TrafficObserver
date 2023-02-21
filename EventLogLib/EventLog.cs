using IotechiCore.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventLogLib
{
    public enum EnLogLevel { Normal, Warning, Error }
    public class EventLogParam : ViewModel
    {
        public string RunType { get { return (string)this["RunType"]; } set { this["RunType"] = value; } }
        public DateTime EventTime { get { return (DateTime)this["EventTime"]; } set { this["EventTime"] = value; } }
        public string EventTimeStr { get { return (string)this["EventTimeStr"]; } set { this["EventTimeStr"] = value; } }
        public EnLogLevel LogLevel { get { return (EnLogLevel)this["LogLevel"]; } set { this["LogLevel"] = value; } }
        public string Message { get { return (string)this["Message"]; } set { this["Message"] = value; } }
        public EventLogParam(string eq, DateTime time, EnLogLevel level, string message)
        {
            RunType = eq;
            EventTime = time;
            EventTimeStr = time.ToString("yyyy-MM-dd HH:mm:ss");
            LogLevel = level;
            Message = message;
        }
        public EventLogParam()
        {

        }
    }
}
