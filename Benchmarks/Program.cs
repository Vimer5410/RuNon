using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Running;

class Program
{
    static void Main(string[] args)
    {


        var summary = BenchmarkRunner.Run<FullCryptoBenchmarks>();
    }
}