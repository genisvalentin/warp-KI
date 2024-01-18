using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Playground
{
    public class DictTest
    {

        Dictionary<string, List<string>> testdict;
        int[] groups;

        public DictTest()
        {
            testdict = new Dictionary<string, List<string>>();
            groups = new int[10000];
            for (int i = 0; i < 10000; i++)
            {
                testdict.Add($"movie{i}.tiff", new List<string> { "0", "0", "0", i.ToString() });
                groups[i] = i;
            }
        }

        [Benchmark]
        public void ForLoop()
        {
            int counter = 0;
            foreach (var mic in testdict)
            {
                mic.Value[3] = (groups[counter] + 1).ToString();
                counter++;
            }
        }

        [Benchmark]
        public void Linq()
        {
            testdict = testdict
            .Zip(groups, (kvp, value) => {
                kvp.Value[3] = value.ToString();
                return new KeyValuePair<string, List<string>>(kvp.Key, kvp.Value);
            }
            )
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<DictTest>();
            Console.ReadLine();
        }
    }
}