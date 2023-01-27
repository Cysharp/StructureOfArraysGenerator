using StructureOfArraysGenerator;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

var config = ManualConfig.CreateMinimumViable()
    .AddDiagnoser(MemoryDiagnoser.Default)
    .AddExporter(MarkdownExporter.Default)
    .AddJob(Job.Default.WithWarmupCount(1).WithIterationCount(1));

#if !DEBUG

BenchmarkRunner.Run<Benchmark>(config, args);

#else

var v = new Benchmark().MultiArraySimdMax();
Console.WriteLine(v);

#endif



[MultiArray(typeof(Vector3))]
public readonly partial struct Vector3MultiArray
{
}




public class Benchmark
{
    const int Count = 10000;

    Vector3[] vector3Array;
    Vector3MultiArray vector3MultiArray;

    public Benchmark()
    {
        var rand = new Random(124);

        var array = new Vector3[Count];
        for (int i = 0; i < Count; i++)
        {
            array[i] = new Vector3(rand.NextSingle(), rand.NextSingle(), rand.NextSingle());
        }

        var multiArray = new Vector3MultiArray(Count);
        for (int i = 0; i < multiArray.Length; i++)
        {
            multiArray[i] = array[i];
        }

        this.vector3Array = array;
        this.vector3MultiArray = multiArray;

        var reference = vector3MultiArray.Y.ToArray().Max();
        var v1 = ArrayMax();
        var v2 = MultiArrayMax();
        var v3 = MultiArraySimdMax();
        if (v1 != reference || v1 != v2 || v1 != v3)
        {
            throw new Exception("different result");
        }
    }

    [Benchmark]
    public float ArrayMax()
    {
        var array = vector3Array;
        var max = float.MinValue;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].Y > max)
            {
                max = array[i].Y;
            }
        }
        return max;
    }

    [Benchmark]
    public float MultiArrayMax()
    {
        var array = vector3MultiArray.Y;
        var max = float.MinValue;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] > max)
            {
                max = array[i];
            }
        }
        return max;
    }

    [Benchmark]
    public float MultiArraySimdMax()
    {
        var array = vector3MultiArray.Y;
        ref var p = ref MemoryMarshal.GetReference(array);
        ref var to = ref Unsafe.Add(ref p, array.Length - Vector256<float>.Count);

        var max = Vector256.LoadUnsafe(ref p);
        p = ref Unsafe.Add(ref p, Vector256<float>.Count);

        while (Unsafe.IsAddressLessThan(ref p, ref to))
        {
            var v = Vector256.LoadUnsafe(ref p);
            max = Vector256.Max(max, v);

            p = ref Unsafe.Add(ref p, Vector256<float>.Count);
        }

        var result = float.MinValue;
        for (int i = 0; i < Vector256<float>.Count; i++)
        {
            var v = max[i];
            if (v > result)
            {
                result = v;
            }
        }

        ref var last = ref Unsafe.Add(ref MemoryMarshal.GetReference(array), array.Length);
        while (Unsafe.IsAddressLessThan(ref p, ref last))
        {
            if (p > result)
            {
                result = p;
            }
            p = ref Unsafe.Add(ref p, 1);
        }

        return result;
    }
}
