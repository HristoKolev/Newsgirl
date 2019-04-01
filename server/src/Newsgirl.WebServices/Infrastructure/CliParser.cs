namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    /// <summary>
    /// Top level cli argument parser.
    /// Returns cli option and the reset of the arguments.
    /// The default option is `WebServer`. 
    /// </summary>
    public static class CliParser
    {
        private static List<CliCommandModel> CommandMap; 
        
        private static readonly object SyncLock = new object();
        
        public static void Scan()
        {
            if (CommandMap != null)
            {
                return;
            }

            lock (SyncLock)
            {
                if (CommandMap != null)
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

                CommandMap = commandMap;
            }
        }
        
        public static (CliCommandModel, string[]) Parse(string[] args)
        {
            string firstArg = args.FirstOrDefault();

            string[] restArgs = args.Skip(1).ToArray();

            var pairsByType = CommandMap.ToDictionary(x => x.CommandName);
            
            if (firstArg != null && pairsByType.ContainsKey(firstArg))
            {
                var model = pairsByType[firstArg];

                return (model, restArgs);
            }

            var defaultCommand = CommandMap.Single(x => x.IsDefault);

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