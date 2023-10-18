using System.Reflection;
using System.Text;
using IMCommon;
using IMServer;
using Xunit.Abstractions;

namespace TestIMServer;

public class StringMessageEntityTest : BaseUnitTest
{
    public StringMessageEntityTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void TestMakeMessage()
    {
        var expectedBody = "TestMessage";
        var expectedBytesLength = 11;

        var messagePacket = new MessagePacket(E_ACTION.LOGIN);
        messagePacket.SetBody(expectedBody);

        var message = messagePacket.MakeMessage();

        Assert.True(message.Body.Equals(expectedBody),
            $"expected body is {expectedBody}");
        Assert.True(message.BodyBytes.Length == expectedBytesLength,
            $"expected bodyBytes is {expectedBytesLength}");
    }

    [Fact]
    public void TestToByteBuffer()
    {
        var expectedBody = "TestMessage";
        var messagePacket = new MessagePacket(E_ACTION.LOGIN);
        messagePacket.SetBody(expectedBody);

        var result = PacketHelper.MakeByteBuffer(messagePacket);

        Assert.True(result.Array.Length > 0,
            $"Expected bytes Length is greater than 0");
    }
}