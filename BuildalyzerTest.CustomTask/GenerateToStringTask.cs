using System;

namespace BuildalyzerTest.CustomTask
{
    using IO = System.IO;
    using MSBUtil = Microsoft.Build.Utilities;
    using MSBFramework = Microsoft.Build.Framework;
    using System.Resources;
    using Buildalyzer;
    using Buildalyzer.Workspaces;
    using Microsoft.CodeAnalysis;
    using System.Linq;
    using System.Collections.Generic;
    using System.Runtime;
    using System.Reflection;
    using Microsoft.CodeAnalysis.CSharp;

    public class GenerateToStringTaskFactory : MSBFramework.ITaskFactory
    {
        public string FactoryName => nameof(GenerateToStringTaskFactory);

        public Type TaskType => typeof(GenerateToStringTask);

        MSBFramework.TaskPropertyInfo[] m_Parameters;

        public void CleanupTask(MSBFramework.ITask task)
        {
        }

        public MSBFramework.ITask CreateTask(MSBFramework.IBuildEngine taskFactoryLoggingHost)
        {
            return new GenerateToStringTask();
        }

        public MSBFramework.TaskPropertyInfo[] GetTaskParameters()
        {
            return m_Parameters;
        }

        public bool Initialize(string taskName, IDictionary<string, MSBFramework.TaskPropertyInfo> parameterGroup, string taskBody, MSBFramework.IBuildEngine taskFactoryLoggingHost)
        {
            m_Parameters = parameterGroup.Values.ToArray();
            return true;
        }
    }

    public class GenerateToStringTask : MSBUtil.Task
    {
        void EnumerateSources(ProjectAnalyzer projectAnalyzer)
        {
            var instance = projectAnalyzer.Project.CreateProjectInstance();
            foreach (var evaluated in instance.EvaluatedItemElements)
            {
                Log.LogMessage($"evaluated: {evaluated.Include}");
            }
            const string itemtype = "Compile";
            var projectDir = IO.Path.GetDirectoryName(projectAnalyzer.ProjectFilePath);
            foreach (var item in instance.GetItems(itemtype))
            {
                Log.LogMessage($"itemtype({itemtype}): {item.EvaluatedInclude}");
                var fileText = System.IO.File.ReadAllText(IO.Path.Combine(projectDir, item.EvaluatedInclude));
                var parsed = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(fileText);
                var rootNode = parsed.GetRoot();
                foreach(var clsNode in rootNode.Ancestors().Where(x => x.IsKind(SyntaxKind.ClassDeclaration)))
                {
                    Log.LogMessage($"src = {item.EvaluatedInclude}, text={clsNode.GetText()}");
                }
            }
        }
        void ProcessWorkspace(ProjectAnalyzer projectAnalyzer)
        {
            Log.LogMessage("get workspace");
            var ws = projectAnalyzer.GetWorkspace();
            foreach (var proj in ws.CurrentSolution.Projects)
            {
                if (proj.TryGetCompilation(out var compilation))
                {
                    foreach (var typeSymbol in compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type).OfType<ITypeSymbol>())
                    {
                        Log.LogMessage("typename={0}", typeSymbol.Name);
                    }
                }
            }
        }
        void DoTask()
        {
            var projectFile = this.BuildEngine.ProjectFileOfTaskNode;
            Log.LogMessage("creating analyzemanager");
            var analyzeManager = new AnalyzerManager();
            Log.LogMessage("get project info:{0}", projectFile);
            var projectAnalyzer = analyzeManager.GetProject(projectFile);
            EnumerateSources(projectAnalyzer);
        }
        public override bool Execute()
        {
            try
            {
                DoTask();
                return true;
            }
            catch (Exception e)
            {
                Log.LogError($"failed to task:{e}");
                return false;
            }
        }
    }
}
