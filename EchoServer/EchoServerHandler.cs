using System;
using System.Net;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;

namespace DevCrews.EchoServer;

public class EchoServerHandler : SimpleChannelInboundHandler<string>
{
    static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<EchoServerHandler>();
    
    public override bool IsSharable => true;

    public override void ChannelActive(IChannelHandlerContext contex)
    {
        contex.WriteAsync($"Welcome to {Dns.GetHostName()} !\r\n");
        contex.WriteAsync($"It is {DateTime.Now} now !\r\n");
        contex.WriteAndFlushAsync("Write your name: ");
    }

    protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
    {
        Logger.Info("Received message: " + msg);
        ctx.WriteAndFlushAsync($"Hello, {msg}\r\n");
    }

    // Catches inbound message exceptions
    public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
    {
        Logger.Error(e);
        contex.CloseAsync();
    }
}