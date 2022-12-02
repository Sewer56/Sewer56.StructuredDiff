// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using Sewer56.StructuredDiff.Benchmarks;
using Sewer56.StructuredDiff.Benchmarks.Config;

Console.WriteLine("Hello, World!");

BenchmarkRunner.Run<CreateDiff>(new BenchmarkConfig((_, _) => 65520));
BenchmarkRunner.Run<ApplyDiff>(new BenchmarkConfig((_, _) => 65520));