using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;

namespace DevCrews.EchoServer;

/// <summary>
/// 메시지 안의 글자수 계산
/// SimpleChannelInboundHandler 기반이지만 상속받은 건 아님.
/// </summary>

public class CountCharsServerHandler : ChannelHandlerAdapter
{
    static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<CountCharsServerHandler>();

    public bool AcceptInboundMessage(object msg) => msg is string;
    public override bool IsSharable => false; 
    // Not sharable as this handler uses a member variable (_messageLength)

    private int _messageLength;

    public override void ChannelRead(IChannelHandlerContext ctx, object message)
    {
        if (this.AcceptInboundMessage(message))
        {
            var stringMessage = (string)message;
            ChannelReadImpl(ctx, stringMessage);
            ctx.FireChannelRead(message); 
            // Triggers the next ChannelInboundHandler.ChannelRead in the ChannelPipeline
        }
    }

    void ChannelReadImpl(IChannelHandlerContext ctx, string msg)
    {
        Logger.Info($"Received message length: {msg.Length}");
        _messageLength = msg.Length;
    }

    /// <summary>
    /// ChannelReadComplete 메소드는 전체 ChannelPipeline가 실행된 이후에 실행된다.
    /// </summary>
    public override void ChannelReadComplete(IChannelHandlerContext ctx)
    {
        ctx.WriteAndFlushAsync($"Your message contained {_messageLength} chars. {System.Environment.NewLine}");
        ctx.FireChannelReadComplete();
    }

}