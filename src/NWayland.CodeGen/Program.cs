using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

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

            var generatedDir = Path.Combine(root, "src", "NWayland", "Generated");
            if(Directory.Exists(generatedDir))
                Directory.Delete(generatedDir, true);
            Directory.CreateDirectory(generatedDir);

            var protocols = new List<string>();
            protocols.Add(Path.Combine(root, "external", "wayland", "protocol", "wayland.xml"));

            foreach (var protocolPath in protocols)
            {
                var protocol =
                    (WaylandProtocol) new XmlSerializer(typeof(WaylandProtocol)).Deserialize(new StringReader(File.ReadAllText(protocolPath)));
                var generated = WaylandProtocolGenerator.Generate(protocol);
                File.WriteAllText(Path.Combine(generatedDir, protocol.Name + ".generated.cs"), generated);
            }
        }
        
    }
}