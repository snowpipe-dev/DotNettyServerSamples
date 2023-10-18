using DotNetty.Transport.Channels;

namespace IMClient;

public class MyLoginInfo
{
    public static readonly MyLoginInfo INSTANCE = new();

    public string Id { get; set; }
    public string Name { get; set; }
    public string Nickname { get; set; }
    public IChannel Channel { get; set; }
    public string RoomName { get; set; }
    
    public bool Active => Channel != null && Channel.Active;
    public bool InActive => !Active;

    private MyLoginInfo() { }
}
