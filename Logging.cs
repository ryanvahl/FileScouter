using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace FileScouter
{
    public static class LoggingUtil
    {        
        public static void ConfigureLoggingUtil()
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("FileScouterLog.txt", rollingInterval: RollingInterval.Day).CreateLogger();
        }
        public static void LogInfo(string message)
        {
            Log.Information(message);
        }

        public static void LogError(string message)
        {
            Log.Error(message);
        }

        public static void LogClose()
        {
            Log.CloseAndFlush();
        }
    }
}
