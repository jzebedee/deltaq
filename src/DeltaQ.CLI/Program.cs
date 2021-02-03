using System;
using System.IO;

namespace DeltaQ.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var f1 = args[1];
            var f2 = args[2];
            var o = args[3];
            switch(args[0])
            {
                case "diff":
                    BsDiff.BsDiff.Create(File.ReadAllBytes(f1), File.ReadAllBytes(f2), File.OpenWrite(o));
                    break;
                case "patch":
                    BsDiff.BsPatch.Apply(File.ReadAllBytes(f1), File.ReadAllBytes(f2), File.OpenWrite(o));
                    break;
            }
        }
    }
}
