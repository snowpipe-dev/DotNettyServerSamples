using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using IMCommon;

namespace IMServer;

public class MessageServerInitializer : ChannelInitializer<ISocketChannel>
{
    private static readonly IInternalLogger s_logger = LoggerHelper.GetLogger<MessageServerInitializer>();

    protected override void InitChannel(ISocketChannel channel)
    {
        var pipeline = channel.Pipeline;
        pipeline.AddLast(new MessageEntityEncoder());
        pipeline.AddLast(new StringEncoder());
        pipeline.AddLast("RequestEntityDecoder", new RequestDecoder());
        pipeline.AddLast(new ServerLoginProcessHandler());
        pipeline.AddLast(new MessageServerReceiveHandler());
    }
}
