using System.Text.Json;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using IMCommon;

namespace IMServer;

public class MessageServerReceiveHandler : SimpleChannelInboundHandler<StringMessage>
{
    private static readonly IInternalLogger s_logger = LoggerHelper.GetLogger<MessageServerReceiveHandler>();

    protected override async void ChannelRead0(IChannelHandlerContext ctx, StringMessage message)
    {
        switch (message.Action)
        {
            case E_ACTION.ROOM_LIST:
                SendRoomList(ctx);
                break;

            case E_ACTION.ENTER_TO_ROOM:
                EnterToRoom(ctx, message);
                break;

            case E_ACTION.TALK_MESSAGE:
                s_logger.Info($"[talk]{message.Contents} From {ChannelHelper.Create(ctx).GetLoginUserInfo().Name}");
                RoomManager.INSTANCE.TalkInTheRoom(ctx.Channel, message.Contents);
                break;

            case E_ACTION.LOGOUT:
                LogOut(ctx);
                // channelInactive()에서 처리함.
                break;

            case E_ACTION.EXIT_FROM_ROOM:
                ExitFromRoom(ctx, message);
                break;

            case E_ACTION.USER_LIST:
                SendUserList(ctx, message);
                break;

            default:
                break;
        }
    }

    private async void LogOut(IChannelHandlerContext ctx)
    {
        RoomManager.INSTANCE.Logout(ctx.Channel);
    }

    private async void SendRoomList(IChannelHandlerContext ctx)
    {
        var roomList = RoomManager.INSTANCE.RoomList;
        var jsonStr = JsonSerializer.Serialize(roomList);

        var response = new StringMessage.Builder(ctx)
            .SetAction(E_ACTION.ROOM_LIST)
            .SetContents(jsonStr)
            .Build();

        await ctx.WriteAndFlushAsync(response);
    }

    private void EnterToRoom(IChannelHandlerContext ctx, StringMessage msg)
    {
        var roomName = msg.Contents;
        if (string.IsNullOrEmpty(roomName))
        {
            new ExceptionSender(ctx).Send("방이름을 입력하지 않았습니다.", false);
            return;
        }

        RoomManager.INSTANCE.EnterToRoom(roomName, ctx.Channel);
    }

    private void ExitFromRoom(IChannelHandlerContext ctx, StringMessage msg)
    {
        RoomManager.INSTANCE.ExitFromRoom(ctx.Channel);
    }

    private async void SendUserList(IChannelHandlerContext ctx, StringMessage msg)
    {
        // 방 사람들 목록 보내주기
        var idList = RoomManager.INSTANCE.GetIdList(ctx.Channel);
        s_logger.Info($"SendUserList/IdList:{idList.Count}");
        var jsonStr = JsonSerializer.Serialize(idList);

        var response = new StringMessage.Builder()
            .SetAction(E_ACTION.USER_LIST)
            .SetContents(jsonStr)
            .Build();

        await ctx.WriteAndFlushAsync(response);
    }
}
