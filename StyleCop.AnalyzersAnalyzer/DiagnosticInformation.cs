using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Text.RegularExpressions;

namespace StyleCop.AnalyzersAnalyzer
{
    public class DiagnosticInformation
    {
        public string Id { get; }
        public string Name { get; }
        public string Type { get; }
        public bool Finished { get; }
        public bool Enabled { get; }

        private DiagnosticInformation(string id, string name, string type, bool finished, bool enabled)
        {
            Id = id;
            Name = name;
            Type = type;
            Finished = finished;
            Enabled = enabled;
        }

        public static DiagnosticInformation Create(SyntaxTree tree, Compilation compilation)
        {
            Regex diagnosticPathRegex = new Regex(@"(?<type>[A-Za-z]+)Rules\\SA(?<id>[0-9]{4})(?<name>[A-Za-z]+)\.cs$");
            var match = diagnosticPathRegex.Match(tree.FilePath);
            if (!match.Success)
            {
                return null;
            }
            string id = match.Groups["id"].Value;
            string name = match.Groups["name"].Value;
            string type = match.Groups["type"].Value;
            bool finished = true;
            bool enabled = true;
            var diagnosticAnalyzer = compilation.GetTypeByMetadataName(typeof(DiagnosticAnalyzer).FullName);
            var semanticModel = compilation.GetSemanticModel(tree);
            var syntaxRoot = tree.GetRoot();



            // Check if diagnostic is implemented
            foreach (var trivia in syntaxRoot.DescendantTrivia())
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                {
                    if (trivia.ToFullString().Contains("TODO: Implement analysis"))
                    {
                        finished = false;
                    }
                }
            }

            // Check if it is disabled
            foreach (var node in syntaxRoot.DescendantNodes())
            {
                if (node.IsKind(SyntaxKind.ClassDeclaration))
                {
                    var typeInfo = semanticModel.GetDeclaredSymbol(node as ClassDeclarationSyntax);

                    if(typeInfo != null)
                    {
                        if (typeInfo.BaseType != diagnosticAnalyzer)
                        {
                            return null;
                        }
                    }
                }
                if (node.IsKind(SyntaxKind.VariableDeclarator))
                {
                    VariableDeclaratorSyntax syntax = node as VariableDeclaratorSyntax;

                    if(syntax != null)
                    {
                        if(syntax.Identifier.Text == "Descriptor")
                        {
                            EqualsValueClauseSyntax equalValueClauseSyntax = syntax.Initializer as EqualsValueClauseSyntax;
                            if(equalValueClauseSyntax != null)
                            {
                                var objectCreationSyntax = equalValueClauseSyntax.Value as ObjectCreationExpressionSyntax;

                                var diagnosticDescriptor = compilation.GetTypeByMetadataName(typeof(DiagnosticDescriptor).FullName);

                                var symbolInfo = semanticModel.GetSymbolInfo(objectCreationSyntax);

                                var symbol = symbolInfo.Symbol;

                                if(symbol?.ContainingType == diagnosticDescriptor)
                                {

                                    if (objectCreationSyntax.ArgumentList != null)
                                    {
                                        var arguments = objectCreationSyntax.ArgumentList.Arguments;
                                        var possibleArgument = arguments[5] as ArgumentSyntax;
                                        if(possibleArgument != null)
                                        {
                                            if(possibleArgument.NameColon == null)
                                            {
                                                var memberAccessExpression = possibleArgument.Expression as MemberAccessExpressionSyntax;
                                                if(memberAccessExpression != null)
                                                {
                                                    var identifierName = memberAccessExpression.Name.Identifier.ToString();
                                                    enabled = identifierName != "DisabledNoTests";
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                System.Console.WriteLine("Not supported");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            DiagnosticInformation info = new DiagnosticInformation(id, name, type, finished, enabled);

            return info;
        }
    }
    public class DiagnosticGroup
    {
        public string Type { get; set; }
        public List<DiagnosticInformation> Diagnostics { get; set; }
    }
}