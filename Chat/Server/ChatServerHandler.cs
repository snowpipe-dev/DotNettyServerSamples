using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;

namespace Chat.Server;

public class ChatServerHandler : SimpleChannelInboundHandler<string>
{
    static readonly IInternalLogger s_logger = InternalLoggerFactory.GetInstance<ChatServerHandler>();
    
    static readonly List<IChannel> s_channels = new();

    public override void ChannelActive(IChannelHandlerContext ctx)
    {
        s_logger.Info($"Client joined - {ctx}");
        // Console.WriteLine($"Client joined - {ctx}");
        s_channels.Add(ctx.Channel);
    }

    protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
    {
        s_logger.Info($"Server received {msg}");
        // Console.WriteLine($"Server received {msg}");
        foreach (var channel in s_channels)
        {
            channel.WriteAndFlushAsync($"-> {msg}\n");
        }
    }

    public override void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
    {
        s_logger.Info($"Closing connection for client - {ctx}");
        // Console.WriteLine($"Closing connection for client - {ctx}");
        ctx.CloseAsync();
    }
}