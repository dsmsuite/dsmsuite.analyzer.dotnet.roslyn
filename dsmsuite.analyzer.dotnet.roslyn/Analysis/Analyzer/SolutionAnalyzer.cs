﻿using dsmsuite.analyzer.dotnet.roslyn.Analysis.Registration;
using dsmsuite.analyzer.dotnet.roslyn.Analysis.Reporting;
using dsmsuite.analyzer.dotnet.roslyn.Graph;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;


namespace dsmsuite.analyzer.dotnet.roslyn.Analysis.Analyzer
{
    public class SolutionAnalyzer : ICodeAnalyzer
    {
        private readonly string _solutionPath;
        private readonly HierarchicalGraph _hierarchicalGraph;

        public SolutionAnalyzer(string solutionPath, IResultReporter reporter)
        {
            _solutionPath = solutionPath;
            _hierarchicalGraph = new HierarchicalGraph(reporter);
        }

        public IHierarchicalGraph Graph => _hierarchicalGraph;

        public async Task AnalyzeAsync()
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Solution solution = await workspace.OpenSolutionAsync(_solutionPath);

            foreach (Project project in solution.Projects)
            {
                if (project.FilePath != null)
                {
                    Console.WriteLine($"Processing project {project.FilePath}");

                    Compilation? compilation = await project.GetCompilationAsync();

                    if (compilation != null)
                    {
                        foreach (Document document in project.Documents)
                        {
                            if (document.FilePath != null)
                            {
                                SyntaxTree? syntaxTree = await document.GetSyntaxTreeAsync();
                                if (syntaxTree != null)
                                {
                                    SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);

                                    SyntaxNode root = await syntaxTree.GetRootAsync();
                                    SyntaxNodeVisitor visitor = new SyntaxNodeVisitor(semanticModel, _hierarchicalGraph);
                                    visitor.Visit(root);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void BuildGraph()
        {
            _hierarchicalGraph.Build();
        }
    }
}