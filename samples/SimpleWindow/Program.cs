using System;
using System.Collections.Generic;
using System.Linq;
using NWayland.Core;
using NWayland.Protocols.Wayland;

namespace SimpleWindow
{
    unsafe class Program
    {
        static void Main(string[] args)
        {
            var display = WlDisplay.Connect(null);
            var registry = display.GetRegistry();
            var registryHandler = new RegistryHandler(registry);
            registry.Events = registryHandler;
            display.Dispatch();
            display.Roundtrip();
            var globals = registryHandler.GetGlobals();

            var compositor = registryHandler.Bind(WlCompositor.BindFactory, WlCompositor.InterfaceName,
                WlCompositor.InterfaceVersion);
            
            display.Roundtrip();
            var shell = registryHandler.Bind(WlShell.BindFactory, WlShell.InterfaceName, WlShell.InterfaceVersion);
            
            display.Roundtrip();
            var surface = compositor.CreateSurface();
            var shellSurface = shell.GetShellSurface(surface);
            shellSurface.SetTitle("Test");
            surface.Commit();
            display.Dispatch();
            display.Roundtrip();
        }
    }

    public class GlobalInfo
    {
        public uint Name { get; }
        public string Interface { get; }
        public uint Version { get; }

        public GlobalInfo(uint name, string @interface, uint version)
        {
            Name = name;
            Interface = @interface;
            Version = version;
        }

        public override string ToString() => $"{Interface} version {Version} at {Name}";
    }
    
    internal class RegistryHandler : WlRegistry.IEvents
    {
        private readonly WlRegistry _registry;

        public RegistryHandler(WlRegistry registry)
        {
            _registry = registry;
        }
        private Dictionary<uint, GlobalInfo> _globals { get; } = new Dictionary<uint, GlobalInfo>();
        public List<GlobalInfo> GetGlobals() => _globals.Values.ToList();
        public void OnGlobal(WlRegistry eventSender, uint name, string @interface, uint version)
        {
            _globals[name] = new GlobalInfo(name, @interface, version);
        }

        public void OnGlobalRemove(WlRegistry eventSender, uint name)
        {
            _globals.Remove(name);
        }

        public T Bind<T>(IBindFactory<T> factory, string iface, int minimumVersion) where T : WlProxy
        {
            var glob = GetGlobals().Where(g => g.Interface == iface && g.Version >= minimumVersion)
                .OrderBy(x => x.Version).FirstOrDefault();
            if (glob == null)
                throw new KeyNotFoundException($"Unable to find {iface} with version at least of {minimumVersion}");
            return _registry.Bind(glob.Name, factory);
        }
    }
}