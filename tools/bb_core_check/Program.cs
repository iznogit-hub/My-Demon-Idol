using System;
using DemonIdol.BlazBlue;

internal static class Program
{
    static int Main()
    {
        var fails = BBCoreChecks.Run();
        if (fails.Count == 0)
        {
            Console.WriteLine("PASS blazblue core checks");
            return 0;
        }
        foreach (string fail in fails) Console.WriteLine("FAIL " + fail);
        return 1;
    }
}
