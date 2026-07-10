using BenchmarkDotNet.Attributes;

namespace MesCopilot.Benchmarks;

[MemoryDiagnoser]
public class BomExplosionBenchmarks
{
    private BomNode[] _nodes = [];

    [GlobalSetup]
    public void Setup()
    {
        _nodes = Enumerable.Range(0, 10_000)
            .Select(index => new BomNode(index, index == 0 ? -1 : (index - 1) / 4, 1m + index % 3))
            .ToArray();
    }

    [Benchmark]
    public decimal ExplodeTenThousandNodes()
    {
        Dictionary<int, decimal> totals = new(10_000) { [0] = 1m };
        foreach (BomNode node in _nodes.AsSpan(1))
        {
            totals[node.Id] = totals[node.ParentId] * node.Quantity;
        }
        return totals.Values.Sum();
    }

    private sealed record BomNode(int Id, int ParentId, decimal Quantity);
}
