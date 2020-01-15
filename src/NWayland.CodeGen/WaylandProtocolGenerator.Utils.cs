using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NWayland.CodeGen
{
    public partial class WaylandProtocolGenerator
    {
        public string Pascalize(string name, bool camel = false)
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

        string ProtocolNamespace(string protocol) => _protocolNamespaces[protocol];
        NameSyntax ProtocolNamespaceSyntax(string protocol) => IdentifierName(ProtocolNamespace(protocol));

        T WithSummary<T>(T member, WaylandProtocolDescription description) where T : MemberDeclarationSyntax
        {
            return WithSummary(member, description?.Value);
        }
        T WithSummary<T>(T member, string description) where T : MemberDeclarationSyntax
        {
            if (string.IsNullOrWhiteSpace(description))
                return member;
            
            var tokens = description
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

        LiteralExpressionSyntax MakeLiteralExpression(string literal)
            => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(literal));
        
        LiteralExpressionSyntax MakeLiteralExpression(int literal)
            => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(literal));

        LiteralExpressionSyntax MakeNullLiteralExpression() => LiteralExpression(
            SyntaxKind.NullLiteralExpression,
            Token(SyntaxKind.NullKeyword));

        string GetWlInterfaceTypeName(string wlTypeName) => _protocolFullNames[wlTypeName];
        
        RefExpressionSyntax GetWlInterfaceRefFor(string wlTypeName)
            => RefExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(GetWlInterfaceTypeName(wlTypeName)),
                    IdentifierName("WlInterface")));
        
        InvocationExpressionSyntax GetWlInterfaceAddressFor(string wlTypeName)
        {
            return InvocationExpression(MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("WlInterface"), IdentifierName("GeneratorAddressOf")),
                ArgumentList(SingletonSeparatedList(Argument(
                    GetWlInterfaceRefFor(wlTypeName)))));
        }

        MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax expr, string identifier) =>
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expr, IdentifierName(identifier));

        SyntaxToken Semicolon() => Token(SyntaxKind.SemicolonToken);

        FieldDeclarationSyntax DeclareConstant(string type, string name, LiteralExpressionSyntax value)
            => FieldDeclaration(
                    VariableDeclaration(ParseTypeName(type),
                        SingletonSeparatedList(
                            VariableDeclarator(name).WithInitializer(EqualsValueClause(value))
                        ))
                ).WithSemicolonToken(Semicolon())
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ConstKeyword)));

        public string TryGetEnumTypeReference(
            string protocol, string iface, string message, string arg, string en)
        {
            en = _hints.FindEnumTypeNameOverride(protocol, iface, message, arg) ?? en;
            if (en == null)
                return null;
            
            string GetName(string n) => Pascalize(n) + "Enum";

            if (!en.Contains("."))
                return GetName(en);
            var sp = en.Split(new[] {'.'}, 2);
            return GetWlInterfaceTypeName(sp[0]) + "." + GetName(sp[1]);
        }
    }
}