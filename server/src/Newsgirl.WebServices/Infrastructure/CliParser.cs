namespace Newsgirl.WebServices.Infrastructure
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Top level cli argument parser.
    /// Returns cli option and the reset of the arguments.
    /// The default option is `WebServer`. 
    /// </summary>
    public static class CliParser
    {
        private static readonly Dictionary<string, CliOption> CliMap = new Dictionary<string, CliOption>
        {
            {"api-call", CliOption.ApiCall}
        };

        public static (CliOption, string[]) Parse(string[] args)
        {
            string firstArg = args.FirstOrDefault();

            string[] restArgs = args.Skip(1).ToArray();
            
            if (firstArg != null && CliMap.ContainsKey(firstArg))
            {
                return (CliMap[firstArg], restArgs);    
            }

            return (CliOption.WebServer, restArgs);
        }
    }
    
    public enum CliOption
    {
        WebServer,
        ApiCall,
    }
}