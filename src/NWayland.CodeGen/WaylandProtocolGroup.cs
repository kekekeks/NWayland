using System.Collections.Generic;

namespace NWayland.CodeGen
{
    public class WaylandProtocolGroup
    {
        public string Assembly { get; set; }
        public string Namespace { get; set; }
        public List<WaylandProtocol> Protocols { get; set; } = new();

        public WaylandProtocolGroup(string assembly, string ns)
        {
            Assembly = assembly;
            Namespace = ns;
        }
    }
}
