using System.Net;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Chat.Client;

class Program
{
    static async Task RunClientAsync()
    {
        var group = new MultithreadEventLoopGroup();

        var serverIP = IPAddress.Parse("127.0.0.1");
        int serverPort = 10000;

        try
        {
            var bootstrap = new Bootstrap();
            bootstrap
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true) // Do not buffer and send packages right away
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        var pipeline = channel.Pipeline;
                        pipeline.AddLast(new StringDecoder());
                        pipeline.AddLast(new StringEncoder());
                        pipeline.AddLast(new ChatClientHandler());
                    }));

            var bootstrapChannel = await bootstrap.ConnectAsync(serverIP, serverPort);

            Console.WriteLine("Please enter your name: ");
            string? clientName = Console.ReadLine();
            Console.WriteLine($"Welcome {clientName}");

            for (; ; )
            {
                string? message = Console.ReadLine();
                await bootstrapChannel.WriteAndFlushAsync($"[{clientName}] {message}");
                bootstrapChannel.Flush();
            }
        }
        finally
        {
            group.ShutdownGracefullyAsync().Wait(1000);
        }
    }

    static void Main() => RunClientAsync().Wait();
}