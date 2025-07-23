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
            // need something to signify where data appended should be, using an underscore after name here, could use dash or another acceptable symbol based on libary docs
            Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File(path: "../../../Data/FileScouterLog_.txt", rollingInterval: RollingInterval.Day).CreateLogger();
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
