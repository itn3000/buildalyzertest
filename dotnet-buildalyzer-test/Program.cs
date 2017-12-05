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
                foreach (var symbol in compilation.GetSymbolsWithName(x => true, SymbolFilter.Type))
                {
                    if (symbol is ITypeSymbol ts)
                    {
                        Console.WriteLine($"name={ts.Name}");
                        var nssymbol = ts.ContainingNamespace;
                        if (nssymbol != null)
                        {
                            Console.WriteLine($"{nssymbol.Name},{nssymbol.NamespaceKind}");
                        }
                    }
                }
                foreach(var doc in proj.Documents)
                {
                    Console.WriteLine($"{doc.FilePath}");
                    var rootNode = await doc.GetSyntaxRootAsync();
                    foreach(StructDeclarationSyntax structDecNode in rootNode.DescendantNodes(node => node.IsKind(SyntaxKind.StructDeclaration)))
                    {
                    }
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
