using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.FileSystemGlobbing;

namespace NWayland.Scanner
{
    public static class Program
    {
        public static void Main()
        {
            AutoGen();
        }

        private static void AutoGen()
        {
            var root = Environment.ProcessPath;
            while (!File.Exists(Path.Combine(root, "NWayland.sln")))
            {
                root = Path.GetFullPath(Path.Combine(root, ".."));
                if (Path.GetPathRoot(root) == root)
                    throw new InvalidOperationException("Unable to find base directory");
            }

            string GetPath(params string[] elements) => Path.Combine(elements.Prepend(root).ToArray());

            IEnumerable<string> GlobPath(params string[] elements)
            {
                var subRoot = GetPath(elements.SkipLast(1).ToArray());
                var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
                matcher.AddInclude(elements.Last());
                return matcher.GetResultsInFullPath(subRoot);

            }

            var coreProtocols = new List<string> { GetPath("external", "wayland", "protocol", "wayland.xml") };
            coreProtocols.AddRange(GlobPath("external", "wayland-protocols", "**/*.xml"));
            var hints = ProtocolHintsConfiguration.GetGeneratorHints();

            WaylandProtocolGroup Group(string assembly, string ns, IEnumerable<string> paths)
                => new(assembly, ns)
                {
                    Protocols = paths.Select(static path =>
                            new XmlSerializer(typeof(WaylandProtocol)).Deserialize(
                                new StringReader(File.ReadAllText(path))) as WaylandProtocol)
                        .Where(p => p is not null && !hints.ProtocolBlacklist.Contains(p.Name)).ToList()!
                };

            var groups = new[]
            {
                Group("NWayland", "NWayland.Protocols", coreProtocols),
                Group("NWayland.Protocols.Plasma", "NWayland.Protocols.Plasma", GlobPath("external", "plasma-wayland-protocols", "src", "protocols", "**/*.xml")),
                Group("NWayland.Protocols.Wlr", "NWayland.Protocols.Wlr", GlobPath("external", "wlr-protocols", "**/*.xml"))
            };

            var gen = new WaylandProtocolGenerator(groups, hints);
            foreach (var g in groups)
            {
                var generatedDir = GetPath("src", g.Assembly, "Generated");
                if (Directory.Exists(generatedDir))
                    Directory.Delete(generatedDir, true);
                Directory.CreateDirectory(generatedDir!);
                foreach (var protocol in g.Protocols)
                {
                    var generated = gen.Generate(protocol);
                    File.WriteAllText(Path.Combine(generatedDir, $"{WaylandProtocolGenerator.Pascalize(protocol.Name)}.Generated.cs"), generated);
                }
            }
        }
    }
}
