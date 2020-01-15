using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace NWayland.CodeGen
{
    public partial class WaylandProtocolGenerator
    {
        private readonly WaylandGeneratorHints _hints;
        private readonly Dictionary<string, string> _protocolFullNames = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _protocolNamespaces = new Dictionary<string, string>();
        public WaylandProtocolGenerator(IEnumerable<WaylandProtocolGroup> protocolGroups, WaylandGeneratorHints hints)
        {
            _hints = hints;
            foreach(var g in protocolGroups)
            foreach (var p in g.Protocols)
            {
                _protocolNamespaces.Add(p.Name, g.Namespace + "." + Pascalize(p.Name));
                foreach (var i in p.Interfaces)
                {
                    var fullName = ProtocolNamespace(p.Name) + "." + Pascalize(i.Name);
                    if (_protocolFullNames.ContainsKey(i.Name))
                        throw new ArgumentException(
                            $"Can't add {i.Name} from {p.Name}, {i.Name} is already associated to {_protocolFullNames[i.Name]}");
                    _protocolFullNames.Add(i.Name, fullName);

                }
            }
        }
        
        public string Generate(WaylandProtocol protocol)
        {
            
            var unit = CompilationUnit();
            unit = Generate(unit, protocol);
            var cw = new AdhocWorkspace();
            var formatted = Microsoft.CodeAnalysis.Formatting.Formatter.Format(unit, cw, cw.Options
                .WithChangedOption(CSharpFormattingOptions.NewLineForMembersInObjectInit, true)
                .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, true)
                .WithChangedOption(CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, true)
                .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true)
            
            );
            return formatted.ToFullString();
        }

        
        CompilationUnitSyntax Generate(CompilationUnitSyntax code, WaylandProtocol protocol)
        {
            code = code.AddUsings(UsingDirective(IdentifierName("System")))
                .AddUsings(UsingDirective(IdentifierName("System.Collections.Generic")))
                .AddUsings(UsingDirective(IdentifierName("System.Linq")))
                .AddUsings(UsingDirective(IdentifierName("System.Text")))
                .AddUsings(UsingDirective(IdentifierName("NWayland.Protocols.Wayland")))
                .AddUsings(UsingDirective(IdentifierName("NWayland.Interop")))
                .AddUsings(UsingDirective(IdentifierName("System.Threading.Tasks")));
            
            
            var ns = NamespaceDeclaration(ProtocolNamespaceSyntax(protocol.Name));

            foreach (var iface in protocol.Interfaces)
            {
                var cl = ClassDeclaration(Pascalize(iface.Name))
                    .WithModifiers(new SyntaxTokenList(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.SealedKeyword),
                        Token(SyntaxKind.UnsafeKeyword),
                        Token(SyntaxKind.PartialKeyword)))
                    .AddBaseListTypes(
                        SimpleBaseType(SyntaxFactory.ParseTypeName("NWayland.Protocols.Wayland.WlProxy")));
                cl = WithSummary(cl, iface.Description);
                cl = WithSignature(cl, iface);
                cl = WithRequests(cl, protocol, iface);
                cl = WithEvents(cl, protocol, iface);
                cl = WithEnums(cl, protocol, iface);
                cl = WithFactory(cl, iface)
                    .AddMembers(DeclareConstant("string", "InterfaceName", MakeLiteralExpression(iface.Name)))
                    .AddMembers(DeclareConstant("int", "InterfaceVersion", MakeLiteralExpression(iface.Version)));
                

                if (iface.Name != "wl_display")
                {
                    var ctor = ConstructorDeclaration(cl.Identifier)
                        .AddModifiers(Token(SyntaxKind.PublicKeyword))
                        .WithParameterList(ParameterList(
                            SeparatedList(new[]
                            {
                                Parameter(Identifier("handle")).WithType(ParseTypeName("IntPtr")),
                                Parameter(Identifier("version")).WithType(ParseTypeName("int")),
                                Parameter(Identifier("display"))
                                    .WithType(ParseTypeName("NWayland.Protocols.Wayland.WlDisplay")),
                            }))).WithBody(Block())
                        .WithInitializer(ConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                            ArgumentList(SeparatedList(new[]
                            {
                                Argument(IdentifierName("handle")),
                                Argument(IdentifierName("version")),
                                Argument(IdentifierName("display"))
                            }))));
                    cl = cl.AddMembers(ctor);
                }

                ns = ns.AddMembers(cl);
            }

            return code.AddMembers(ns);
        }


       
    }
}