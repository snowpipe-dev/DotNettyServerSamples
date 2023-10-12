using System.Text;
using DotNetty.Buffers;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;

namespace IMCommon;

public class ByteBufferHelper
{
    private static IInternalLogger s_logger = LoggerHelper.GetLogger<ByteBufferHelper>();
    public static readonly byte CR;
    public static readonly byte LF;
    public static readonly byte[] CRLF;
    public static readonly string STR_CRLF;

    static ByteBufferHelper()
    {
        CR = 13;
        LF = 10;
        CRLF = new byte[] { CR, LF };
        STR_CRLF = Encoding.UTF8.GetString(CRLF);
    }

    public static string GetLine(IByteBuffer input)
    {
        var posLF = input.ForEachByte(ByteProcessor.FindLF);
        if (posLF < 0)
        {
            return string.Empty;
        }

        int lastStrPos = posLF;
        if (posLF > 0 && input.GetByte(posLF - 1) == CR)
        {
            --lastStrPos;
        }

        int readerIndex = input.ReaderIndex;
        var message = input.ToString(readerIndex, lastStrPos - readerIndex, Encoding.UTF8);
        input.SetReaderIndex(posLF + 1);

        return message;
    }
}
