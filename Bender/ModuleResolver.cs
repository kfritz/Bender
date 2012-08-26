using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Bender.Module;

namespace Bender
{
    internal class ModuleResolver
    {
#pragma warning disable 0649
        [ImportMany(typeof(IModule), AllowRecomposition = true)]
        private IEnumerable<IModule> importedModules;
#pragma warning restore 0649
        private List<IModule> loadedModules;
        private IReadOnlyList<IModule> readOnlyModules;
        private IEnumerable<string> moduleNames;
        private FileSystemWatcher watcher;
        private DirectoryCatalog dirCatalog;
        private CompositionContainer container;

        public ModuleResolver(string moduleName, DirectoryInfo directory = null)
            : this(Enumerable.Repeat(moduleName, 1), directory)
        {
        }

        public ModuleResolver(IEnumerable<string> moduleNames, DirectoryInfo directory = null)
        {
            var assCatalog = new AssemblyCatalog(typeof(Program).Assembly);
            var aggCatalog = new AggregateCatalog();
            aggCatalog.Catalogs.Add(assCatalog);
            if (directory != null)
            {
                WatchForAssemblies(directory, aggCatalog);
            }
            container = new CompositionContainer(aggCatalog);
            container.ComposeParts(this);

            this.moduleNames = moduleNames;
            loadedModules = new List<IModule>();
            FilterModules();
            readOnlyModules = loadedModules.AsReadOnly();
        }

        public IEnumerable<IModule> GetModules()
        {
            return readOnlyModules;
        }
        
        private void WatchForAssemblies(DirectoryInfo directory, AggregateCatalog aggCatalog)
        {
            dirCatalog = new DirectoryCatalog(directory.FullName);
            aggCatalog.Catalogs.Add(dirCatalog);

            watcher = new FileSystemWatcher(directory.FullName, "*.dll");
            watcher.Created += OnAssemblyChanged;
            watcher.Changed += OnAssemblyChanged;
            watcher.Deleted += OnAssemblyChanged;
            watcher.EnableRaisingEvents = true;
        }

        private void OnAssemblyChanged(object sender, FileSystemEventArgs e)
        {
            dirCatalog.Refresh();
            FilterModules();
        }

        public void FilterModules()
        {
            foreach (var m in importedModules)
            {
                if (moduleNames.Contains(m.GetType().Name) && !loadedModules.Contains(m))
                {
                    loadedModules.Add(m);
                }
            }
        }
    }
}
