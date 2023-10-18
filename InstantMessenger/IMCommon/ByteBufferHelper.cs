using System.Text;
using DotNetty.Buffers;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;

namespace IMCommon;

public class PacketHelper
{
    private static IInternalLogger s_logger = LoggerHelper.GetLogger<PacketHelper>();
    public static readonly byte CR;
    public static readonly byte LF;
    public static readonly byte[] CRLF;
    public static readonly string STR_CRLF;

    static PacketHelper()
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

    public static IByteBuffer MakeByteBuffer(MessagePacket messagePacket)
    {
        var message = messagePacket.MakeMessage();

        // var sb = new StringBuilder();
        // var mesageBytes = Encoding.UTF8.GetBytes(message.Header);
        // foreach(var b in mesageBytes)
        // {
        //     sb.AppendFormat("{0:x2}", b);
        // }

        // s_logger.Info($"Header hex:{sb} length:{mesageBytes.Length}");

        var result = ByteBufferUtil.WriteUtf8(PooledByteBufferAllocator.Default, message.Header);
        if (message.BodyBytes != null)
        {
            result.WriteBytes(message.BodyBytes);
        }

        return result;
    }
}
