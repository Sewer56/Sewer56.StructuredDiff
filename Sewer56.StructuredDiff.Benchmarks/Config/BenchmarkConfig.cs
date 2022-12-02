using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Sewer56.StructuredDiff.Benchmarks.Config;

internal class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig(Func<Summary, BenchmarkCase, long> getFileSize)
    {
        Add(DefaultConfig.Instance);

        AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithId(".NET (Current Process)"));

        //AddJob(Job.Default.WithRuntime(CoreRuntime.Core31));
        AddExporter(MarkdownExporter.GitHub);
        AddColumn(BaselineRatioColumn.RatioMean);
        AddColumn(new Speed(getFileSize));
        AddLogger(new ConsoleLogger());
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
    }
}