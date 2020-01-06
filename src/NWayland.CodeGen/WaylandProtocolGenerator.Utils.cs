using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NWayland.CodeGen
{
    public static partial class WaylandProtocolGenerator
    {
        static string Pascalize(string name, bool camel = false)
        {
            var upperizeNext = !camel;
            var sb = new StringBuilder(name.Length);
            foreach (var och in name)
            {
                var ch = och;
                if (ch == '_')
                    upperizeNext = true;
                else
                {
                    if (upperizeNext)
                    {
                        ch = char.ToUpperInvariant(ch);
                        upperizeNext = false;
                    }

                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        static string ProtocolNamespace(string protocol) => $"NWayland.Protocols.{Pascalize(protocol)}";
        static NameSyntax ProtocolNamespaceSyntax(string protocol) => IdentifierName(ProtocolNamespace(protocol));
        
        static string FullInterfaceName(string protocol, string name) =>
            $"NWayland.Protocols.{Pascalize(protocol)}.{Pascalize(name)}";
        
        
        static T WithSummary<T>(this T member, WaylandProtocolDescription description) where T : MemberDeclarationSyntax
        {
            if (string.IsNullOrWhiteSpace(description?.Value))
                return member;
            
            var tokens = description.Value
                .Replace("\r", "")
                .Split('\n')
                .Select(line => XmlTextLiteral(line.TrimStart()))
                .SelectMany(l => new[] {l, XmlTextNewLine("\n")})
                .SkipLast(1);

            var summary = XmlElement("summary",
                SingletonList<XmlNodeSyntax>(XmlText(TokenList(tokens))));
            
            return member.WithLeadingTrivia(TriviaList(
                Trivia(DocumentationComment(summary, XmlText("\n")))));
        }

        static LiteralExpressionSyntax MakeLiteralExpression(string literal)
            => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(literal));
        
        static LiteralExpressionSyntax MakeLiteralExpression(int literal)
            => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(literal));

        static LiteralExpressionSyntax MakeNullLiteralExpression() => LiteralExpression(
            SyntaxKind.NullLiteralExpression,
            Token(SyntaxKind.NullKeyword));

        static RefExpressionSyntax GetWlInterfaceRefFor(string wlTypeName)
            => RefExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(Pascalize(wlTypeName)),
                    IdentifierName("WlInterface")));
        static InvocationExpressionSyntax GetWlInterfaceAddressFor(string wlTypeName)
        {
            return InvocationExpression(MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("WlInterface"), IdentifierName("GeneratorAddressOf")),
                ArgumentList(SingletonSeparatedList(Argument(
                    GetWlInterfaceRefFor(wlTypeName)))));
        }

        static MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax expr, string identifier) =>
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expr, IdentifierName(identifier));

        static SyntaxToken Semicolon() => Token(SyntaxKind.SemicolonToken);

        static FieldDeclarationSyntax DeclareConstant(string type, string name, LiteralExpressionSyntax value)
            => FieldDeclaration(
                    VariableDeclaration(ParseTypeName(type),
                        SingletonSeparatedList(
                            VariableDeclarator(name).WithInitializer(EqualsValueClause(value))
                        ))
                ).WithSemicolonToken(Semicolon())
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ConstKeyword)));
    }
}