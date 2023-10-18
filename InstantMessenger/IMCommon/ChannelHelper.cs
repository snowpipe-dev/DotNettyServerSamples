using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace IMCommon;

public class ChannelHelper
{
    private static readonly string USER_INFO = "userInfo";

    private IChannel _channel;

    public LoginUserInfo LoginUserInfo => (LoginUserInfo)Get(USER_INFO);

    private ChannelHelper(IChannel channel)
    {
        _channel = channel;
    }

    public ChannelHelper Attach(string key, object value)
    {
        var attributeKey = AttributeKey<object>.ValueOf(key);
        _channel.GetAttribute(attributeKey).Set(value);

        return this;
    }

    public object Get(string key)
    {
        var attributeKey = AttributeKey<object>.ValueOf(key);
        return _channel.GetAttribute(attributeKey).Get();
    }

    public ChannelHelper AttachLoginUserInfo(LoginUserInfo loginUserInfo)
    {
        Attach(USER_INFO, loginUserInfo);
        return this;
    }

    public LoginUserInfo GetLoginUserInfo()
    {
        return (LoginUserInfo)Get(USER_INFO);
    }

    public static ChannelHelper Create(IChannel channel)
    {
        return new ChannelHelper(channel);
    }

    public static ChannelHelper Create(IChannelHandlerContext ctx)
    {
        return new ChannelHelper(ctx.Channel);
    }
}
