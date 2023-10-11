using System.Text;
using DotNetty.Buffers;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;

namespace IMCommon;

public class ByteBufferHelper
{
    static private IInternalLogger s_logger = LoggerHelper.GetLogger<ByteBufferHelper>();
    static public readonly byte CR;
    static public readonly byte LF;
    static public readonly byte[] CRLF;
    static public readonly string STR_CRLF;

    static ByteBufferHelper()
    {
        CR = 13;
        LF = 10;
        CRLF = new byte[] { CR, LF };
        STR_CRLF = Encoding.UTF8.GetString(CRLF);
    }

    static public string GetLine(IByteBuffer input)
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
