using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;

namespace IMCommon;

public class MessagePacketEncoder : MessageToMessageEncoder<MessagePacket>
{
    private static readonly IInternalLogger s_logger = LoggerHelper.GetLogger<MessagePacketEncoder>();

    protected override void Encode(IChannelHandlerContext context, MessagePacket message, List<object> output)
    {
        if (message != null)
        {
            s_logger.Info(message.ToString());
            output.Add(PacketHelper.MakeByteBuffer(message));
        }
    }
}
