using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NWayland.CodeGen
{
    public partial class WaylandProtocolGenerator
    {
        private InvocationExpressionSyntax GenerateWlMessage(WaylandProtocolMessage msg)
        {
            var signature = new StringBuilder();
            if (msg.Since != 0)
                signature.Append(msg.Since);
            var interfaceList = new SeparatedSyntaxList<ExpressionSyntax>();
            if (msg.Arguments is not null)
                foreach (var arg in msg.Arguments)
                {
                    if (arg.AllowNull)
                        signature.Append('?');
                    if (arg.Type == WaylandArgumentTypes.NewId && arg.Interface is null)
                        signature.Append("su");
                    signature.Append(WaylandArgumentTypes.NamesToCodes[arg.Type]);
                    if (!string.IsNullOrWhiteSpace(arg.Interface))
                        interfaceList = interfaceList.Add(
                            GetWlInterfaceAddressFor(arg.Interface));
                    else
                        interfaceList = interfaceList.Add(MakeNullLiteralExpression());
                }

            var argList = ArgumentList(SeparatedList(new[]
            {
                Argument(MakeLiteralExpression(msg.Name)),
                Argument(MakeLiteralExpression(signature.ToString())),
                Argument(ArrayCreationExpression(ArrayType(ParseTypeName("WlInterface*[]")))
                    .WithInitializer(InitializerExpression(SyntaxKind.ArrayInitializerExpression,
                        interfaceList)))

            }));

            return InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("WlMessage"), IdentifierName("Create")),
                argList
            ).WithLeadingTrivia(CarriageReturn);
        }

        private ArgumentSyntax GenerateWlMessageList(in WaylandProtocolMessage[] messages)
        {
            var elements = new SeparatedSyntaxList<ExpressionSyntax>();
            foreach (var msg in messages)
                elements = elements.Add(GenerateWlMessage(msg));
            return Argument(ArrayCreationExpression(ArrayType(ParseTypeName("WlMessage[]")), InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                elements)));
        }

        private ClassDeclarationSyntax WithSignature(ClassDeclarationSyntax cl, WaylandProtocolInterface @interface)
        {
            var attr = AttributeList(SingletonSeparatedList(
                Attribute(
                    IdentifierName("System.Runtime.CompilerServices.FixedAddressValueType"))
            )).NormalizeWhitespace();
            var sigField = FieldDeclaration(new SyntaxList<AttributeListSyntax>(
                    new[] {attr}),
                new SyntaxTokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)),
                VariableDeclaration(ParseTypeName("NWayland.Interop.WlInterface"))
                    .AddVariables(VariableDeclarator("WlInterface")));
            cl = cl.AddMembers(sigField);

            var staticCtor = ConstructorDeclaration(cl.Identifier)
                .AddModifiers(Token(SyntaxKind.StaticKeyword));

            staticCtor = staticCtor.AddBodyStatements(ExpressionStatement(InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("WlInterface"), IdentifierName("Init")),

                ArgumentList(SeparatedList(new[]
                    {
                        Argument(MakeLiteralExpression(@interface.Name)),
                        Argument(MakeLiteralExpression(@interface.Version)),
                        GenerateWlMessageList(@interface.Requests?.Cast<WaylandProtocolMessage>().ToArray() ?? Array.Empty<WaylandProtocolMessage>()),
                        GenerateWlMessageList(@interface.Events ?? Array.Empty<WaylandProtocolMessage>())
                    }
                )))));

            cl = cl.AddMembers(staticCtor);

            cl = cl.AddMembers(MethodDeclaration(ParseTypeName("WlInterface*"), "GetWlInterface")
                .WithModifiers(TokenList(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword)))
                .WithBody(Block(ReturnStatement(GetWlInterfaceAddressFor(@interface.Name)))));

            return cl;
        }
    }
}
