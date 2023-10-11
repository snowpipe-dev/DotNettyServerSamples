using DotNetty.Transport.Channels;

namespace IMCommon;

public class LoginUserInfo
{
    public string? Id { get; private set; }
	public string? Name { get; private set; }
    public string? Nickname { get; private set; }
    public IChannel Channel { get; private set; }

    public LoginUserInfo(string? id, string? name, string? nickname)
    {
        Id = id;
        Name = name;
        Nickname = nickname;
    }

    public LoginUserInfo(string? id, string? name, string? nickname, IChannel channel)
    {
        Id = id;
        Name = name;
        Nickname = nickname;
        Channel = channel;
    }

    public string Desc()
    {
        return $"[{Id}] {Name}";
    }

    public override string ToString()
    {
        return Id;
    }

    static public LoginUserInfo Create(IChannelHandlerContext ctx)
    {
        return ChannelHelper.Create(ctx).LoginUserInfo;
    }

    static public LoginUserInfo Create(IChannel channel)
    {
        return ChannelHelper.Create(channel).LoginUserInfo;
    }
}
