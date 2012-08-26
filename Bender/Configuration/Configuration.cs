using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bender.Backend.Xmpp;
using Bender.Common;
using Bender.Module;

namespace Bender.Configuration
{
    /* 
     * ENHANCE: Strongly typed configuration
     * 
     * Modules should be able to declare a type to hold their own configuration
     * information and have it automatically populated and passed to them by
     * the framework.
     */
    internal class Configuration : IConfiguration
    {
        private static Regex configPattern = new Regex(@"^\s*(\S+)\s+(.+?)\s*$");

        private ModuleResolver moduleResolver;
        private IDictionary<string, string> configuration = new Dictionary<string, string>();
        private ISet<string> enabledModules;

        public string this[string key]
        {
            get
            {
                string val;
                this.configuration.TryGetValue(key, out val);

                return val;
            }
        }

        public string Name
        {
            get { return this[Constants.ConfigKey.Name]; }
        }

        public string Jid
        {
            get { return this[Constants.ConfigKey.XmppJid]; }
        }

        public string Password
        {
            get { return this[Constants.ConfigKey.XmppPassword]; }
        }

        public string ModulesDirectoryPath
        {
            get { return this[Constants.ConfigKey.ModulesDirectory]; }
        }

        public IEnumerable<string> Rooms
        {
            get
            {
                return Regex.Split(this[Constants.ConfigKey.XmppRooms], @"\s+");
            }
        }

        public IEnumerable<IModule> Modules
        {
            get
            {
                AssertModuleResolver();
                return moduleResolver.GetModules();
            }
        }

        public void Start(IBackend backend)
        {
            foreach (var module in Modules)
            {
                module.OnStart(this, backend);
            }
        }

        public void EnableModule(string moduleName, IBackend backend)
        {
            enabledModules.Add(moduleName);
            AssertModuleResolver();
            moduleResolver.FilterModules();
            Start(backend);
            //TODO: Write this change to the configuration file
        }

        public void DisableModule(string moduleName)
        {
            enabledModules.Remove(moduleName);
            AssertModuleResolver();
            moduleResolver.FilterModules();
            //TODO: Write this change to the configuration file
        }

        public Configuration(Stream stream)
        {
            var reader = new StreamReader(stream);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var match = configPattern.Match(line);
                if (match.Success)
                {
                    configuration[match.Groups[1].Value] = match.Groups[2].Value;
                }
                else
                {
                    // TODO: Log a warning
                }
            }

            enabledModules = new HashSet<string>(Regex.Split(this[Constants.ConfigKey.Modules], @"\s+"));
        }

        private void AssertModuleResolver()
        {
            if (moduleResolver == null)
            {
                DirectoryInfo modulesDirectory = null;
                if (!string.IsNullOrWhiteSpace(ModulesDirectoryPath))
                {
                    modulesDirectory = new DirectoryInfo(ModulesDirectoryPath);
                }
                moduleResolver = new ModuleResolver(enabledModules, modulesDirectory);
            }
        }
    }
}
