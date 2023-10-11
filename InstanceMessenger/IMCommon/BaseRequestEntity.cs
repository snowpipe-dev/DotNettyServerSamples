using System.Text;
using DotNetty.Buffers;

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

public abstract class BaseRequestEntity<T> where T : class
{
    static public E_ACTION Decode(string code)
    {
        return Enum.Parse<E_ACTION>(code);
    }

    abstract public IByteBuffer ToByteBuffer();

    public E_ACTION Action { get; protected set; }
    public Dictionary<string, string> Headers { get; protected set; } = new();
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

    public BaseRequestEntity()
    { }

    public BaseRequestEntity(E_ACTION code)
    {
        Action = code;
    }

    public void SetAction(E_ACTION code)
    {
        Action = code;
    }

    public void SetAction(string code)
    {
        Action = Decode(code);
    }

    public T SetRefId(string? refId)
    {
        Headers.Add("refId", refId!);
        return (T)Convert.ChangeType(this, typeof(T));
    }

    public T SetRefName(string? refName)
    {
        Headers.Add("refName", refName!);
        return (T)Convert.ChangeType(this, typeof(T));
    }

    public T SetLength(int length)
    {
        if (Headers.ContainsKey("length"))
        {
            Headers["length"] = length.ToString();
        }
        else
        {
            Headers.Add("length", length.ToString());
        }

        return (T)Convert.ChangeType(this, typeof(T));
    }

    public T AddHeader(string key, string value)
    {
        if (Headers.ContainsKey(key))
        {
            Headers[key] = value;
        }
        else
        {
            Headers.Add(key, value);
        }

        return (T)Convert.ChangeType(this, typeof(T));
    }

    public string GetHeader(string key)
    {
        if (Headers.ContainsKey(key))
        {
            return Headers[key];
        }

        return string.Empty;
    }

    public T AddAllHeaders(Dictionary<string, string> otherHeaders)
    {
        Headers.Append(otherHeaders);

        return (T)Convert.ChangeType(this, typeof(T));
    }
}
