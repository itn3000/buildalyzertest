using System;

namespace BuildalyzerTest.CustomTask
{
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
        void DoTask()
        {
            var projectFile = this.BuildEngine.ProjectFileOfTaskNode;
            Log.LogMessage("creating analyzemanager");
            var analyzeManager = new AnalyzerManager();
            Log.LogMessage("get project info:{0}", projectFile);
            var projectAnalyzer = analyzeManager.GetProject(projectFile);
            // Log.LogMessage("get workspace");
            // var ws = projectAnalyzer.GetWorkspace();
            // foreach (var proj in ws.CurrentSolution.Projects)
            // {
            //     if (proj.TryGetCompilation(out var compilation))
            //     {
            //         foreach (var typeSymbol in compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type).OfType<ITypeSymbol>())
            //         {
            //             Log.LogMessage("typename={0}", typeSymbol.Name);
            //         }
            //     }
            // }
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
