using System;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace NWayland.CodeGen
{
    public partial class WaylandProtocolGenerator
    {
        static UInt32Converter UInt32Converter = new UInt32Converter();
        EnumDeclarationSyntax CreateEnum(WaylandProtocolEnum en)
        {
            var decl = EnumDeclaration(Pascalize(en.Name + "Enum"))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .AddBaseListTypes(SimpleBaseType(ParseTypeName("uint")));
            decl = WithSummary(decl, en.Description);
            if (en.IsBitField)
                decl = decl.AddAttributeLists(AttributeList(SingletonSeparatedList(
                    Attribute(
                        IdentifierName("System.Flags"))
                )));
            foreach (var entry in en.Entries)
            {
                var parsed = SyntaxFactory.ParseExpression(entry.Value);

                // Hack for enum members named like '270'
                var name = Pascalize(entry.Name);
                if (char.IsDigit(name[0]))
                    name = "k_" + name;
                                
                var member = EnumMemberDeclaration(name)
                    .WithEqualsValue(EqualsValueClause(parsed));
                member = WithSummary(member, entry.Summary);
                decl = decl.AddMembers(member);
            }
            

            return decl;
        }
        
        ClassDeclarationSyntax WithEnums(ClassDeclarationSyntax cl,
            WaylandProtocol protocol,
            WaylandProtocolInterface iface)
        {
            if (iface.Enums == null)
                return cl;
            foreach (var en in iface.Enums) 
                cl = cl.AddMembers(CreateEnum(en));

            return cl;
        }
    }
}