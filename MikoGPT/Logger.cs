using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikoGPT
{
    class Logger
    {
        public static Logger? Instance { get; set; }

        public IEnumerable<string> FileFilter = new LinkedList<string>();
        public IEnumerable<string> ConsoleFilter = new LinkedList<string>();

        FileStream? logFile = null;
        public Logger() { }
        public Logger(string file)
        {
            Directory.CreateDirectory("logs");
            logFile = new FileStream($"logs/log_{DateTime.Now.ToString().Replace("/", ".").Replace(" ", "_").Replace(":", "_")}.txt", FileMode.Append, FileAccess.Write);
        }
        ~Logger() =>
            logFile?.Close();

        public enum WarningLevel
        {
            Log,
            Warning,
            Error,
        }
        Dictionary<WarningLevel, ConsoleColor> consoleColors = new()
        {
            { WarningLevel.Log, ConsoleColor.White },
            { WarningLevel.Warning, ConsoleColor.Yellow },
            { WarningLevel.Error, ConsoleColor.Red },
        };
        public void Log(string filter, Exception exception)
        {
            Log(filter, exception.ToString(), WarningLevel.Error);
        }
        public void Log(string filter, string message, WarningLevel warningLevel=WarningLevel.Log)
        {
            lock (this)
            {
                var beginString = $"{DateTime.Now}|{filter.PadRight(15)}|{Enum.GetName(warningLevel)?.PadRight(7)}|: ";

                if (logFile is not null && !FileFilter.Contains(filter))
                {
                    logFile.Write(Encoding.UTF8.GetBytes(beginString + message + "\n"));
                    logFile.Flush();
                }

                if (!ConsoleFilter.Contains(filter))
                {
                    Console.Write(beginString);
                    Console.ForegroundColor = consoleColors[warningLevel];
                    Console.WriteLine(message);
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }
        public void Log(string message) => Log("", message);
    }
}
