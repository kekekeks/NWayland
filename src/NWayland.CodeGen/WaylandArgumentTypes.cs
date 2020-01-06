using System;
using System.Collections.Generic;

namespace NWayland.CodeGen
{
    public class WaylandArgumentTypes
    {
        public const string Int32 = "int";
        public const string Uint32 = "uint";
        public const string Fixed = "fixed";
        public const string String = "string";
        public const string Object = "object";
        public const string NewId = "new_id";
        public const string Array = "array";
        public const string FileDescriptor = "fd";

        public static Dictionary<string, char> NamesToCodes = new Dictionary<string, char>
        {
            ["int"] = 'i',
            ["uint"] = 'u',
            ["fixed"] = 'f',
            ["string"] = 's',
            ["object"] = 'o',
            ["new_id"] = 'n',
            ["array"] = 'a',
            ["fd"] = 'h',
        };

    }
}