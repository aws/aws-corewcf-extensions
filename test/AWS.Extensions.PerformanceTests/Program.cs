using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace AWS.Extensions.PerformanceTests
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run(
                typeof(Program).Assembly,
                ManualConfig
                    .Create(DefaultConfig.Instance)
                    .AddValidator(ExecutionValidator.FailOnError)
                    .AddDiagnoser(MemoryDiagnoser.Default, ThreadingDiagnoser.Default, ExceptionDiagnoser.Default)
                    .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Method))
                    .WithOptions(ConfigOptions.JoinSummary)
            );
        }
    }
}
