using System;

namespace dotnet_buildalyzer_test
{
    using Microsoft.Extensions.CommandLineUtils;
    using Buildalyzer;
    using Buildalyzer.Workspaces;
    using System.Threading.Tasks;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;
    using Microsoft.CodeAnalysis.Formatting;
    using System.Collections.Generic;

    static class SyntaxExtensions
    {
        public static string GetPropertyName(this PropertyDeclarationSyntax syntax)
        {
            return syntax.Identifier.Text;
        }
        public static string GetFieldName(this FieldDeclarationSyntax syntax)
        {
            return syntax.Declaration.Variables.First().Identifier.Text;
        }
        public static bool IsInnerClass(this ITypeSymbol sym)
        {
            return sym.ContainingType != null;
        }
        public static string GetQualifiedNamespace(this ITypeSymbol sym)
        {
            INamedTypeSymbol namedType = sym.ContainingType;
            INamespaceSymbol ns = sym.ContainingNamespace;
            var nslist = new List<string>();
            while (ns != null)
            {
                if (!string.IsNullOrEmpty(ns.Name))
                {
                    nslist.Add(ns.Name);
                }
                ns = ns.ContainingNamespace;
            }
            nslist.Reverse();
            return string.Join(".", nslist);
        }
    }

    class Program
    {
        static async Task ProcessCompilationResult(Project proj, Workspace ws)
        {
            Console.WriteLine($"processing project: {proj.FilePath}");
            var compilation = await proj.GetCompilationAsync();
            // from compiled assembly symbols
            var extCompilationRoot = CSharpSyntaxTree.ParseText($@"
            namespace BuildalyzerTest.Generated
            {{
                static class MemberwiseToStringExtensions
                {{
                }}
            }}
            ").GetRoot();
            foreach (var symbol in compilation.GetSymbolsWithName(x => true, SymbolFilter.Type).OfType<ITypeSymbol>())
            {
                if(symbol.IsInnerClass())
                {
                    continue;
                }
                var members = symbol.GetMembers()
                    .Where(x =>
                    {
                        if (x.Kind == SymbolKind.Field)
                        {
                            return ((IFieldSymbol)x).DeclaredAccessibility == Accessibility.Public;

                        }
                        else if (x.Kind == SymbolKind.Property)
                        {
                            var propsymbol = (IPropertySymbol)x;
                            return !propsymbol.IsWriteOnly && propsymbol.DeclaredAccessibility == Accessibility.Public;
                        }
                        else
                        {
                            return false;
                        }
                    }).ToArray();
                if (members.Length != 0)
                {
                    var method = CSharpSyntaxTree.ParseText($@"
public static string MemberwiseToString(this {symbol.GetQualifiedNamespace()}.{symbol.Name} obj)
{{
    return string.Format(""{string.Join(", ", Enumerable.Range(0, members.Length).Select(i => $"{{{i}}}"))}"",
        {string.Join(", ", members.Select(m => $"obj.{m.Name}"))});
}}").GetRoot();
                    var classDef = extCompilationRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
                        .First();
                    var newClassDef = classDef.AddMembers(method.DescendantNodes().OfType<MethodDeclarationSyntax>().First());
                    extCompilationRoot = extCompilationRoot.ReplaceNode(classDef, newClassDef);
                }
            }
            extCompilationRoot = Formatter.Format(extCompilationRoot, ws);
            Console.WriteLine($"result: {extCompilationRoot.GetText()}");
        }
        static async Task ProcessFromDocument(Project proj, Workspace ws)
        {
            foreach (var doc in proj.Documents)
            {
                Console.WriteLine($"{doc.FilePath}");
                var rootNode = await doc.GetSyntaxRootAsync();
                foreach (var classDecNode in rootNode.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    foreach (var ns in classDecNode.Ancestors().OfType<NamespaceDeclarationSyntax>())
                    {
                        Console.WriteLine($"ns name: {ns.Name}");
                    }
                    foreach (var member in classDecNode.Members.OfType<PropertyDeclarationSyntax>())
                    {
                        Console.WriteLine($"prop id: {member.GetPropertyName()}");
                    }
                    foreach (var member in classDecNode.Members.OfType<FieldDeclarationSyntax>())
                    {
                        Console.WriteLine($"field id: {member.GetFieldName()}");
                    }
                }
            }
        }
        static async Task GenerateToStringTask(string projectFilePath)
        {
            var manager = new AnalyzerManager();
            Console.WriteLine($"creating project analyzer({projectFilePath})");
            var analyzer = manager.GetProject(projectFilePath);
            var ws = analyzer.GetWorkspace();
            foreach (var proj in ws.CurrentSolution.Projects)
            {
                Console.WriteLine($"processing project: {proj.FilePath}");
                var compilation = await proj.GetCompilationAsync();
                // from compiled assembly symbols
                await ProcessCompilationResult(proj, ws);
                // from documents(source)
                await ProcessFromDocument(proj, ws);
            }
        }
        static CommandLineApplication CreateApp()
        {
            var app = new CommandLineApplication(false);
            var csprojpath = app.Option("-p|--project",
                "path to csproj",
                CommandOptionType.SingleValue);
            app.OnExecute(async () =>
            {
                if (csprojpath.HasValue())
                {
                    await GenerateToStringTask(csprojpath.Value());
                }
                else
                {
                    var fname = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.csproj").FirstOrDefault();
                    if (string.IsNullOrEmpty(fname))
                    {
                        Console.WriteLine($"no csproj found");
                        return -1;
                    }
                    else
                    {
                        await GenerateToStringTask(fname);
                    }
                }
                return 0;
            });
            app.HelpOption("-?|-h|--help");
            return app;
        }
        static void Main(string[] args)
        {
            var app = CreateApp();
            var retcode = app.Execute(args);
            if (retcode != 0)
            {
                Environment.ExitCode = retcode;
            }
        }
    }
}
