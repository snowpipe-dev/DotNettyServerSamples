using System.Text;
using DotNetty.Buffers;
using IMServer;
using Xunit.Abstractions;

namespace TestIMServer;

public class ByteBufferHelperTest : BaseUnitTest
{
    public ByteBufferHelperTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void TestGetLine()
    {
        var expectedLine = "ALine";
        var expectedNextLine = "NextLine";
        var expectedNextReaderIndex = 7;

        var sb = new StringBuilder();
        sb.Append("ALine").Append(ByteBufferHelper.STR_CRLF).Append("expectedNextLine");

        var byteBuffer = ByteBufferUtil.WriteUtf8(PooledByteBufferAllocator.Default, sb.ToString());
        var resultLine = ByteBufferHelper.GetLine(byteBuffer);
        Assert.True(resultLine.Equals(expectedLine),
            $"expected Line is {expectedLine}");

        var readerIndex = byteBuffer.ReaderIndex;
        Assert.True(readerIndex == expectedNextReaderIndex, 
            $"expected next readerIndex is {readerIndex}");
    }
}
