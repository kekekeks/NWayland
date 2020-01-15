using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NWayland.CodeGen
{
    public class WaylandGeneratorHints
    {
        static bool IsMatch(string what, string pattern)
        {
            if (pattern == null)
                return true;
            return Regex.IsMatch(what, pattern);
        }
        
        public class ArrayTypeNameHint
        {
            public string Protocol { get; set; }
            public string Interface { get; set; }
            public string Message { get; set; }
            public string Argument { get; set; }
            public string TypeName { get; set; }

            public bool Match(string protocol, string iface, string message, string argument)
            {
                return IsMatch(protocol, Protocol)
                       && IsMatch(Interface, iface)
                       && IsMatch(Message, message)
                       && IsMatch(Argument, argument);
            }
        }

        public ArrayTypeNameHintCollection ArrayTypeNameHints { get; } = new ArrayTypeNameHintCollection();

        public List<string> ProtocolBlacklist { get; set; } = new List<string>();
        
        public class ArrayTypeNameHintCollection : List<ArrayTypeNameHint>
        {
            public void Add(string protocol, string iface, string message, string arg, string typeName)
                => Add(new ArrayTypeNameHint
                    {Protocol = protocol, Interface = iface, Message = message, Argument = arg, TypeName = typeName});
        }
        
        public string GetTypeNameForArray(string protocol, string iface, string message, string argument)
        {
            return ArrayTypeNameHints.LastOrDefault(x => x.Match(protocol, iface, message, argument))?.TypeName ?? "byte";
        }
    }
}