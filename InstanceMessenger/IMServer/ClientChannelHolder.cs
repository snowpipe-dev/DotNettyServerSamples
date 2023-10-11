using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using IMCommon;

namespace IMServer;

public class ClientChannelHolder
{
    class OldAndNewRoomChannels
    {
        public List<IChannel> OldList { get; set; }
        public List<IChannel> NewList { get; set; }
        public string OldRoomName { get; set; }
        public string NewRoomName { get; set; }
        public bool IsRoomListChanged { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"OldRoomName:[{OldRoomName}] OldList:[{OldList?.Count}]");
            sb.AppendLine($"NewRoomName:[{NewRoomName}] NewList:[{NewList?.Count}]");
            sb.AppendLine($"IsRoomListChanged:[{IsRoomListChanged}]");

            return sb.ToString();
        }
    }

    static private readonly IInternalLogger s_logger = LoggerHelper.GetLogger<ClientChannelHolder>();

    static public readonly ClientChannelHolder INSTANCE = new();

    private readonly object _roomLock = new();

    private ConcurrentDictionary<string, List<IChannel>> _roomToChannelList = new();
    private ConcurrentDictionary<IChannel, string> _channelToRoom = new();

    public List<string> RoomList => _roomToChannelList.Keys.ToList();

    private OldAndNewRoomChannels UpdateChannelRoom(IChannel channel, string roomName)
    {
        // roomName이 null이면, logout으로 간주함.
        var result = new OldAndNewRoomChannels
        {
            NewRoomName = roomName
        };

        string oldRoomName;
        if (_channelToRoom.TryGetValue(channel, out oldRoomName))
        {
            if (!string.IsNullOrEmpty(oldRoomName) && !_roomToChannelList.ContainsKey(oldRoomName))
            {
                result.IsRoomListChanged = true;
            }
        }

        if (!string.IsNullOrEmpty(roomName) && !_roomToChannelList.ContainsKey(roomName))
        {
            result.IsRoomListChanged = true;
        }

        result.OldRoomName = oldRoomName;

        List<IChannel> oldRoomChannelList = null;
        if (oldRoomName != null)
        {
            oldRoomChannelList = _roomToChannelList[oldRoomName];
            if (oldRoomChannelList == null)
            {
                RefreshAllRooms();
                result.IsRoomListChanged = true;
                result.NewList = _roomToChannelList[roomName];
                return result;
            }

            if (!oldRoomChannelList.Contains(channel))
            {
                RefreshAllRooms();
                result.IsRoomListChanged = true;
                result.NewList = _roomToChannelList[roomName];
                return result;
            }
        }

        if (oldRoomChannelList != null)
        {
            // channel을 지우기 전에 먼저 셋팅
            result.OldList = new List<IChannel>(oldRoomChannelList);
            oldRoomChannelList.Remove(channel);
            s_logger.Info($"{oldRoomName} 방의 채널 수 {_roomToChannelList[oldRoomName].Count}");

            if (!string.IsNullOrEmpty(oldRoomName) && _roomToChannelList[oldRoomName].Count == 0)
            {
                _roomToChannelList.Remove(oldRoomName, out _);
                result.IsRoomListChanged = true;
            }
        }

        if (roomName != null)
        {
            if (_channelToRoom.ContainsKey(channel))
            {
                _channelToRoom[channel] = roomName;
            }
            else
            {
                _channelToRoom.TryAdd(channel, roomName);
            }

            List<IChannel> newRoomChannelList;
            if (!_roomToChannelList.TryGetValue(roomName, out newRoomChannelList) ||
                newRoomChannelList == null)
            {
                newRoomChannelList = new();
                _roomToChannelList.TryAdd(roomName, newRoomChannelList);

                if (!string.IsNullOrEmpty(roomName))
                {
                    result.IsRoomListChanged = true;
                }
            }

            newRoomChannelList.Add(channel);
            result.NewList = newRoomChannelList;
        }

        s_logger.Info($"OldAndNew\n{result}");
        return result;
    }

    private void RefreshAllRooms()
    {
        //logger.info("== 전체 방에 대해서 refresh함.");
        lock (_roomLock)
        {
            // CopyOnWriteArrayList 이기 때문에, 미리 다 만들어 놓았다가 한번에 add를 하자
            var multiDictionary = new ConcurrentDictionary<string, List<IChannel>>();
            var etor = _channelToRoom.GetEnumerator();
            while (etor.MoveNext())
            {
                if (!multiDictionary.ContainsKey(etor.Current.Value))
                {
                    multiDictionary[etor.Current.Value] = new();
                }

                multiDictionary[etor.Current.Value].Add(etor.Current.Key);
            }

            _roomToChannelList.Clear();

            var keyEtor = multiDictionary.Keys.GetEnumerator();
            while (keyEtor.MoveNext())
            {
                var list = multiDictionary[keyEtor.Current];
                var newList = new List<IChannel>(list);
                _roomToChannelList.TryAdd(keyEtor.Current, newList);
            }
        }
    }

    private void WriteAndFlushAsync(List<IChannel> channelList, StringMessage message)
    {
        if (channelList == null)
        {
            return;
        }

        var byteBuffer = message.ToByteBuffer();
        channelList.ForEach(async e =>
        {
            if (e.Active)
            {
                s_logger.Info($"Channel User[{channelList.IndexOf(e)}/{channelList.Count}] {ChannelHelper.Create(e).GetLoginUserInfo().Name}");
                await e.WriteAndFlushAsync(byteBuffer.Duplicate().Retain());
            }
        });

        ReferenceCountUtil.Release(byteBuffer);
    }

    public void Login(IChannel channel)
    {
        var userInfo = LoginUserInfo.Create(channel);
        // 종료할때 방에서 나오기

        // channel.CloseAsync().ContinueWith(e =>
        // {
        //     var oldAndNew = UpdateChannelRoom(channel, null);
        //     var oldList = oldAndNew.OldList;

        //     // 방사람들한테 전송
        //     if (oldList != null)
        //     {
        //         var message = new StringMessage.Builder(userInfo)
        //             .SetAction(E_ACTION.LOGOUT)
        //             .Build();
        //         WriteAndFlushAsync(oldList, message);
        //     }

        //     var removeRoomName = string.Empty;
        //     _channelToRoom.Remove(channel, out removeRoomName);

        //     var valueEtor = _roomToChannelList.Values.GetEnumerator();
        //     while (valueEtor.MoveNext())
        //     {
        //         var channelList = valueEtor.Current;
        //         channelList.Where(e => e != null && e.Equals(channel)).ToList()
        //             .ForEach(e => channelList.Remove(e));
        //     }
        // });

        // 로비에 들어가기
        var oldAndNew = UpdateChannelRoom(channel, "lobby");
        var newList = oldAndNew.NewList;

        // 방사람들한테 전송
        if (newList != null)
        {
            var message = new StringMessage.Builder(channel)
                .SetAction(E_ACTION.LOGIN)
                .Build();
            WriteAndFlushAsync(newList, message);
        }
    }

    public async void Logout(IChannel channel)
    {
        var oldAndNew = UpdateChannelRoom(channel, null);
        var oldList = oldAndNew.OldList;
        var userInfo = ChannelHelper.Create(channel).GetLoginUserInfo();

        // 방사람들한테 전송
        if (oldList != null)
        {
            var message = new StringMessage.Builder(userInfo)
                .SetAction(E_ACTION.LOGOUT)
                .Build();
            WriteAndFlushAsync(oldList, message);
        }

        _channelToRoom.Remove(channel, out _);
        var channelList = _roomToChannelList.Values.ToList();
        channelList.ForEach(list =>
            list.Where(e => e != null && e.Equals(channel))
                .ToList().ForEach(e => list.Remove(e))
        );

        await channel.CloseAsync();
    }

    public bool ExistsId(string id)
    {
        return _channelToRoom.Keys.Where(e => LoginUserInfo.Create(e).Id.Equals(id)).Count() > 0;
    }

    public List<string> GetIdList(IChannel channel)
    {
        string roomName;
        if (_channelToRoom.TryGetValue(channel, out roomName!))
        {
            if (!string.IsNullOrEmpty(roomName))
            {
                return GetIdList(roomName);
            }
        }

        return new();
    }

    public List<string> GetIdList(string roomName)
    {
        List<IChannel> channelList;
        if (_roomToChannelList.TryGetValue(roomName, out channelList!))
        {
            return channelList.Select(e => LoginUserInfo.Create(e).Id).ToList();
        }

        return new();
    }

    public void EnterToRoom(string roomName, IChannel channel)
    {
        var oldAndNew = UpdateChannelRoom(channel, roomName);

        // 이전방에서 나오기
        var oldList = oldAndNew.OldList;
        if (oldList != null)
        {
            var response = new StringMessage.Builder(channel)
                .SetAction(E_ACTION.EXIT_FROM_ROOM)
                .SetRoomName(oldAndNew.OldRoomName)
                .Build();

            WriteAndFlushAsync(oldList, response);
        }

        // 새로운 방에 들어가기
        var newList = oldAndNew.NewList;
        if (newList != null)
        {
            var response = new StringMessage.Builder(channel)
                .SetAction(E_ACTION.ENTER_TO_ROOM)
                .SetRoomName(roomName)
                .Build();

            WriteAndFlushAsync(newList, response);
        }

        // 방목록이 바뀌었다면
        if (oldAndNew.IsRoomListChanged)
        {
            SendRoomList();
        }
    }

    private void SendRoomList()
    {
        var jsonStr = JsonSerializer.Serialize(RoomList);
        var response = new StringMessage.Builder()
            .SetAction(E_ACTION.ROOM_LIST)
            .SetContents(jsonStr)
            .Build();

        var channelList = _channelToRoom.Keys.ToList();
        WriteAndFlushAsync(channelList, response);
    }

    public void TalkInTheRoom(IChannel channel, string contents)
    {
        string roomName;
        if (!_channelToRoom.TryGetValue(channel, out roomName!))
        {
            s_logger.Warn($"해당 채널이 있는 {roomName}방을 찾지 못했습니다.");
            return;
        }

        if (string.IsNullOrEmpty(roomName))
        {
            s_logger.Warn($"잘못된 방이름입니다.");
            return;
        }

        List<IChannel> channelList;
        if (!_roomToChannelList.TryGetValue(roomName, out channelList!) ||
            channelList == null)
        {
            s_logger.Warn($"방이름[{roomName}]으로 채널목록을 가져올 수 없습니다.");
            return;
        }

        s_logger.Info($"[{roomName}] 방 채널 수:{channelList.Count}");

        var message = new StringMessage.Builder(channel)
                        .SetAction(E_ACTION.TALK_MESSAGE)
                        .SetContents(contents)
                        .Build();
        var byteBuffer = message.ToByteBuffer();

        try
        {
            channelList.ForEach(async channel =>
            {
                await channel.WriteAndFlushAsync(byteBuffer.Duplicate().Retain());
            });
        }
        finally
        {
            ReferenceCountUtil.Release(byteBuffer);
        }
    }

    public void ExitFromRoom(IChannel channel)
    {
        var oldAndNew = UpdateChannelRoom(channel, "lobby");
        var oldList = oldAndNew.OldList;
        if (oldList != null)
        {
            var response = new StringMessage.Builder(channel)
                .SetAction(E_ACTION.EXIT_FROM_ROOM)
                .SetRoomName(oldAndNew.OldRoomName)
                .Build();
            WriteAndFlushAsync(oldList, response);
        }

        // 방목록이 바뀌었다면
        if (oldAndNew.IsRoomListChanged)
        {
            SendRoomList();
        }
    }
}
