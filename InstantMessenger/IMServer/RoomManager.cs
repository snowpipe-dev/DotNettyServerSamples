using System.Collections.Concurrent;
using System.Text.Json;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using IMCommon;

namespace IMServer;

public class RoomManager
{
    private static readonly IInternalLogger s_logger = LoggerHelper.GetLogger<RoomManager>();

    private static readonly string LOBBY_ROOM_NAME = "lobby";
    private static readonly object _channelLock = new();

    public static readonly RoomManager INSTANCE = new();

    private ConcurrentDictionary<string/*roomName*/, InstantRoom> _roomDict = new();
    private List<IChannel> _channelList = new();

    public List<string> RoomList => _roomDict.Keys.ToList();

    public void Login(IChannel channel)
    {
        lock (_channelLock)
        {
            _channelList.Add(channel);
        }

        // 로비에 들어가기
        InstantRoom? newRoom;
        if (!_roomDict.TryGetValue(LOBBY_ROOM_NAME, out newRoom))
        {
            newRoom = new(LOBBY_ROOM_NAME);
            _roomDict.TryAdd(LOBBY_ROOM_NAME, newRoom);
        }

        newRoom.Enter(channel);

        var message = new MessagePacket.Builder(channel)
                .SetAction(E_ACTION.LOGIN)
                .Build();
        newRoom.SendMessage(message);
    }

    public async void Logout(IChannel channel)
    {
        var oldInstanceRoom = GetCurrentRoom(channel);
        if (oldInstanceRoom != null)
        {
            var userInfo = ChannelHelper.Create(channel).GetLoginUserInfo();
            var message = new MessagePacket.Builder(userInfo)
                .SetAction(E_ACTION.LOGOUT)
                .Build();
            oldInstanceRoom.SendMessage(message);
            oldInstanceRoom.Exit(channel);

            if (oldInstanceRoom.IdList.Count == 0)
            {
                _roomDict.Remove(oldInstanceRoom.RoomName, out _);
                SendRoomList();
            }
        }

        lock (_channelLock)
        {
            _channelList.Remove(channel);
        }

        await channel.CloseAsync();
    }

    public bool ExistsId(string id)
    {
        return _roomDict.Values.Where(room => room.Exists(id)).FirstOrDefault() != null;
    }

    public List<string> GetIdList(IChannel channel)
    {
        var room = GetCurrentRoom(channel);
        if (room != null)
        {
            return room.IdList;
        }

        return new();
    }

    private InstantRoom? GetCurrentRoom(IChannel channel)
    {
        return _roomDict.Values.Where(e => e.Exists(channel)).FirstOrDefault();
    }

    public InstantRoom EnterToRoom(string roomName, IChannel channel)
    {
        var updatedNewRoom = false;

        var oldInstanceRoom = GetCurrentRoom(channel);
        if (oldInstanceRoom != null)
        {
            oldInstanceRoom.SendExitMessage(channel);
            oldInstanceRoom.Exit(channel);

            if (oldInstanceRoom.IdList.Count == 0)
            {
                _roomDict.Remove(oldInstanceRoom.RoomName, out _);
                updatedNewRoom = true;
            }
        }


        InstantRoom? newRoom;
        if (!_roomDict.TryGetValue(roomName, out newRoom))
        {
            newRoom = new(roomName);
            _roomDict.TryAdd(roomName, newRoom);

            updatedNewRoom = true;
        }

        newRoom.Enter(channel);
        newRoom.SendEnterMessage(channel);

        if (updatedNewRoom)
        {
            SendRoomList();
        }

        return newRoom;
    }

    private void BroadcastMessage(MessagePacket message)
    {
        if (_channelList.Count == 0)
        {
            return;
        }

        var byteBuffer = PacketHelper.MakeByteBuffer(message);
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

    public void SendRoomList()
    {
        var jsonStr = JsonSerializer.Serialize(RoomList);
        var response = new MessagePacket.Builder()
            .SetAction(E_ACTION.ROOM_LIST)
            .SetBody(jsonStr)
            .Build();
        BroadcastMessage(response);
    }

    public void TalkInTheRoom(IChannel channel, string contents)
    {
        var currentRoom = GetCurrentRoom(channel);
        if (currentRoom == null)
        {
            s_logger.Warn($"해당 채널이 있는 방을 찾지 못했습니다.");
            return;
        }

        var message = new MessagePacket.Builder(channel)
                                .SetAction(E_ACTION.TALK_MESSAGE)
                                .SetBody(contents)
                                .Build();
        currentRoom.Talk(message);
    }

    public void ExitFromRoom(IChannel channel)
    {
        EnterToRoom(LOBBY_ROOM_NAME, channel);
    }
}
