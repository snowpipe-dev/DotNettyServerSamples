using DotNetty.Transport.Channels;

namespace Chat.Client;

public class ChatClientHandler : SimpleChannelInboundHandler<string>
{
    protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
    {
        Console.WriteLine($"Message: {msg}");
    }
}
