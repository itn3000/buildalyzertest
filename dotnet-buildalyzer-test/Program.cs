using System;

namespace dotnet_buildalyzer_test
{
    using Microsoft.Extensions.CommandLineUtils;
    using Buildalyzer;
    using Buildalyzer.Workspaces;
    using System.Threading.Tasks;
    using System.IO;
    using System.Linq;
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
            // foreach(var node in syntax.DescendantNodes())
            // {
            // }
            // return "";
        }
        public static string GetFieldName(this FieldDeclarationSyntax syntax)
        {
            return syntax.Declaration.Variables.First().Identifier.Text;
            // foreach(var node in syntax.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            // {
            //     return node.Identifier.Text;
            //     // Console.WriteLine($"{node.Kind()},{node.GetText()}");
            // }
            // return "";
        }
    }

    class Program
    {
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
                foreach (var symbol in compilation.GetSymbolsWithName(x => true, SymbolFilter.Type))
                {
                    if (symbol is ITypeSymbol ts)
                    {
                        Console.WriteLine($"name={ts.Name}");
                        var nssymbol = ts.ContainingNamespace;
                        if (nssymbol != null)
                        {
                            Console.WriteLine($"nssymbol: {nssymbol.Name},{nssymbol.NamespaceKind},{nssymbol.ToDisplayString()}");
                        }
                    }
                }
                // from documents(source)
                foreach(var doc in proj.Documents)
                {
                    var extFileName = Path.GetFileNameWithoutExtension(doc.FilePath);
                    Console.WriteLine($"doc filepath: {doc.FilePath}");
                    var rootNode = await doc.GetSyntaxRootAsync();
                    var rootNs = rootNode.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
                    string rootNsString;
                    if(rootNs != null)
                    {
                        rootNsString = rootNs.Name.ToString();
                    }else{
                        rootNsString = "Extension";
                    }
                    var extCompilationRoot = CSharpSyntaxTree.ParseText($@"
                    namespace {rootNsString}
                    {{
                        static class MemberwiseToStringExtensions
                        {{

                        }}
                    }}
                    ").GetRoot();
                    foreach(var structDecNode in rootNode.DescendantNodes().OfType<StructDeclarationSyntax>())
                    {
                        var members = new List<string>();
                        foreach(var member in structDecNode.Members.OfType<PropertyDeclarationSyntax>())
                        {
                            Console.WriteLine($"prop id: {member.GetPropertyName()}");
                            members.Add(member.GetPropertyName());
                        }
                        foreach(var member in structDecNode.Members.OfType<FieldDeclarationSyntax>())
                        {
                            foreach(var x in member.DescendantNodes())
                            {
                                Console.WriteLine($"{x.Kind()},{x.GetText()}");
                            }
                            Console.WriteLine($"field id: {member.GetFieldName()}");
                            members.Add(member.GetFieldName());
                        }
                        var method = CSharpSyntaxTree.ParseText($@"
public static string MemberwiseToString(this {structDecNode.Identifier} obj)
{{
    return string.Format(""{string.Join(", ", Enumerable.Range(0, members.Count).Select(i => $"{{{i}}}"))}"",
        {string.Join(", ", members.Select(m => $"obj.{m}"))});
}}");
                        // var newStructNode = structDecNode.AddMembers(
                        //     method.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First()
                        //         .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n")));
                        // Console.WriteLine($"newstructnode text: {newStructNode.GetText()}");
                        // rootNode = rootNode.ReplaceNode(structDecNode, newStructNode);
                        // var formatted = Formatter.Format(rootNode, ws);
                        // Console.WriteLine($"formatted text: {formatted.GetText()}");
                        var extClassDef = extCompilationRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
                        var newExtClassDef = extClassDef.AddMembers(method.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First());
                        extCompilationRoot = extCompilationRoot.ReplaceNode(extClassDef, newExtClassDef);
                    }
                    foreach(var classDecNode in rootNode.DescendantNodes().OfType<ClassDeclarationSyntax>())
                    {
                        foreach(var ns in classDecNode.Ancestors().OfType<NamespaceDeclarationSyntax>())
                        {
                            Console.WriteLine($"ns name: {ns.Name}");
                        }
                        foreach(var member in classDecNode.Members.OfType<PropertyDeclarationSyntax>())
                        {
                            Console.WriteLine($"prop id: {member.GetPropertyName()}");
                        }
                        foreach(var member in classDecNode.Members.OfType<FieldDeclarationSyntax>())
                        {
                            Console.WriteLine($"field id: {member.GetFieldName()}");
                        }
                    }
                    extCompilationRoot = Formatter.Format(extCompilationRoot, ws);
                    Console.WriteLine($"ext source: {extCompilationRoot.GetText()}");
                }
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
