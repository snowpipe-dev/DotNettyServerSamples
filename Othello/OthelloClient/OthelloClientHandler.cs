using DotNetty.Transport.Channels;

namespace Othello.Client;

public enum E_GAME_STATUS
{
    IDLE = 0,
    IN_GAME,
}

public enum E_TILE_TYPE
{
    BLANK = 0,
    WHITE,
    BLACK
}

public class OthelloClientHandler : SimpleChannelInboundHandler<string>
{
    private readonly E_TILE_TYPE[,] _tileGrid = new E_TILE_TYPE[8, 8];
    private readonly E_GAME_STATUS _gameStatus = E_GAME_STATUS.IDLE;
    private E_TILE_TYPE _tileType = E_TILE_TYPE.BLANK;
    private int _memberIndex = -1;
    public bool IsHost => _memberIndex == 0;

    private void InitGrid()
    {
        for (int i = 0; i < 8; ++i)
        {
            for (int j = 0; j < 8; ++j)
            {
                _tileGrid[i, j] = E_TILE_TYPE.BLANK;
            }
        }

        _tileGrid[3, 3] = E_TILE_TYPE.WHITE;
        _tileGrid[3, 4] = E_TILE_TYPE.BLACK;
        _tileGrid[4, 3] = E_TILE_TYPE.BLACK;
        _tileGrid[4, 4] = E_TILE_TYPE.WHITE;
    }

    private void PrintGrid()
    {
        Console.WriteLine("/|01234567");
        Console.WriteLine("-+--------");

        for (int i = 0; i < 8; ++i)
        {
            Console.Write($"{i}|");
            for (int j = 0; j < 8; ++j)
            {
                Console.Write((int)_tileGrid[i, j]);
            }
            Console.WriteLine("");
        }
    }

    private void PrintPutCommand()
    {
        Console.WriteLine("Command Put on coordinate.");
        Console.WriteLine("ex)Put::5,4");
        Console.Write(">");
    }

    private void HandleMessage(string message)
    {
        var messages = message.Split("::");

        string commandKey = messages[0];
        string commandValue = string.Empty;
        if (messages.Length > 1)
        {
            commandValue = messages[1];
        }

        if (commandKey.Equals("CompleteJoin"))
        {
            Console.WriteLine($"Welcome");
            _memberIndex = int.Parse(commandValue);
            return;
        }

        if (commandKey.Equals("StartGame"))
        {
            Console.WriteLine("StartGame");
            InitGrid();
            PrintGrid();
            return;
        }

        if (commandKey.Equals("TileType"))
        {
            var tempTileType = Enum.Parse<E_TILE_TYPE>(commandValue);
            Console.WriteLine($"your TileType:{tempTileType}[{(int)tempTileType}]");
            _tileType = tempTileType;
            return;
        }

        if (commandKey.Equals("SuccessPut"))
        {
            var coordnates = commandValue.Split(",");
            var coordnateY = int.Parse(coordnates[0]);
            var coordnateX = int.Parse(coordnates[1]);
            var tileType = Enum.Parse<E_TILE_TYPE>(coordnates[2]);
            _tileGrid[coordnateY, coordnateX] = tileType;

            PrintGrid();
            return;
        }

        if (commandKey.Equals("FlipTiles"))
        {
            var smalleValues = commandValue.Split("#");
            var flipTileType = Enum.Parse<E_TILE_TYPE>(smalleValues[0]);
            var flipCoordnates = smalleValues[1].Split("$");

            foreach (var flipCoordnate in flipCoordnates)
            {
                var coordnates = flipCoordnate.Split(",");
                var coordnateY = int.Parse(coordnates[0]);
                var coordnateX = int.Parse(coordnates[1]);
                _tileGrid[coordnateY, coordnateX] = flipTileType;
            }

            PrintGrid();
            return;
        }

        if (commandKey.Equals("NextTurn"))
        {
            if (_memberIndex == int.Parse(commandValue))
            {
                PrintPutCommand();
            }
            else
            {
                Console.WriteLine("Wait. This is not your turn.");
            }
            return;
        }

        if (commandKey.Equals("Members"))
        {
            Console.WriteLine(commandValue);
            if (IsHost)
            {
                Console.WriteLine("You can Start game.");
                Console.WriteLine("ex)Start");
                Console.Write(">");
            }

            return;
        }

        if (commandKey.Equals("FailPut"))
        {
            PrintPutCommand();
            return;
        }

        if (commandKey.Equals("Message"))
        {
            Console.WriteLine(commandValue);
            return;
        }
    }

    protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
    {
        Console.WriteLine($"===> Packet[{msg}]");
        var messageList = msg.Split("|");
        foreach (var message in messageList)
        {
            HandleMessage(message);
        }
    }
}
