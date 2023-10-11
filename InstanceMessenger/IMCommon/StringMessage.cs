using System.Text;
using DotNetty.Buffers;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;

namespace IMCommon;

public struct Message
{
    public string Header { get; set; }
    public string Contents { get; set; }
    public byte[] ContentsBytes { get; set; }
}

public class StringMessage : BaseRequestEntity<StringMessage>
{
    static private readonly IInternalLogger s_logger = LoggerHelper.GetLogger<StringMessage>();
    public string Contents { get; private set; } = string.Empty;

    public StringMessage() : base() { }

    public StringMessage SetContents(string contents)
    {
        Contents = contents;
        return this;
    }

    public StringMessage SetRefIdAndName(LoginUserInfo loginUserInfo)
    {
        SetRefId(loginUserInfo.Id!);
        SetRefName(loginUserInfo.Name!);
        return this;
    }

    public Message MakeMessage()
    {
        var sbHeader = new StringBuilder();
        sbHeader.Append('=').Append(ByteBufferHelper.STR_CRLF);

        //Action
        sbHeader.Append(Action.ToString()).Append(ByteBufferHelper.STR_CRLF);

        //Length
        int length = 0;
        byte[] contentsBytes = ""u8.ToArray();
        if (!string.IsNullOrEmpty(Contents))
        {
            contentsBytes = Encoding.UTF8.GetBytes(Contents);
            length = contentsBytes.Length;
        }
        sbHeader.Append($"length:{length}").Append(ByteBufferHelper.STR_CRLF);

        Headers.Keys.Where(e => !e.Equals("length")).ToList().ForEach(e =>
        {
            sbHeader.Append($"{e}:{Headers[e]}").Append(ByteBufferHelper.STR_CRLF);
        });

        //헤더 끝
        sbHeader.Append('=').Append(ByteBufferHelper.STR_CRLF);

        var header = sbHeader.ToString();
        return new Message()
        {
            Header = header,
            Contents = Contents,
            ContentsBytes = contentsBytes
        };
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("== HEADER ==");

        var etor = Headers.GetEnumerator();
        while (etor.MoveNext())
        {
            sb.AppendLine($"{etor.Current.Key}:{etor.Current.Value}");
        }

        sb.AppendLine($"Action:{Action}");
        sb.AppendLine($"RefId:{RefId}");
        sb.AppendLine($"RefName:{RefName}");
        sb.AppendLine($"Length:{Length}");
        sb.AppendLine($"Contents:{Contents}");
        return sb.ToString();
    }

    public override IByteBuffer ToByteBuffer()
    {
        var message = MakeMessage();

        // var sb = new StringBuilder();
        // var mesageBytes = Encoding.UTF8.GetBytes(message.Header);
        // foreach(var b in mesageBytes)
        // {
        //     sb.AppendFormat("{0:x2}", b);
        // }

        // s_logger.Info($"Header hex:{sb} length:{mesageBytes.Length}");

        var result = ByteBufferUtil.WriteUtf8(PooledByteBufferAllocator.Default, message.Header);
        if (message.ContentsBytes != null)
        {
            result.WriteBytes(message.ContentsBytes);
        }

        return result;
    }

    public class Builder
    {
        private LoginUserInfo _loginUserInfo = null;

        private E_ACTION _action;
        private string _contents = string.Empty;
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

        public Builder SetContents(string contents)
        {
            _contents = contents;
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

        public StringMessage Build()
        {
            var message = new StringMessage();
            message.SetAction(_action);
            message.SetContents(_contents);

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
            sb.AppendLine($"Contents:{_contents}");
            
            var etor = _headers.GetEnumerator();
            while (etor.MoveNext())
            {
                sb.AppendLine($"{etor.Current.Key}:{etor.Current.Value}");
            }

            return sb.ToString();
        }
    }
}
