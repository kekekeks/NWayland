using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace NWayland.CodeGen
{
    public partial class WaylandProtocolGenerator
    {
        InvocationExpressionSyntax GenerateWlMessage(WaylandProtocolMessage msg)
        {
            var signature = new StringBuilder();
            if (msg.Since != 0)
                signature.Append(msg.Since);
            var ifaceList = new SeparatedSyntaxList<ExpressionSyntax>();
            if (msg.Arguments != null)
            {
                foreach (var arg in msg.Arguments)
                {
                    if (arg.AllowNull)
                        signature.Append('?');
                    if (arg.Type == WaylandArgumentTypes.NewId && arg.Interface == null)
                        signature.Append("su");
                    signature.Append(WaylandArgumentTypes.NamesToCodes[arg.Type]);
                    if (!string.IsNullOrWhiteSpace(arg.Interface))
                        ifaceList = ifaceList.Add(
                            GetWlInterfaceAddressFor(arg.Interface));
                    else
                        ifaceList = ifaceList.Add(MakeNullLiteralExpression());
                }
            }

            var argList = ArgumentList(SeparatedList(new[]
            {
                Argument(MakeLiteralExpression(msg.Name)),
                Argument(MakeLiteralExpression(signature.ToString())),
                Argument(ArrayCreationExpression(ArrayType(ParseTypeName("WlInterface*[]")))
                    .WithInitializer(InitializerExpression(SyntaxKind.ArrayInitializerExpression,
                        ifaceList)))

            }));

            return InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("WlMessage"), IdentifierName("Create")),
                argList
            ).WithLeadingTrivia(SyntaxFactory.CarriageReturn);
        }

        ArgumentSyntax GenerateWlMessageList(in WaylandProtocolMessage[] messages)
        {
            var elements = new SeparatedSyntaxList<ExpressionSyntax>();
            foreach (var msg in messages)
                elements = elements.Add(GenerateWlMessage(msg));
            return Argument(ArrayCreationExpression(ArrayType(ParseTypeName("WlMessage[]")), InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                elements)));
        }
        
        
        ClassDeclarationSyntax WithSignature(ClassDeclarationSyntax cl, WaylandProtocolInterface iface)
        {
            var attr = AttributeList(SingletonSeparatedList(
                Attribute(
                    IdentifierName("System.Runtime.CompilerServices.FixedAddressValueType"))
            )).NormalizeWhitespace();
            var sigField = FieldDeclaration(new SyntaxList<AttributeListSyntax>(
                    new[] {attr}),
                new SyntaxTokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)),
                VariableDeclaration(ParseTypeName("NWayland.Interop.WlInterface"))
                    .AddVariables(SyntaxFactory.VariableDeclarator("WlInterface")));
            cl = cl.AddMembers(sigField);

            var staticCtor = ConstructorDeclaration(cl.Identifier)
                .AddModifiers(Token(SyntaxKind.StaticKeyword));

            staticCtor = staticCtor.AddBodyStatements(ExpressionStatement(InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("WlInterface"), IdentifierName("Init")),

                ArgumentList(SeparatedList(new[]
                    {
                        Argument(MakeLiteralExpression(iface.Name)),
                        Argument(MakeLiteralExpression(iface.Version)),
                        GenerateWlMessageList(iface.Requests ?? new WaylandProtocolMessage[0]),
                        GenerateWlMessageList(iface.Events ?? new WaylandProtocolMessage[0])
                    }
                )))));
            

            cl = cl.AddMembers(staticCtor);

            cl = cl.AddMembers(MethodDeclaration(ParseTypeName("WlInterface*"), "GetWlInterface")
                .WithModifiers(TokenList(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword)))
                .WithBody(Block(ReturnStatement(GetWlInterfaceAddressFor(iface.Name)))));
            
            return cl;
        }
    }
}