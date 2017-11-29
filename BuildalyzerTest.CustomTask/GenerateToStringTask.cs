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
            try
            {
                if (System.AppDomain.CurrentDomain != null)
                {
                    System.AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
                    {
                        taskFactoryLoggingHost.LogMessageEvent(new MSBFramework.BuildMessageEventArgs($"resolving {e.Name}", "AssemblyResolve", "AssemblyResolve", MSBFramework.MessageImportance.Normal));
                        return Assembly.Load(new AssemblyName(e.Name));
                    };
                }
            }
            catch (Exception e)
            {

            }
            return true;
        }
    }

    public class GenerateToStringTask : MSBUtil.Task
    {
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
            ProcessWorkspace(projectAnalyzer);
            // var projectInstance = projectAnalyzer.Compile();
            // if(projectInstance != null)
            // {
            //     foreach(var item in projectInstance.GetItems("Compile"))
            //     {
            //         Log.LogMessage($"{item.ItemType},{item.EvaluatedInclude}");
            //     }
            // }else{
            //     Log.LogMessage("projectInstance is NULL");
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
