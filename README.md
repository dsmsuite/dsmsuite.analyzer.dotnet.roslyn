#  A software dependency analyzer for C# using the Roslyn compiler

# Introduction

This reposiory contains a software dependency analyzer for C#. 

Planned features:
- Uses a solution file as input.
- Analysis software dependencies at detailed level e.g. methods, variables, types.
- Resulting nodes and edges are saved in a sqlite database. This database can be imported in the dsmsuite viewer.

# References
The following information was used as inputb for the development of te code:

* [Chatgpt session](ChaptgptSession.md) used for initial setup of code
* [Introduction to Roslyn and its use in program development](https://unicorn-dev.medium.com/introduction-to-roslyn-and-its-use-in-program-development-ee576503d659) to get some mor background on Rislyn and refine the solution.


# Status

Under development. Not stable version is available yet.

# To view AST

https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/syntax-visualizer?tabs=csharp



