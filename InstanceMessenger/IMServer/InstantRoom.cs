using System.Collections.Concurrent;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using IMCommon;

namespace IMServer;

public class InstantRoom
{
    private static IInternalLogger s_logger = LoggerHelper.GetLogger<InstantRoom>();
    private readonly object _lockObject = new();
    private readonly List<IChannel> _channelList = new();

    public string RoomName { get; init; }
    public List<string> IdList { get; private set; } = new();

    public InstantRoom(string roomName)
    {
        RoomName = roomName;
    }

    public bool Exists(IChannel channel)
    {
        return _channelList.Contains(channel);
    }

    public bool Exists(string id)
    {
        return _channelList
            .Where(e => LoginUserInfo.Create(e).Id!.Equals(id))
            .FirstOrDefault() != null;
    }

    public void Enter(IChannel channel)
    {
        lock (_lockObject)
        {
            _channelList.Add(channel);
            IdList.Add(LoginUserInfo.Create(channel).Id!);
        }
    }

    public void SendEnterMessage(IChannel channel)
    {
        var response = new StringMessage.Builder(channel)
            .SetAction(E_ACTION.ENTER_TO_ROOM)
            .SetRoomName(RoomName)
            .Build();

        SendMessage(response);
    }

    public void SendExitMessage(IChannel channel)
    {
        var response = new StringMessage.Builder(channel)
                    .SetAction(E_ACTION.EXIT_FROM_ROOM)
                    .SetRoomName(RoomName)
                    .Build();

        SendMessage(response);
    }

    public void Exit(IChannel channel)
    {
        lock (_lockObject)
        {
            _channelList.Remove(channel);
            IdList.Remove(LoginUserInfo.Create(channel).Id!);
        }
    }

    public void Talk(StringMessage message)
    {
        SendMessage(message);
    }

    public void SendMessage(StringMessage message)
    {
        if (_channelList.Count == 0)
        {
            return;
        }

        var byteBuffer = message.ToByteBuffer();
        _channelList.ForEach(async e =>
        {
            if (e.Active)
            {
                s_logger.Info($"Channel User[{_channelList.IndexOf(e)}/{_channelList.Count}] {ChannelHelper.Create(e).GetLoginUserInfo().Name}");
                await e.WriteAndFlushAsync(byteBuffer.Duplicate().Retain());
            }
        });

        ReferenceCountUtil.Release(byteBuffer);
    }
}
