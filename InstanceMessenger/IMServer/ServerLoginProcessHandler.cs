using System.Net;
using System.Text.Json;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using IMCommon;

namespace IMServer;

public class ServerLoginProcessHandler : SimpleChannelInboundHandler<StringMessage>
{
    private static readonly IInternalLogger s_logger = LoggerHelper.GetLogger<ServerLoginProcessHandler>();

    protected override async void ChannelRead0(IChannelHandlerContext ctx, StringMessage message)
    {
        if (message.Action != E_ACTION.LOGIN)
        {
            new ExceptionSender(ctx).Send($"로그인 요청이 아닙니다.({message.Action})", true);
            return;
        }

        var id = message.RefId;
        var name = message.RefName;
        var nickname = message.GetHeader("nickname");
        var passwd = message.GetHeader("passwd");

        if (string.IsNullOrEmpty(id))
        {
            new ExceptionSender(ctx).Send("id가 비었습니다.", true);
        }

        if (RoomManager.INSTANCE.ExistsId(id!))
        {
            new ExceptionSender(ctx).Send($"해당하는 id가 이미 로그인한 사용자가 있습니다. ({id})", true);
        }

        if (!"1111".Equals(passwd))
        {
            new ExceptionSender(ctx).Send("패스워드가 틀립니다. (패스워드는 무조건 '1111'로)", true);
            return;
        }

        var loginInfo = new LoginUserInfo(id, name, nickname, ctx.Channel);

        var remoteEndPoint = (IPEndPoint)ctx.Channel.RemoteAddress;
        var host = remoteEndPoint.Address.ToString();
        var port = remoteEndPoint.Port;

        s_logger.Info($"로그인 함 {loginInfo.Desc()} [{host}] [{port}]");
        ChannelHelper.Create(ctx).AttachLoginUserInfo(loginInfo);

        RoomManager.INSTANCE.Login(ctx.Channel);
        //로그인후에는 이 핸들러 삭제
        ctx.Channel.Pipeline.Remove(this);

        // 방 목록을 보내주기
        var roomList = RoomManager.INSTANCE.RoomList;
        var roomListJson = JsonSerializer.Serialize(roomList);

        var response = new StringMessage.Builder()
            .SetAction(E_ACTION.ROOM_LIST)
            .SetContents(roomListJson)
            .Build();

        await ctx.WriteAndFlushAsync(response);
    }
}
