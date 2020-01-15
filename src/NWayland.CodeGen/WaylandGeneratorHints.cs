using System;
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
        
        public class TypeNameHint
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
        
        public TypeNameHintCollection ArrayTypeNameHints { get; } = new TypeNameHintCollection();
        public TypeNameHintCollection EnumTypeNameHints { get; } = new TypeNameHintCollection();

        public List<string> ProtocolBlacklist { get; set; } = new List<string>();
        
        public class TypeNameHintCollection : List<TypeNameHint>
        {
            public void Add(string protocol, string iface, string message, string arg, string typeName)
                => Add(new TypeNameHint
                    {Protocol = protocol, Interface = iface, Message = message, Argument = arg, TypeName = typeName});
        }
        
        public string GetTypeNameForArray(string protocol, string iface, string message, string argument)
        {
            var found = ArrayTypeNameHints.LastOrDefault(x => x.Match(protocol, iface, message, argument))?.TypeName;
            if (found != null)
                return found;
            Console.Error.WriteLine($"Unknown array type for {protocol}:{iface}:{message}:{argument}");
            return "byte";
        }
        
        public string FindEnumTypeNameOverride(string protocol, string iface, string message, string argument) 
            => EnumTypeNameHints.LastOrDefault(x => x.Match(protocol, iface, message, argument))?.TypeName;
    }
}