using Xunit.Abstractions;

namespace TestIMServer;

public abstract class BaseUnitTest
{
    protected readonly ITestOutputHelper _output;

    public BaseUnitTest(ITestOutputHelper output)
    {
        _output = output;
    }
}