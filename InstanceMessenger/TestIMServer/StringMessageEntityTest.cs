using System.Reflection;
using System.Text;
using IMServer;
using Xunit.Abstractions;

namespace TestIMServer;

public class StringMessageEntityTest : BaseUnitTest
{
    public StringMessageEntityTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void TestMakeMessage()
    {
        var expectedContents = "TestMessage";
        var expectedBytesLength = 11;

        var stringMessageEntity = new StringMessageEntity(E_ACTION.LOGIN);
        stringMessageEntity.SetContents(expectedContents);

        var message = stringMessageEntity.MakeMessage();

        Assert.True(message.Contents.Equals(expectedContents),
            $"expected contents is {expectedContents}");
        Assert.True(message.ContentsBytes.Length == expectedBytesLength,
            $"expected contentsBytes is {expectedBytesLength}");
    }

    [Fact]
    public void TestToByteBuffer()
    {
        var expectedContents = "TestMessage";
        var stringMessageEntity = new StringMessageEntity(E_ACTION.LOGIN);
        stringMessageEntity.SetContents(expectedContents);

        var result = stringMessageEntity.ToByteBuffer();

        Assert.True(result.Array.Length > 0,
            $"Expected bytes Length is greater than 0");
    }
}