using DotNetty.Transport.Channels;

namespace IMCommon;

public class ExceptionSender
{
    private IChannelHandlerContext _ctx;

    public ExceptionSender(IChannelHandlerContext ctx)
    {
        _ctx = ctx;
    }

    public void Send(string message, bool closeChannel)
    {
        var messageEntity = new StringMessage.Builder(_ctx)
            .SetAction(E_ACTION.RESPONSE_FAIL)
            .SetContents(message)
            .Build();

        _ctx.WriteAndFlushAsync(messageEntity)
            .ContinueWith(e =>
            {
                if (closeChannel)
                {
                    _ctx.CloseAsync();
                }
            });
    }

    // public void Send(Throwable th, bool closeChannel)
    // {
    //     if (th instanceof MsgException) {
    //         MsgException ex = (MsgException)th;
    //         String msg = ex.getMessage() + "[" + ex.getCode() + "]";
    //         StrMsgEntity msgEntity = new StrMsgEntityBuilder(ctx).setAction(MsgAction.Response_Fail).setContents(msg).build();
    //         ctx.writeAndFlush(msgEntity).addListener(f-> {
    //             if (closeChannel) { ctx.close(); }
    //         });
    //     } else
    //     {
    //         String msg = "[ERROR] " + th.getMessage();
    //         StrMsgEntity msgEntity = new StrMsgEntityBuilder(ctx).setAction(MsgAction.Response_Fail).setContents(msg).build();
    //         ctx.writeAndFlush(msgEntity).addListener(f-> {
    //             if (closeChannel) { ctx.close(); }
    //         });
    //     }
    // }
}
