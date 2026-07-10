using BenchmarkDotNet.Attributes;

namespace MesCopilot.Benchmarks;

[MemoryDiagnoser]
public class SchedulingBenchmarks
{
    private Operation[] _operations = [];

    [GlobalSetup]
    public void Setup()
    {
        _operations = Enumerable.Range(0, 500)
            .Select(index => new Operation(index / 10, index % 10, 15 + index % 45))
            .Reverse()
            .ToArray();
    }

    [Benchmark]
    public DateTime ScheduleFiftyOrdersWithTenSteps()
    {
        DateTime cursor = new(2026, 7, 10, 8, 0, 0, DateTimeKind.Utc);
        foreach (Operation operation in _operations.OrderBy(item => item.WorkOrder).ThenBy(item => item.Sequence))
        {
            cursor = cursor.AddMinutes(operation.DurationMinutes);
        }
        return cursor;
    }

    private sealed record Operation(int WorkOrder, int Sequence, int DurationMinutes);
}
