using DotNetty.Transport.Channels;
using IMCommon;

namespace IMClient;

public class ClientConsoleInput
{
    static public ClientConsoleInput INSTANCE = new();
    public bool IsInitialized { get; private set; }

    private IChannel _channel;

    private ClientConsoleInput() { }

    public void SetChannel(IChannel channel)
    {
        _channel = channel;
        IsInitialized = true;
    }

    public async Task<bool> RunAsync()
    {
        var readLine = Console.ReadLine();
        if (string.IsNullOrEmpty(readLine))
        {
            return true;
        }

        if (!_channel.Active)
        {
            return false;
        }

        var pos = readLine.IndexOf(":");
        var action = E_ACTION.TALK_MESSAGE;
        var contents = readLine;

        if (pos > -1)
        {
            var actionStr = readLine[..pos];
            action = Enum.Parse<E_ACTION>(actionStr);
            contents = readLine[(pos + 1)..];
        }

        var request = new StringMessage.Builder(_channel)
            .SetAction(action)
            .SetContents(contents)
            .Build();

        await _channel.WriteAndFlushAsync(request);
        return true;
    }
}
