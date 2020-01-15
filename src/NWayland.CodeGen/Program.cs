using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GlobExpressions;

namespace NWayland.CodeGen
{
    class Program
    {
        static void Main(string[] args)
        {
            AutoGen();
            
        }


        static void AutoGen()
        {
            var root = typeof(Program).Assembly.GetModules()[0].FullyQualifiedName;
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
                var glob = Glob.Files(subRoot, elements.Last());
                return glob.Select(x => Path.Combine(subRoot, x));

            }
            var generatedDir = GetPath("src", "NWayland", "Generated");
            if(Directory.Exists(generatedDir))
                Directory.Delete(generatedDir, true);
            Directory.CreateDirectory(generatedDir);

            
            
            var protocols = new List<string>();
            protocols.Add(GetPath("external", "wayland", "protocol", "wayland.xml"));
            protocols.AddRange(GlobPath("external", "wayland-protocols", "**/*.xml"));
            protocols.AddRange(GlobPath("external", "kwayland", "src", "client", "protocols", "**/*.xml"));
            var hints = ProtocolHintsConfiguration.GetGeneratorHints();
            var allProtocols = protocols.Select(path =>
                    (WaylandProtocol) new XmlSerializer(typeof(WaylandProtocol)).Deserialize(
                        new StringReader(File.ReadAllText(path))))
                .Where(p => !hints.ProtocolBlacklist.Contains(p.Name)).ToList();
                
            
            var gen = new WaylandProtocolGenerator(allProtocols, hints);
            
            foreach (var protocol in allProtocols)
            {
                var generated = gen.Generate(protocol);
                File.WriteAllText(Path.Combine(generatedDir, protocol.Name + ".generated.cs"), generated);
            }
        }
        
    }
}