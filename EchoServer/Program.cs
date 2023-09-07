using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DevCrews.EchoServer;
/*
 * TCP 서버를 어떻게 만드는지 보여준다.
 *  - StringEncoder 와 StringDecoder를 통해서 byte[]를 문자열로 인코드/디코드.
 *  - ChannelHandler들을 ChannelPipeline에 연결.
 *  - ChatServerHandler는 레퍼런스 카운트 같은 DotNetty의 특정 기능들을 캡슐화한 SimpleChannelInboundHandler를 구현한다.
 *  - HasUpperCharsServerHandler 는 ChannelHandlerAdapter 구현하고, FireChannelReady를 통한 ChannelPipeline의 다음 핸들러로 전달된 메시지를 사용한다.
 *  - CountCharsServerHandler 는 출력 스트림에 쓰기하는 ChannelReadComplete 사용한다.
 */

class Program
{
    static async Task RunServerAsync()
    {
        var logLevel = LogLevel.INFO;

        // var configureNamedOptions = new ConfigureNamedOptions<ConsoleLoggerOptions>("", null);
        // var optionsFactory = new OptionsFactory<ConsoleLoggerOptions>(new[] { configureNamedOptions }, 
        //     Enumerable.Empty<IPostConfigureOptions<ConsoleLoggerOptions>>());
        // var optionsMonitor = new OptionsMonitor<ConsoleLoggerOptions>(optionsFactory, 
        //     Enumerable.Empty<IOptionsChangeTokenSource<ConsoleLoggerOptions>>(), 
        //     new OptionsCache<ConsoleLoggerOptions>());
        // var loggerProvider = new ConsoleLoggerProvider(optionsMonitor);
        // InternalLoggerFactory.DefaultFactory.AddProvider(loggerProvider);

        var serverPort = 8080;

        var bossGroup = new MultithreadEventLoopGroup(1); //  accepts an incoming connection
        var workerGroup = new MultithreadEventLoopGroup();
        // handles the traffic of the accepted connection once the boss accepts the connection 
        // and registers the accepted connection to the worker

        var encoder = new StringEncoder();
        var decoder = new StringDecoder();

        var echoServerHandler = new EchoServerHandler();

        try
        {
            var bootstrap = new ServerBootstrap();

            bootstrap
                .Group(bossGroup, workerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 100) // maximum queue length for incoming connection
                .Handler(new LoggingHandler(logLevel))
                .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;

                    // handler evaluation order is 1, 2, 3, 4, 5 for inbound data and 5, 4, 3, 2, 1 for outbound

                    // The DelimiterBasedFrameDecoder splits the data stream into frames (individual messages e.g. strings ) 
                    // and do not allow requests longer than n chars.
                    // It is required to use a frame decoder suchs as DelimiterBasedFrameDecoder 
                    // or LineBasedFrameDecoder before the StringDecoder.
                    pipeline.AddLast("1", new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
                    pipeline.AddLast("2", encoder);
                    pipeline.AddLast("3", decoder);
                    pipeline.AddLast("4", new CountCharsServerHandler());
                    //pipeline.AddLast("4½", new HasUpperCharsServerHandler());
                    pipeline.AddLast("5", echoServerHandler);
                }));

            IChannel bootstrapChannel = await bootstrap.BindAsync(serverPort);

            Console.WriteLine("Let us test the server in a command prompt");
            Console.WriteLine($"\n telnet localhost {serverPort}");
            Console.ReadLine();

            await bootstrapChannel.CloseAsync();
        }
        finally
        {
            Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
        }
    }

    static void Main(string[] args) => RunServerAsync().Wait();
}
