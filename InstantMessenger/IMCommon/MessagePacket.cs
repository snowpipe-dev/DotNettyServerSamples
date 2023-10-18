using System.Text;
using DotNetty.Buffers;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;

namespace IMCommon;

public enum E_ACTION
{
    LOGIN = 0, //로그인
    LOGOUT, //로그아웃
    ENTER_TO_ROOM, //방 입장 요청
    EXIT_FROM_ROOM, //방 퇴장 요청
    ROOM_LIST, //방 목록
    TALK_MESSAGE, //메시지 톡 전송
    INFO_MESSAGE, //정보 메시지 전송
    USER_LIST, //방 사람들 목록
    RESPONSE_SUCCESS, //성공
    RESPONSE_FAIL, //실패
}

public struct Message
{
    public string Header { get; set; }
    public string Body { get; set; }
    public byte[] BodyBytes { get; set; }
}

public class MessagePacket
{
    private static readonly IInternalLogger s_logger = LoggerHelper.GetLogger<MessagePacket>();

    public E_ACTION Action { get; protected set; }
    public Dictionary<string, string> Headers { get; protected set; } = new();
    public string Body { get; private set; } = string.Empty;

    public string RefId => GetHeader("refId");
    public string RefName => GetHeader("refName");
    public string RoomName => GetHeader("roomName");
    public int Length
    {
        get
        {
            var length = GetHeader("length");
            return string.IsNullOrEmpty(length) ? 0 : int.Parse(length);
        }
    }

    public MessagePacket() : base() { }

    public void SetAction(E_ACTION code)
    {
        Action = code;
    }

    public void SetAction(string code)
    {
        Action = Enum.Parse<E_ACTION>(code);
    }

    public MessagePacket SetRefId(string? refId)
    {
        Headers.Add("refId", refId!);
        return this;
    }

    public MessagePacket SetRefName(string? refName)
    {
        Headers.Add("refName", refName!);
        return this;
    }

    public MessagePacket SetLength(int length)
    {
        if (Headers.ContainsKey("length"))
        {
            Headers["length"] = length.ToString();
        }
        else
        {
            Headers.Add("length", length.ToString());
        }

        return this;
    }

    public MessagePacket AddHeader(string key, string value)
    {
        if (Headers.ContainsKey(key))
        {
            Headers[key] = value;
        }
        else
        {
            Headers.Add(key, value);
        }

        return this;
    }

    public string GetHeader(string key)
    {
        if (Headers.ContainsKey(key))
        {
            return Headers[key];
        }

        return string.Empty;
    }

    public MessagePacket AddAllHeaders(Dictionary<string, string> otherHeaders)
    {
        Headers.Append(otherHeaders);
        return this;
    }

    public MessagePacket SetBody(string body)
    {
        Body = body;
        return this;
    }

    public MessagePacket SetRefIdAndName(LoginUserInfo loginUserInfo)
    {
        SetRefId(loginUserInfo.Id!);
        SetRefName(loginUserInfo.Name!);
        return this;
    }

    public Message MakeMessage()
    {
        var sbHeader = new StringBuilder();
        sbHeader.Append('=').Append(PacketHelper.STR_CRLF);

        //Action
        sbHeader.Append(Action.ToString()).Append(PacketHelper.STR_CRLF);

        //Length
        int length = 0;
        byte[] bodyBytes = ""u8.ToArray();
        if (!string.IsNullOrEmpty(Body))
        {
            bodyBytes = Encoding.UTF8.GetBytes(Body);
            length = bodyBytes.Length;
        }

        sbHeader.Append($"length:{length}").Append(PacketHelper.STR_CRLF);

        Headers.Keys.Where(e => !e.Equals("length")).ToList().ForEach(e =>
        {
            sbHeader.Append($"{e}:{Headers[e]}").Append(PacketHelper.STR_CRLF);
        });

        //헤더 끝
        sbHeader.Append('=').Append(PacketHelper.STR_CRLF);

        return new Message()
        {
            Header = sbHeader.ToString(),
            Body = Body,
            BodyBytes = bodyBytes
        };
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("== MessagePAcket ==");

        var etor = Headers.GetEnumerator();
        while (etor.MoveNext())
        {
            sb.AppendLine($"{etor.Current.Key}:{etor.Current.Value}");
        }

        sb.AppendLine($"Action:{Action}");
        sb.AppendLine($"RefId:{RefId}");
        sb.AppendLine($"RefName:{RefName}");
        sb.AppendLine($"Length:{Length}");
        sb.AppendLine($"Body:{Body}");
        return sb.ToString();
    }

    public class Builder
    {
        private LoginUserInfo _loginUserInfo = null;

        private E_ACTION _action;
        private string _body = string.Empty;
        private Dictionary<string, string> _headers = null;

        public Builder()
        { }

        public Builder(IChannelHandlerContext ctx)
        {
            _loginUserInfo = LoginUserInfo.Create(ctx);
        }

        public Builder(IChannel channel)
        {
            _loginUserInfo = LoginUserInfo.Create(channel);
        }

        public Builder(LoginUserInfo loginUserInfo)
        {
            _loginUserInfo = loginUserInfo;
        }

        public Builder SetHeader(string key, string? value)
        {
            _headers ??= new();
            _headers.Add(key, value!);
            return this;
        }

        public Builder SetAction(E_ACTION action)
        {
            _action = action;
            return this;
        }

        public Builder SetBody(string body)
        {
            _body = body;
            return this;
        }

        public Builder SetRefId(string? refId)
        {
            SetHeader("refId", refId);
            return this;
        }

        public Builder SetRefName(string? refName)
        {
            SetHeader("refName", refName);
            return this;
        }

        public Builder SetRoomName(string? roomName)
        {
            SetHeader("roomName", roomName);
            return this;
        }

        public Builder SetRefIdAndName(LoginUserInfo userInfo)
        {
            SetRefId(userInfo.Id);
            SetRefName(userInfo.Name);
            return this;
        }

        public MessagePacket Build()
        {
            var message = new MessagePacket();
            message.SetAction(_action);
            message.SetBody(_body);

            if (_loginUserInfo != null)
            {
                message.SetRefId(_loginUserInfo.Id);
                message.SetRefName(_loginUserInfo.Name);
            }

            if (_headers != null)
            {
                message.AddAllHeaders(_headers);
            }

            return message;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Action:{_action}");
            sb.AppendLine($"Body:{_body}");

            var etor = _headers.GetEnumerator();
            while (etor.MoveNext())
            {
                sb.AppendLine($"{etor.Current.Key}:{etor.Current.Value}");
            }

            return sb.ToString();
        }
    }
}
