using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NWayland.CodeGen
{
    public class WaylandGeneratorHints
    {
        private static bool IsMatch(string what, string? pattern)
            => pattern is null || Regex.IsMatch(what, pattern);

        public class TypeNameHint
        {
            public TypeNameHint(string protocol, string @interface, string message, string argument, string typeName)
            {
                Protocol = protocol;
                Interface = @interface;
                Message = message;
                Argument = argument;
                TypeName = typeName;
            }

            public string Protocol { get; }
            public string Interface { get; }
            public string Message { get; }
            public string Argument { get; }
            public string TypeName { get; }

            public bool Match(string protocol, string @interface, string message, string argument)
                => IsMatch(Protocol, protocol)
                   && IsMatch(Interface, @interface)
                   && IsMatch(Message, message)
                   && IsMatch(Argument, argument);
        }

        public TypeNameHintCollection ArrayTypeNameHints { get; } = new();
        public TypeNameHintCollection EnumTypeNameHints { get; } = new();

        public List<string> ProtocolBlacklist { get; } = new();

        public class TypeNameHintCollection : List<TypeNameHint>
        {
            public void Add(string protocol, string @interface, string message, string arg, string typeName)
                => Add(new TypeNameHint(protocol, @interface, message, arg, typeName));
        }

        public string GetTypeNameForArray(string protocol, string @interface, string message, string argument)
        {
            var found = ArrayTypeNameHints.LastOrDefault(x => x.Match(protocol, @interface, message, argument))?.TypeName;
            if (found is not null)
                return found;
            Console.Error.WriteLine($"Unknown array type for {protocol}:{@interface}:{message}:{argument}");
            return "byte";
        }

        public string? FindEnumTypeNameOverride(string protocol, string @interface, string message, string argument)
            => EnumTypeNameHints.LastOrDefault(x => x.Match(protocol, @interface, message, argument))?.TypeName;
    }
}
