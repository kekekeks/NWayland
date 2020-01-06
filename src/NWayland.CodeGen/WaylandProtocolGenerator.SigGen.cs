using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace NWayland.CodeGen
{
    public static partial class WaylandProtocolGenerator
    {
        static InvocationExpressionSyntax GenerateWlMessage(WaylandProtocolMessage msg)
        {
            var argList = new SeparatedSyntaxList<ArgumentSyntax>();
            var signature = new StringBuilder();
            if (msg.Arguments != null)
            {
                foreach (var arg in msg.Arguments)
                {
                    if (arg.AllowNull)
                        signature.Append('?');
                    signature.Append(WaylandArgumentTypes.NamesToCodes[arg.Type]);
                    if (!string.IsNullOrWhiteSpace(arg.Interface))
                        argList = argList.Add(Argument(
                            GetWlInterfaceAddressFor(arg.Interface)));
                    else
                        argList = argList.Add(Argument(MakeNullLiteralExpression()));
                }
            }

            argList = argList.Insert(0, Argument(MakeLiteralExpression(signature.ToString())));
            argList = argList.Insert(0, Argument(MakeLiteralExpression(msg.Name)));

            return InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("WlMessage"), IdentifierName("Create")),
                ArgumentList(argList)
            ).WithLeadingTrivia(SyntaxFactory.CarriageReturn);
        }

        static ArgumentSyntax GenerateWlMessageList(in WaylandProtocolMessage[] messages)
        {
            var elements = new SeparatedSyntaxList<ExpressionSyntax>();
            foreach (var msg in messages)
                elements = elements.Add(GenerateWlMessage(msg));
            return Argument(ArrayCreationExpression(ArrayType(ParseTypeName("WlMessage[]")), InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                elements)));
        }
        
        
        static ClassDeclarationSyntax WithSignature(this ClassDeclarationSyntax cl, WaylandProtocolInterface iface)
        {
            var attr = AttributeList(SingletonSeparatedList(
                Attribute(
                    IdentifierName("System.Runtime.CompilerServices.FixedAddressValueType"))
            )).NormalizeWhitespace();
            var sigField = FieldDeclaration(new SyntaxList<AttributeListSyntax>(
                    new[] {attr}),
                new SyntaxTokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)),
                VariableDeclaration(ParseTypeName("NWayland.Core.WlInterface"))
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