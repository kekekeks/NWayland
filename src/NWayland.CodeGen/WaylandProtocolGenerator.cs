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
    public static partial class WaylandProtocolGenerator
    {
        public static string Generate(string xmlText)
        {
            var protocol =
                (WaylandProtocol) new XmlSerializer(typeof(WaylandProtocol)).Deserialize(new StringReader(xmlText));
            return Generate(protocol);
        }
        
        public static string Generate(WaylandProtocol protocol)
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

        
        static CompilationUnitSyntax Generate(CompilationUnitSyntax code, WaylandProtocol protocol)
        {
            code = code.AddUsings(UsingDirective(IdentifierName("System")))
                .AddUsings(UsingDirective(IdentifierName("System.Collections.Generic")))
                .AddUsings(UsingDirective(IdentifierName("System.Linq")))
                .AddUsings(UsingDirective(IdentifierName("System.Text")))
                .AddUsings(UsingDirective(IdentifierName("NWayland.Protocols.Wayland")))
                .AddUsings(UsingDirective(IdentifierName("NWayland.Core")))
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
                        SimpleBaseType(SyntaxFactory.ParseTypeName("NWayland.Core.WlProxy")))
                    .WithSummary(iface.Description)
                    .WithSignature(iface)
                    .WithRequests(iface)
                    .WithEvents(iface)
                    .WithFactory(iface)
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
                                Parameter(Identifier("display"))
                                    .WithType(ParseTypeName("NWayland.Protocols.Wayland.WlDisplay")),
                            }))).WithBody(Block())
                        .WithInitializer(ConstructorInitializer(SyntaxKind.BaseConstructorInitializer,
                            ArgumentList(SeparatedList(new[]
                            {
                                Argument(IdentifierName("handle")),
                                Argument(IdentifierName("display")),
                            }))));
                    cl = cl.AddMembers(ctor);
                }

                ns = ns.AddMembers(cl);
            }

            return code.AddMembers(ns);
        }


       
    }
}