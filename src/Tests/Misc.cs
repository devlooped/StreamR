using System;
using System.Threading.Tasks;

namespace Devlooped.Assistant;

public class Misc(ITestOutputHelper output)
{
    [Fact]
    public void Run()
    {
        output.WriteLine("Hello World!");
    }   
}
