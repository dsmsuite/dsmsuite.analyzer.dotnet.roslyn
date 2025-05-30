﻿using dsmsuite.analyzer.dotnet.roslyn.Analysis.Registration;
using dsmsuite.analyzer.dotnet.roslyn.Analysis.Reporting;
using dsmsuite.analyzer.dotnet.roslyn.Graph;
using dsmsuite.analyzer.dotnet.roslyn.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil.Cil;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace dsmsuite.analyzer.dotnet.roslyn.test
{
    public class TestFixture : IResultReporter
    {
        private string _namespace;
        private HierarchicalGraph _hierarchicalGraph;
        private int _failedCount = 0;
        private int _ignoredCount = 0;

        public TestFixture()
        {
            _namespace = GetNamespace();
            _hierarchicalGraph = CreateHierarchicalGraph();
        }

        public void Analyze(string sourceCodeFile, [CallerFilePath] string callerFilePath = "")
        {
            SyntaxTree tree = CreateSyntaxTreeFromSourceCodeFile(sourceCodeFile, callerFilePath);
            SemanticModel semanticModel = CreateSemanticModel(tree);
            SyntaxNodeVisitor walker = new SyntaxNodeVisitor(semanticModel, _hierarchicalGraph);
            walker.Visit(tree.GetRoot());
            _hierarchicalGraph.Build();

            Console.WriteLine("Actual nodes:");
            foreach (INode node in _hierarchicalGraph.Nodes)
            {
                if (IncludeNode(node))
                {
                    Console.WriteLine($"Assert.IsTrue(NodeExists(\"{GetRelativeNodeName(node.Fullname)}\", NodeType.{node.NodeType}));");
                }
                //Console.WriteLine($"  name={node.Fullname} type={node.NodeType} file={node.Filename} lines={node.Startline}-{node.Endline}");
            }

            Console.WriteLine("Actual edges:");
            foreach (IEdge edge in _hierarchicalGraph.Edges)
            {
                if (IncludeNode(edge.Source) && IncludeNode(edge.Target))
                {
                    Console.WriteLine($"Assert.IsTrue(EdgeExists(\"{GetRelativeNodeName(edge.Source.Fullname)}\",\"{GetRelativeNodeName(edge.Target.Fullname)}\",EdgeType.{edge.EdgeType}));");
                    //Console.WriteLine($"  Edge: source={edge.Source.Fullname} target={edge.Target.Fullname} type={edge.EdgeType} file={edge.Filename} line={edge.Line}");
                }
            }
        }



        public string Namespace => _namespace;
        public int FailedCount => _failedCount;
        public int IgnoredCount => _ignoredCount;

        public bool NodeCountIs(int expectedNodeCount, NodeType nodeType)
        {
            int actualNodeCount = 0;
            foreach (INode node in _hierarchicalGraph.Nodes)
            {
                if (node.Fullname.StartsWith($"{_namespace}.") && node.NodeType == nodeType)
                {
                    actualNodeCount++;
                }
            }

            return actualNodeCount == expectedNodeCount;
        }

        public bool NodeExists(string name, NodeType nodeType)
        {
            int count = 0;
            foreach (INode node in _hierarchicalGraph.Nodes)
            {
                if (NodeNameMatches(node, name) &&
                    NodeTypeMatches(node, nodeType))
                {
                    count++;
                }
            }

            return count == 1;
        }

        public INode? FindNode(string name, NodeType nodeType)
        {
            INode? foundNode = null;
            foreach (INode node in _hierarchicalGraph.Nodes)
            {
                if (NodeNameMatches(node, name) &&
                    NodeTypeMatches(node, nodeType))
                {
                    foundNode = node;
                }
            }
            return foundNode;
        }

        public bool EdgeCountIs(int expectedEdgeCount, EdgeType edgeType)
        {
            int actualEdgeCount = 0;
            foreach (IEdge edge in _hierarchicalGraph.Edges)
            {
                if (edge.EdgeType == edgeType)
                {
                    actualEdgeCount++;
                }
            }

            return actualEdgeCount == expectedEdgeCount;
        }

        public bool EdgeExists(string source, string target, EdgeType edgeType)
        {
            int count = 0;
            foreach (IEdge edge in _hierarchicalGraph.Edges)
            {
                if (NodeNameMatches(edge.Source, source) &&
                    NodeNameMatches(edge.Target, target) &&
                    EdgeTypeMatches(edge, edge.EdgeType))
                {
                    count++;
                }
            }

            return count == 1;
        }

        public IEdge? FindEdge(string source, string target, EdgeType edgeType)
        {
            IEdge? foundEdge = null;
            foreach (IEdge edge in _hierarchicalGraph.Edges)
            {
                if (NodeNameMatches(edge.Source, source) &&
                    NodeNameMatches(edge.Target, target) &&
                    EdgeTypeMatches(edge, edge.EdgeType))
                {
                    foundEdge = edge;
                }
            }
            return foundEdge;
        }

        private bool IncludeNode(INode node)
        {
            return node.Fullname.StartsWith($"{_namespace}.");
        }

        private bool NodeNameMatches(INode node, string actual)
        {
            return node.Fullname == GetFullNodeName(actual);
        }

        private string GetFullNodeName(string relativeNodeName)
        {
            return $"{_namespace}.{relativeNodeName}";
        }

        private string GetRelativeNodeName(string fullNodeName)
        {
            if (fullNodeName.StartsWith($"{_namespace}."))
            {
                return fullNodeName.Substring(_namespace.Length + 1);
            }
            else
            {
                return fullNodeName;
            }
        }

        private bool NodeTypeMatches(INode node, NodeType nodeType)
        {
            return node.NodeType == nodeType;
        }

        private bool EdgeTypeMatches(IEdge edge, EdgeType edgeType)
        {
            return edge.EdgeType == edgeType;
        }

        public void ReportResult(string actionDescription, string syntaxNodeFilename, int syntaxNodeline, Result result, [CallerFilePath] string sourceFile = "", [CallerMemberName] string method = "", [CallerLineNumber] int lineNumber = 0)
        {
            switch (result)
            {
                case Result.Success:
                    break;
                case Result.Failed:
                    Console.WriteLine($"Failed: action={actionDescription} line={syntaxNodeline} from method={method} line={lineNumber}");
                    _failedCount++;
                    break;
                case Result.Ignored:
                    Console.WriteLine($"Ignored: action={actionDescription} line={syntaxNodeline} from method={method} line={lineNumber}");
                    _ignoredCount++;
                    break;
                default:
                    break;
            }
        }

        private SyntaxTree CreateSyntaxTreeFromSourceCodeFile(string sourceCodeFile, string callerFilePath)
        {
            string? callerDirectoryPath = Path.GetDirectoryName(callerFilePath);
            Assert.IsNotNull(callerDirectoryPath, "Caller directory path cannot be null.");

            string filename = Path.Combine(callerDirectoryPath, sourceCodeFile);
            string code = File.ReadAllText(filename);
            return CSharpSyntaxTree.ParseText(code);
        }

        private SemanticModel CreateSemanticModel(SyntaxTree tree)
        {
            CSharpCompilation compilation = CreateCompilationUnit(tree);
            return compilation.GetSemanticModel(tree);
        }

        private HierarchicalGraph CreateHierarchicalGraph()
        {
            return new HierarchicalGraph(this);
        }

        private string GetNamespace()
        {
            string? assemblyName = typeof(TestFixture).Assembly.GetName().Name;
            Assert.IsNotNull(assemblyName, "Assembly name cannot be null.");
            return assemblyName;
        }

        private CSharpCompilation CreateCompilationUnit(SyntaxTree tree)
        {
            Guid guid = Guid.NewGuid();
            PortableExecutableReference mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            CSharpCompilation compilation = CSharpCompilation.Create(
                $"Analysis_{guid}",
                syntaxTrees: new[] { tree },
                references: new[] { mscorlib });
            return compilation;
        }
    }
}
