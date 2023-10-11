using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;

namespace IMCommon;

public class MessageEntityEncoder : MessageToMessageEncoder<StringMessage>
{
    static private readonly IInternalLogger s_logger = LoggerHelper.GetLogger<MessageEntityEncoder>();

    protected override void Encode(IChannelHandlerContext context, StringMessage message, List<object> output)
    {
        if (message != null)
        {
            output.Add(message.ToByteBuffer());
        }
    }
}
