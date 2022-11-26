using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NWayland.Scanner
{
    public partial class WaylandProtocolGenerator
    {
        public static string Pascalize(string name, bool camel = false)
        {
            var upperizeNext = !camel;
            var sb = new StringBuilder(name.Length);
            foreach (var och in name)
            {
                var ch = och;
                if (ch == '_')
                {
                    upperizeNext = true;
                }
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

        private string ProtocolNamespace(string protocol) => _protocolNamespaces[protocol];

        private NameSyntax ProtocolNamespaceSyntax(string protocol)
            => IdentifierName(ProtocolNamespace(protocol));

        private static T WithSummary<T>(T member, WaylandProtocolDescription? description) where T : MemberDeclarationSyntax
            => WithSummary(member, description?.Value);

        private static T WithSummary<T>(T member, string? description) where T : MemberDeclarationSyntax
        {
            if (string.IsNullOrWhiteSpace(description))
                return member;

            var nodes = description.Replace("\r", null)
                .Replace("\t", null)
                .Split("\n\n")
                .SelectMany(static paragraph =>
                {
                    var lines = paragraph.Split('\n')
                        .Select(static line => line.Trim())
                        .Where(static line => line != string.Empty)
                        .Select(XmlTextLiteral)
                        .ToArray();
                    return new[]
                    {
                        new XmlNodeSyntax[] { XmlText(lines) },
                        new XmlNodeSyntax[] { XmlEmptyElement("br"), XmlEmptyElement("br"), XmlText(XmlTextNewLine("\n")) }
                    };
                })
                .SelectMany(static x => x)
                .Prepend(XmlText(XmlTextNewLine("\n")));
            var summary = XmlElement("summary", List(nodes));
            return member.WithLeadingTrivia(TriviaList(Trivia(DocumentationComment(summary, XmlText("\n")))));
        }

        private static LiteralExpressionSyntax MakeLiteralExpression(string literal)
            => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(literal));

        private static LiteralExpressionSyntax MakeLiteralExpression(int literal)
            => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(literal));

        private static LiteralExpressionSyntax MakeNullLiteralExpression() =>
            LiteralExpression(SyntaxKind.NullLiteralExpression, Token(SyntaxKind.NullKeyword));

        private string GetWlInterfaceTypeName(string wlTypeName) => _protocolFullNames[wlTypeName];

        private RefExpressionSyntax GetWlInterfaceRefFor(string wlTypeName)
            => RefExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(GetWlInterfaceTypeName(wlTypeName)),
                    IdentifierName("WlInterface")));

        private InvocationExpressionSyntax GetWlInterfaceAddressFor(string wlTypeName)
            => InvocationExpression(MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("WlInterface"), IdentifierName("GeneratorAddressOf")),
                ArgumentList(SingletonSeparatedList(Argument(
                    GetWlInterfaceRefFor(wlTypeName)))));

        private static MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax expr, string identifier) =>
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expr, IdentifierName(identifier));

        private static SyntaxToken Semicolon() => Token(SyntaxKind.SemicolonToken);

        private static FieldDeclarationSyntax DeclareConstant(string type, string name, ExpressionSyntax value)
            => FieldDeclaration(
                    VariableDeclaration(ParseTypeName(type),
                        SingletonSeparatedList(
                            VariableDeclarator(name).WithInitializer(EqualsValueClause(value))
                        ))
                ).WithSemicolonToken(Semicolon())
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ConstKeyword)));

        private string? TryGetEnumTypeReference(string protocol, string @interface, string message, string arg, string? en)
        {
            en = _hints.FindEnumTypeNameOverride(protocol, @interface, message, arg) ?? en;
            if (en is null)
                return null;

            static string GetName(string n) => $"{Pascalize(n)}Enum";

            if (!en.Contains('.'))
                return GetName(en);
            var sp = en.Split(new[] {'.'}, 2);
            return $"{GetWlInterfaceTypeName(sp[0])}.{GetName(sp[1])}";
        }
    }
}
