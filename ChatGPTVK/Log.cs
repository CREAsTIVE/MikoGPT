using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatGPTVK
{
    static class Log
    {
        public const string LogFile = "Logs.txt";
        public const string ErrorLogFile = "Errors.txt";
        public static void logToFile(string filePath, string message)
        {
            if (!File.Exists(filePath))
                File.Create(filePath).Close();
            File.AppendAllText(filePath, $"{DateTime.Now} | {message}\n\n");
        }
        public static void log(string message, bool display = false)
        {
            logToFile(LogFile, message);
            if (display)
                Console.Write($"{DateTime.Now} | {message.Replace("\n", "\n\t\t")}\n\n");
        }
        public static void warn(string message, bool display = false)
        {
            logToFile(ErrorLogFile, message);
            logToFile(LogFile, message);
            Console.Write($"{DateTime.Now} | ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{message.Replace("\n", "\n\t\t")}\n\n");
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void error(string message)
        {
            logToFile(ErrorLogFile, message);
            logToFile(LogFile, message);
            Console.Write($"{DateTime.Now} | ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{message.Replace("\n", "\n\t\t")}\n\n");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
