using System.Collections.Generic;

namespace NWayland.Scanner
{
    public class WaylandProtocolGroup
    {
        public string Assembly { get; }
        public string Namespace { get; }
        public List<WaylandProtocol> Protocols { get; set; } = new();

        public WaylandProtocolGroup(string assembly, string ns)
        {
            Assembly = assembly;
            Namespace = ns;
        }
    }
}
