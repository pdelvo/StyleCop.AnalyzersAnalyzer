/// <summary>
/// A tool to analyze the current status of the project
/// </summary>
namespace StyleCop.AnalyzersAnalyzer
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.MSBuild;
    using RazorEngine;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis.CodeFixes;

    class Program
    {
        static void Main(string[] args)
        {
            string path = args.Length > 0 ? args[0] : @"C:\StyleCopAnalyzers";

            string solutionFile = Path.Combine(path, "StyleCopAnalyzers.sln");



            if (File.Exists(solutionFile))
            {
                var workSpace = MSBuildWorkspace.Create();

                Solution solution = workSpace.OpenSolutionAsync(solutionFile).Result;

                Project project = solution.Projects.Single(x => x.Name == "StyleCop.Analyzers");

                var compilation = project.GetCompilationAsync().Result;

                compilation = compilation.WithOptions(compilation.Options.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

                MemoryStream memStream = new MemoryStream();

                var emitResult = compilation.Emit(memStream);

                var assembly = Assembly.Load(memStream.ToArray());

                var codeFixTypes = assembly.ExportedTypes.Where(x => x.FullName.EndsWith("CodeFixProvider"));
                var codeFixProviders = codeFixTypes.Select(t => Activator.CreateInstance(t, true)).OfType<CodeFixProvider>().Where(x => x != null).ToArray();
                
                var diagnostics = from diagnostic in compilation.SyntaxTrees.Select(x => DiagnosticInformation.Create(x, compilation, codeFixProviders))
                                  where diagnostic != null
                                  orderby diagnostic.Id
                                  group diagnostic by diagnostic.Type into g
                                  select new DiagnosticGroup { Type = g.Key, Diagnostics = g.ToList() };
                string result = Razor.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "HtmlGen.cshtml")), diagnostics);

                Console.WriteLine(result);
//                Console.WriteLine("<!-- Auto generated, do not change manually -->");
//                Console.WriteLine(@"
//<html>
//<head>
//<link href=""//maxcdn.bootstrapcdn.com/bootstrap/3.3.1/css/bootstrap.min.css"" rel=""stylesheet""/>
//<title>Status page</title>
//</head>
//<body style=""padding: 5em;"">");
//                Console.WriteLine("<h1>Current status (Last update \{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)} UTC)</h1>");
//                foreach (var item in diagnostics)
//                {
//                    Console.WriteLine("<h2>" + item.Type + "</h2>");
//                    Console.WriteLine();
//                    Console.WriteLine("<table class=\"table table-hover table-condensed\"><thead><tr><td>ID</td><td>Name</td><td>Implemented (\{item.Diagnostics.Count(x => x.Finished)}/\{item.Diagnostics.Count})</td><td>Tests (\{item.Diagnostics.Count(x => x.Enabled)}/\{item.Diagnostics.Count})</td></thead>");

//                    foreach (var diagnostic in item.Diagnostics)
//                    {
//                        string @class = diagnostic.Enabled ? "success" : diagnostic.Finished ? "warning" : "danger";
//                        Console.WriteLine("<tr class=\"\{@class}\">");
//                        Console.WriteLine("<td><a href=\"http://www.stylecop.com/docs/SA\{diagnostic.Id}.html\">SA\{diagnostic.Id}</a></td>");
//                        Console.WriteLine("<td>" + diagnostic.Name + "</td>");
//                        Console.WriteLine("<td>" + GetHtmlCheckbox(diagnostic.Finished) + "</td>");
//                        Console.WriteLine("<td>" + GetHtmlCheckbox(diagnostic.Enabled) + "</td>");
//                        Console.WriteLine("</tr>");
//                    }

//                    Console.WriteLine("</table>");
//                    Console.WriteLine();
//                }
//                Console.WriteLine("</body>");
            }
        }



        private static string GetHtmlCheckbox(bool finished)
        {
            return "<input type=\"checkbox\" " + (finished ? "checked=\"checked\"" : "") + "  disabled=\"disabled\" />";
        }
    }
}
