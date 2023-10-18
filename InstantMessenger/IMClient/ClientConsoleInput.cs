using DotNetty.Transport.Channels;
using IMCommon;

namespace IMClient;

public class ClientConsoleInput
{
    public static ClientConsoleInput INSTANCE = new();
    public bool IsInitialized { get; private set; }

    private IChannel? _channel;

    private ClientConsoleInput() { }

    public void SetChannel(IChannel channel)
    {
        _channel = channel;
        IsInitialized = true;
    }

    public async Task<bool> RunAsync()
    {
        if (_channel == null)
        {
            return true;
        }

        var readLine = Console.ReadLine();
        if (string.IsNullOrEmpty(readLine))
        {
            return true;
        }

        if (!_channel!.Active)
        {
            return false;
        }

        var pos = readLine.IndexOf(":");
        var action = E_ACTION.TALK_MESSAGE;
        var body = readLine;

        if (pos > -1)
        {
            var actionStr = readLine[..pos];
            action = Enum.Parse<E_ACTION>(actionStr);
            body = readLine[(pos + 1)..];
        }

        var request = new MessagePacket.Builder(_channel)
            .SetAction(action)
            .SetBody(body)
            .Build();

        await _channel.WriteAndFlushAsync(request);
        return true;
    }
}
