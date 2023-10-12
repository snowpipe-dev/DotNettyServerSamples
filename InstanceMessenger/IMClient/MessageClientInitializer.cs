using DotNetty.Codecs;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using IMCommon;

namespace IMClient;

public class MessageClientInitializer : ChannelInitializer<ISocketChannel>
{
    private static readonly IInternalLogger s_logger = LoggerHelper.GetLogger<MessageClientInitializer>();
    private static MessageEntityEncoder s_messageEntityEncoder = new();
    private LoginRequest _request;

    public MessageClientInitializer(LoginRequest loginRequest)
    {
        _request = loginRequest;
    }

    protected override void InitChannel(ISocketChannel channel)
    {
        var pipeline = channel.Pipeline;
        pipeline.AddLast(s_messageEntityEncoder);
        pipeline.AddLast(new StringEncoder());
        pipeline.AddLast(new MessageClientLoginHandler(_request));
        pipeline.AddLast("RequestEntityDecoder", new RequestDecoder());
        pipeline.AddLast(new ClientMessageReceiveHandler()); //Logging
    }
}
