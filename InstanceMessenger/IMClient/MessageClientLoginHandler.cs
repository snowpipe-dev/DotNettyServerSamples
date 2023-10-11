using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using IMCommon;

namespace IMClient;

public class MessageClientLoginHandler : ChannelHandlerAdapter
{
    static private readonly IInternalLogger s_logger = LoggerHelper.GetLogger<MessageClientLoginHandler>();
    private readonly LoginRequest _loginRequest;

    public MessageClientLoginHandler(LoginRequest loginRequest)
    {
        _loginRequest = loginRequest;
    }

    // public override Task CloseAsync(IChannelHandlerContext ctx)
    // {
    //     s_logger.Info("MessageClientLoginHandler.Close");
    //     MyLoginInfo.INSTANCE.Channel = null;

    //     // 종료될때 처리
    //     ctx.Channel.Pipeline.Remove(this);
    //     return base.CloseAsync(ctx);
    // }

    public override void ChannelActive(IChannelHandlerContext ctx)
    {
        //login 처리
        var requestLogin = new StringMessage.Builder(ctx)
                .SetAction(E_ACTION.LOGIN)
                .SetRefId(_loginRequest.Id)
                .SetRefName(_loginRequest.Name)
                .SetHeader("nickname", _loginRequest.Nick)
                .SetHeader("passwd", _loginRequest.Passwd)
                .Build();

        ctx.WriteAndFlushAsync(requestLogin.ToByteBuffer()).ContinueWith(async e =>
            {
                s_logger.Info("로그인됨");
                MyLoginInfo.INSTANCE.Channel = ctx.Channel;
                MyLoginInfo.INSTANCE.Id = _loginRequest.Id;
                MyLoginInfo.INSTANCE.RoomName = string.Empty;
                MyLoginInfo.INSTANCE.Name = _loginRequest.Name;
                MyLoginInfo.INSTANCE.Nickname = _loginRequest.Nick;

                // 기본방에 대한 user list 요청
                var requestUserList = new StringMessage.Builder(ctx)
                    .SetAction(E_ACTION.USER_LIST)
                    .SetRoomName(MyLoginInfo.INSTANCE.RoomName)
                    .Build();

                await ctx.WriteAndFlushAsync(requestUserList);
            });

        ClientConsoleInput.INSTANCE.SetChannel(ctx.Channel);
    }

    public override void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
    {
        s_logger.Info($"Closing connection for client - {ctx}");
        s_logger.Trace(exception);

        ctx.CloseAsync();
    }
}
