namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    /// <summary>
    /// Top level cli argument parser.
    /// Returns cli command and the reset of the arguments.
    /// </summary>
    public static class CliParser
    {
        public static List<CliCommandModel> AllCommands { get; private set; } 
        
        private static readonly object SyncLock = new object();
        
        public static void Scan()
        {
            if (AllCommands != null)
            {
                return;
            }

            lock (SyncLock)
            {
                if (AllCommands != null)
                {
                    return;
                }

                var commandMap = new List<CliCommandModel>();
            
                var cliCommandTypes = typeof(ICliCommand)
                                      .Assembly.DefinedTypes
                                      .Where(x => x.IsClass && typeof(ICliCommand).IsAssignableFrom(x))
                                      .ToList();

                foreach (var commandType in cliCommandTypes)
                {
                    var attribute = commandType.GetCustomAttribute<CliCommandAttribute>();

                    if (attribute == null)
                    {
                        throw new DetailedLogException($"No `{nameof(CliCommandAttribute)} found for command type.`")
                        {
                            Context =
                            {
                                {"CommandType", commandType.Name }
                            }
                        };
                    }
                
                    commandMap.Add(new CliCommandModel
                    {
                        CommandName = attribute.CommandName,
                        CommandType = commandType,
                        IsDefault = attribute.IsDefault,
                        SkipSettingsLoading = attribute.SkipSettingsLoading
                    });
                }

                AllCommands = commandMap;
            }
        }
        
        public static (CliCommandModel, string[]) Parse(string[] args)
        {
            // The command name is the first argument.
            string commandName = args.FirstOrDefault();
            
            // Get the rest to pass to the executing command.
            string[] restArgs = args.Skip(1).ToArray();
            
            var commandsByName = AllCommands.ToDictionary(x => x.CommandName);
            
            if (commandName != null && commandsByName.ContainsKey(commandName))
            {
                return (commandsByName[commandName], restArgs);
            }

            // If the command name was not specified - use the only command marked as Default.
            var defaultCommand = AllCommands.Single(x => x.IsDefault);

            return (defaultCommand, restArgs);
        }
    }

    public class CliCommandModel
    {
        public Type CommandType { get; set; }

        public string CommandName { get; set; }

        public bool IsDefault { get; set; }

        public bool SkipSettingsLoading { get; set; }
    }
 
    [AttributeUsage(AttributeTargets.Class)]
    public class CliCommandAttribute : Attribute
    {
        public string CommandName { get; }

        public bool IsDefault { get; set; }
        
        public bool SkipSettingsLoading { get; set; }
        
        public CliCommandAttribute(string commandName)
        {
            this.CommandName = commandName;
        }
    }
    
    public interface ICliCommand
    {
        Task<int> Run(string[] args);
    }
}