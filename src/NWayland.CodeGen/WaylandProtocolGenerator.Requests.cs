using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace NWayland.CodeGen
{
    public partial class WaylandProtocolGenerator
    {
        MethodDeclarationSyntax CreateMethod(WaylandProtocol protocol, WaylandProtocolInterface iface,
            WaylandProtocolRequest request, int index)
        {
            var newIdArgument = request.Arguments?.FirstOrDefault(a => a.Type == WaylandArgumentTypes.NewId);
            if (newIdArgument != null && newIdArgument.Interface == null)
                return null;
            var ctorType = newIdArgument?.Interface;
            var dotNetCtorType = ctorType == null ? "void" : GetWlInterfaceTypeName(ctorType);
            
            var method = MethodDeclaration(
                ParseTypeName(dotNetCtorType), Pascalize(request.Name));

            var plist = new SeparatedSyntaxList<ParameterSyntax>();
            var arglist = new SeparatedSyntaxList<ExpressionSyntax>();
            var statements = new SeparatedSyntaxList<StatementSyntax>();
            var callStatements = new SeparatedSyntaxList<StatementSyntax>();
            var fixedDeclarations = new List<VariableDeclarationSyntax>();
            
            if (request.Since > 0)
            {
                statements = statements.Add(IfStatement(
                    BinaryExpression(SyntaxKind.LessThanExpression,
                        IdentifierName("Version"), MakeLiteralExpression(request.Since))
                    ,
                    request.Type == "destructor"
                        ? (StatementSyntax) ReturnStatement()
                        : ThrowStatement(ObjectCreationExpression(ParseTypeName("System.InvalidOperationException"))
                            .WithArgumentList(
                                ArgumentList(SingletonSeparatedList(Argument(MakeLiteralExpression(
                                    $"Request {request.Name} is only supported since version {request.Since}"))))))));
            }
            
            if (request.Arguments != null)

                foreach (var arg in request.Arguments ?? Array.Empty<WaylandProtocolArgument>())
                {
                    TypeSyntax parameterType = null;
                    var nullCheck = false;
                    var argName = "@" + Pascalize(arg.Name, true);
                    if (arg.Type == WaylandArgumentTypes.Int32 
                        || arg.Type == WaylandArgumentTypes.Fixed
                        || arg.Type == WaylandArgumentTypes.FileDescriptor)
                    {
                        parameterType = ParseTypeName("System.Int32");
                        arglist = arglist.Add(IdentifierName(argName));
                    }
                    else if (arg.Type == WaylandArgumentTypes.Uint32)
                    {
                        parameterType = ParseTypeName("System.UInt32");
                        arglist = arglist.Add(IdentifierName(argName));
                    }
                    else if (arg.Type == WaylandArgumentTypes.NewId)
                    {
                        arglist = arglist.Add(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("WlArgument"), IdentifierName("NewId")));
                    }
                    else if (arg.Type == WaylandArgumentTypes.String)
                    {
                        nullCheck = true;
                        parameterType = ParseTypeName("System.String");
                        var tempName = "__marshalled__" + argName.TrimStart('@');
                        var bufferType = ParseTypeName("NWayland.Interop.NWaylandMarshalledString");

                        statements = statements.Add(LocalDeclarationStatement(
                            new SyntaxTokenList(Token(SyntaxKind.UsingKeyword)),
                            VariableDeclaration(ParseTypeName("var"))
                                .WithVariables(SingletonSeparatedList(
                                    VariableDeclarator(tempName)
                                        .WithInitializer(EqualsValueClause(ObjectCreationExpression(bufferType)
                                            .WithArgumentList(
                                                ArgumentList(
                                                    SingletonSeparatedList(Argument(IdentifierName(argName)))))))
                                ))));
                        arglist = arglist.Add(IdentifierName(tempName));
                    }
                    else if (arg.Type == WaylandArgumentTypes.Object)
                    {
                        nullCheck = true;
                        parameterType = ParseTypeName(GetWlInterfaceTypeName(arg.Interface));
                        arglist = arglist.Add(IdentifierName(argName));
                    }
                    else if (arg.Type == WaylandArgumentTypes.Array)
                    {
                        if (arg.AllowNull)
                            throw new NotSupportedException(
                                "Wrapping nullable arrays is currently not supported");
                        var arrayElementType = _hints.GetTypeNameForArray(protocol.Name, iface.Name, request.Name, arg.Name); 
                        parameterType = ParseTypeName("ReadOnlySpan<" + arrayElementType + ">");
                        var pointerName = "__pointer__" + argName.TrimStart('@');
                        var tempName = "__marshalled__" + argName.TrimStart('@');
                        fixedDeclarations.Add(VariableDeclaration(ParseTypeName(arrayElementType + "*"),
                            SingletonSeparatedList(VariableDeclarator(pointerName)
                                .WithInitializer(EqualsValueClause(IdentifierName(argName))))));

                        callStatements = callStatements.Add(LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
                            .WithVariables(SingletonSeparatedList(VariableDeclarator(tempName)
                                .WithInitializer(EqualsValueClause(
                                    InvocationExpression(MemberAccess(ParseTypeName("NWayland.Interop.WlArray"),
                                        "FromPointer"), ArgumentList(SeparatedList(new[]
                                        {
                                            Argument(IdentifierName(pointerName)),
                                            Argument(MemberAccess(IdentifierName(argName), "Length"))
                                        }
                                    ))))

                                )))));

                        arglist = arglist.Add(PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                            IdentifierName(tempName)));

                    }

                    if (parameterType != null)
                    {
                        plist = plist.Add(Parameter(Identifier(argName)).WithType(parameterType));
                    }

                    if (nullCheck)
                        statements = statements.Insert(0, IfStatement(
                            BinaryExpression(SyntaxKind.EqualsExpression, IdentifierName(argName),
                                MakeNullLiteralExpression()),
                            ThrowStatement(ObjectCreationExpression(ParseTypeName("System.ArgumentNullException"))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(MakeLiteralExpression(argName.TrimStart('@')))))))));
                }

            callStatements = callStatements.Add(LocalDeclarationStatement(VariableDeclaration(ParseTypeName("WlArgument*"))
                .WithVariables(SingletonSeparatedList(VariableDeclarator("__args")
                    .WithInitializer(EqualsValueClause(StackAllocArrayCreationExpression(
                        ArrayType(ParseTypeName("WlArgument[]")),
                        InitializerExpression(SyntaxKind.ArrayInitializerExpression, arglist))))))));
            
            var marshalArgs = SeparatedList(new[]
            {
                Argument(MemberAccess( IdentifierName("this"), "Handle")),
                Argument(MakeLiteralExpression(index)),
                Argument(IdentifierName("__args"))
            });
            if (ctorType != null)
                marshalArgs = marshalArgs.Add(Argument(GetWlInterfaceRefFor(ctorType)));

            var callExpr = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("LibWayland"),
                    IdentifierName(ctorType == null
                        ? "wl_proxy_marshal_array"
                        : "wl_proxy_marshal_array_constructor")),
                ArgumentList(marshalArgs));

            
            
            if (ctorType == null)
                callStatements = callStatements.Add(ExpressionStatement(callExpr));
            else
            {
                callStatements = callStatements.Add(LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator("__ret").WithInitializer(EqualsValueClause(callExpr))))));
                callStatements = callStatements.Add(ReturnStatement(ConditionalExpression(BinaryExpression(
                        SyntaxKind.EqualsExpression, IdentifierName("__ret"),
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("IntPtr"),
                            IdentifierName("Zero"))),
                    MakeNullLiteralExpression(),
                    ObjectCreationExpression(ParseTypeName(dotNetCtorType)).WithArgumentList(
                        ArgumentList(SeparatedList(new[]
                        {
                            Argument(IdentifierName("__ret")),
                            Argument(IdentifierName("Version")),
                            Argument(IdentifierName("Display"))
                        }))))));
            }

            if (fixedDeclarations.Count == 0)
                statements = statements.AddRange(callStatements);
            else
            {
                var callBlock = (StatementSyntax) Block(callStatements);
                fixedDeclarations.Reverse();
                foreach (var fd in fixedDeclarations)
                    callBlock = FixedStatement(fd, callBlock);
                statements = statements.Add(callBlock);
            }

            method = WithSummary(method.WithParameterList(ParameterList(plist))
                    .WithBody(Block(statements))
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword))),
                request.Description);

            if (request.Type == "destructor")
            {
                method = method
                    .WithIdentifier(Identifier("CallWaylandDestructor"))
                    .WithModifiers(TokenList(
                        Token(SyntaxKind.ProtectedKeyword),
                        Token(SyntaxKind.SealedKeyword),
                        Token(SyntaxKind.OverrideKeyword)
                    ));
            }


            return method;
        }


        ClassDeclarationSyntax WithRequests(ClassDeclarationSyntax cl,
            WaylandProtocol protocol,
            WaylandProtocolInterface iface)
        {
            if (iface.Requests == null)
                return cl;
            for (var idx = 0; idx < iface.Requests.Length; idx++)
            {
                var method = CreateMethod(protocol, iface, iface.Requests[idx], idx);
                if (method != null)
                    cl = cl.AddMembers(method);
            }

            return cl;
        }
    }
}