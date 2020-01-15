using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace NWayland.CodeGen
{
    public partial class WaylandProtocolGenerator
    {
        static ClassDeclarationSyntax WithFactory(this ClassDeclarationSyntax cl, WaylandProtocolInterface iface)
        {
            if (iface.Name == "wl_display" || iface.Name == "wl_registry")
                return cl;
            var factoryInterfaceType = ParseTypeName("IBindFactory<" + cl.Identifier.Text + ">");
            var fac = ClassDeclaration("ProxyFactory")
                .AddBaseListTypes(SimpleBaseType(factoryInterfaceType))
                .AddMembers(MethodDeclaration(
                        ParseTypeName("WlInterface*"), "GetInterface")
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithBody(Block(ReturnStatement(GetWlInterfaceAddressFor(iface.Name))))
                )
                .AddMembers(MethodDeclaration(
                        ParseTypeName(cl.Identifier.Text), "Create")
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithParameterList(ParameterList(SeparatedList(new[]
                    {
                        Parameter(Identifier("handle")).WithType(ParseTypeName("IntPtr")),
                        Parameter(Identifier("version")).WithType(ParseTypeName("int")),
                        Parameter(Identifier("display")).WithType(ParseTypeName("WlDisplay")),
                    })))
                    .WithBody(Block(ReturnStatement(
                        ObjectCreationExpression(ParseTypeName(cl.Identifier.Text))
                            .WithArgumentList(ArgumentList(SeparatedList(new[]
                            {
                                Argument(IdentifierName("handle")),
                                Argument(IdentifierName("version")),
                                Argument(IdentifierName("display"))
                            })))
                    )))
                );
            cl = cl
                .AddMembers(fac)
                .AddMembers(PropertyDeclaration(factoryInterfaceType, "BindFactory")
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                    .WithAccessorList(AccessorList(SingletonList(
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Semicolon()))))
                    .WithInitializer(EqualsValueClause(
                        ObjectCreationExpression(ParseTypeName("ProxyFactory"))
                            .WithArgumentList(ArgumentList())

                    )).WithSemicolonToken(Semicolon())
                );
            return cl;
        }
    }
}