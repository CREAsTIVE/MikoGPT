using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MikoGPT.ConsoleCommandController
{
    public class ConsoleCommandController
    {
        public struct CommandParams
        {
            public CommandParams(string command, string[] args)
            {
                this.args = args;
                this.command = command;
            }
            public string command;
            public string[] args;

            int argIndex;

            public string GetNext() => args[argIndex++];
            public string GetToEnd() => string.Join(" ", args[argIndex..]);
        }
        public delegate void CommandCallback(CommandParams args);

        public Dictionary<string, CommandCallback> commands = new();
        public void RegisterCommand(string name, CommandCallback callback) => commands[name] = callback;
        public void Listen()
        {
            while (true)
            {
                string input = Console.ReadLine() ?? "";
                if (input == "") continue;

                string[] args = input.Split(' ');
                string commandName = args[0];
                args = args[1..];

                if (commands.TryGetValue(commandName, out var callback))
                {
                    try
                    {
                        callback(new CommandParams(commandName, args));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error ocurred on command {commandName}, with args [{string.Join(" ", args)}]:\n{e}");
                    }
                    continue;
                }

                Console.WriteLine("Undefined command");
                Console.WriteLine($"Command list: \n{string.Join("\n", commands.Keys)}");
            }
        }
        public void ListenAsync() => Task.Run(Listen);
    }
}
