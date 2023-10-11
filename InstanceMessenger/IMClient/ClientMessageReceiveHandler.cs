using System.Text.Json;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using IMCommon;

namespace IMClient;

public class ClientMessageReceiveHandler : SimpleChannelInboundHandler<StringMessage>
{
    static private readonly IInternalLogger s_logger = LoggerHelper.GetLogger<ClientMessageReceiveHandler>();
    private ClientMessageProcessor _clientMessageProcessor = new();

    public override void ChannelInactive(IChannelHandlerContext context)
    {
        s_logger.Warn("끊김");
    }

    protected override void ChannelRead0(IChannelHandlerContext context, StringMessage message)
    {
        switch (message.Action)
        {
            case E_ACTION.LOGIN:
                s_logger.Info($"'{message.RefId}'님이 로그인했습니다.");
                break;

            case E_ACTION.LOGOUT:
                s_logger.Info($"'{message.RefId}'님이 로그아웃했습니다.");
                break;

            case E_ACTION.ROOM_LIST:
                var roomList = JsonSerializer.Deserialize<List<string>>(message.Contents);
                var roomNameStr = string.Join(",", roomList!);
                s_logger.Info($"> [방 목록] : {roomNameStr}");
                break;

            case E_ACTION.USER_LIST:
                s_logger.Info($"> [사람들 목록] : {message.Contents}");
                break;

            case E_ACTION.TALK_MESSAGE:
                s_logger.Info($"> [{message.RefId}] : {message.Contents}");
                break;

            case E_ACTION.INFO_MESSAGE:
                s_logger.Info($"> {message.Contents}");
                break;

            case E_ACTION.ENTER_TO_ROOM:
                s_logger.Info($"[{message.RoomName}] '{message.RefId}'님이 방으로 들어왔습니다.");
                break;

            case E_ACTION.EXIT_FROM_ROOM:
                s_logger.Info($"[{message.RoomName}] '{message.RefId}'님이 방에서 퇴장했습니다.");
                break;

            case E_ACTION.RESPONSE_FAIL:
                s_logger.Warn($"[FAIL] {message.Contents}");
                break;
        }
    }

    public override void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
    {
        s_logger.Info($"Closing connection for client - {ctx}");
        s_logger.Trace(exception);

        ctx.CloseAsync();
    }
}
