using System.Text;
using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;

namespace Othello.Server;

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

public class Coord
{
    public int Y { get; private set; }
    public int X { get; private set; }

    public Coord(int y, int x)
    {
        Y = y;
        X = x;
    }

    static public Coord operator +(Coord a, Coord b) => new(a.Y + b.Y, a.X + b.X);

    public override int GetHashCode()
    {
        int hashCode = 28347;
        hashCode = hashCode * 28347 ^ X.GetHashCode();
        hashCode = hashCode * 28347 ^ Y.GetHashCode();
        return hashCode;
    }

    public override bool Equals(object obj)
    {
        return obj is Coord other &&
            other.X == X &&
            other.Y == Y;
    }

    public override string ToString()
    {
        return $"{Y},{X}";
    }
}

public class Member
{
    public int Index { get; set; }
    public required IChannel Channel { get; set; }
    public E_TILE_TYPE TileType { get; set; } = E_TILE_TYPE.BLANK;
    public E_TILE_TYPE OpponentTileType { get; set; } = E_TILE_TYPE.BLANK;
    public string Name { get; set; } = string.Empty;
    public bool IsHost => Index == 0;

    public async void SendMessage(string message)
    {
        await Channel.WriteAndFlushAsync($"{message}|");
    }
}

public class OthelloServerHandler : SimpleChannelInboundHandler<string>
{
    static readonly IInternalLogger s_logger = InternalLoggerFactory.GetInstance<OthelloServerHandler>();

    static readonly E_TILE_TYPE[,] _tileGrid = new E_TILE_TYPE[8, 8];
    static readonly List<IChannel> s_channels = new();
    static readonly Dictionary<int/*index*/, Member> s_memberDict = new();
    static readonly List<Coord> s_eightDirectionCoordList = new()
    {
        new Coord(-1, -1),
        new Coord(-1, 0),
        new Coord(-1, 1),
        new Coord(0, -1),
        new Coord(0, 1),
        new Coord(1, -1),
        new Coord(1, 0),
        new Coord(1, 1),
    };


    static E_GAME_STATUS s_status = E_GAME_STATUS.IDLE;
    static int s_currentChannelIndex = 0;

    private Member GetMember(IChannel channel)
    {
        var index = s_channels.IndexOf(channel);
        return s_memberDict[index];
    }

    public override void ChannelActive(IChannelHandlerContext ctx)
    {
        s_logger.Info($"Client joined - {ctx}");
        // Console.WriteLine($"Client joined - {ctx}");
        s_channels.Add(ctx.Channel);
    }

    private async void BroadCastAsync(string message)
    {
        foreach (var channel in s_channels)
        {
            await channel.WriteAndFlushAsync($"{message}|");
        }
    }

    private async void BroadCastExceptMe(IChannelHandlerContext ctx, string message)
    {
        foreach (var channel in s_channels)
        {
            if (ctx.Channel == channel)
            {
                continue;
            }

            await channel.WriteAndFlushAsync($"{message}|");
        }
    }

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
        var sb = new StringBuilder();
        sb.AppendLine("/|01234567");
        sb.AppendLine("-+--------");

        for (int i = 0; i < 8; ++i)
        {
            sb.Append($"{i}|");
            for (int j = 0; j < 8; ++j)
            {
                sb.Append((int)_tileGrid[i, j]);
            }
            sb.Append("\n");
        }

        s_logger.Info(sb.ToString());
    }

    private HashSet<Coord> FindFlipTiles(Member member, int coordnateY, int coordnateX)
    {
        var flipCoordnateSet = new HashSet<Coord>();

        #region Top
        //1. Put한 Tile 기준으로 위 쪽으로 나랑 같은 타입이 있는지 체크
        int topY = coordnateY;
        for (int y = coordnateY - 1; y >= 0; --y)
        {
            if (_tileGrid[y, coordnateX] == E_TILE_TYPE.BLANK)
            {
                //BLANK 이면 원위치하고 break
                topY = coordnateY;
                break;
            }
            else if (_tileGrid[y, coordnateX] == member.TileType)
            {
                //자기와 같은 타일이면 그전 위치에서 스탑
                topY = y + 1;
                break;
            }

            //그외에는 계속 위로 스캔
        }
        #endregion

        #region Bottom
        //2. Put한 Tile 기준으로 아래 쪽으로 나랑 같은 타입이 있는지 체크
        int bottomY = coordnateY;
        for (int y = coordnateY + 1; y < 8; ++y)
        {
            if (_tileGrid[y, coordnateX] == E_TILE_TYPE.BLANK)
            {
                //BLANK 이면 원위치하고 break
                bottomY = coordnateY;
                break;
            }
            else if (_tileGrid[y, coordnateX] == member.TileType)
            {
                //자기와 같은 타일이면 그전 위치에서 스탑
                bottomY = y - 1;
                break;
            }

            //그외에는 계속 아래로 스캔
        }
        #endregion
        //3. topY ~ bottomY 사이의 타일을 모두 바꾼다.
        for (int y = topY; y <= bottomY; ++y)
        {
            if (_tileGrid[y, coordnateX] == member.OpponentTileType)
            {
                _tileGrid[y, coordnateX] = member.TileType;
                flipCoordnateSet.Add(new Coord(y, coordnateX));
            }
        }

        #region Left
        //4. Put한 Tile 기준으로 왼쪽으로 나랑 같은 타입이 있는지 체크
        int leftX = coordnateX;
        for (int x = coordnateX - 1; x >= 0; --x)
        {
            if (_tileGrid[coordnateY, x] == E_TILE_TYPE.BLANK)
            {
                //BLANK 이면 원위치하고 break
                leftX = coordnateX;
                break;
            }
            else if (_tileGrid[coordnateY, x] == member.TileType)
            {
                //자기와 같은 타일이면 그전 위치에서 스탑
                leftX = x + 1;
                break;
            }

            //그외에는 계속 왼쪽으로 스캔
        }
        #endregion

        #region Right
        //5. Put한 Tile 기준으로 오른쪽으로 나랑 같은 타입이 있는지 체크
        int rightX = coordnateX;
        for (int x = coordnateX + 1; x < 8; ++x)
        {
            if (_tileGrid[coordnateY, x] == E_TILE_TYPE.BLANK)
            {
                //BLANK 이면 원위치하고 break
                rightX = coordnateX;
                break;
            }
            else if (_tileGrid[coordnateY, x] == member.TileType)
            {
                //자기와 같은 타일이면 그전 위치에서 스탑
                rightX = x - 1;
                break;
            }

            //그외에는 계속 오른쪽으로 스캔
        }
        #endregion

        //6. topY ~ bottomY 사이의 타일을 모두 바꾼다.
        for (int x = leftX; x <= rightX; ++x)
        {
            if (_tileGrid[coordnateY, x] == member.OpponentTileType)
            {
                _tileGrid[coordnateY, x] = member.TileType;
                flipCoordnateSet.Add(new Coord(coordnateY, x));
            }
        }

        #region TopLeft
        int topLeftX = coordnateX;
        int topLeftY = coordnateY;

        for (int x = coordnateX - 1, y = coordnateY - 1; x >= 0 && y >= 0; --x, --y)
        {
            if (_tileGrid[y, x] == E_TILE_TYPE.BLANK)
            {
                //BLANK 이면 원위치하고 break
                topLeftX = coordnateX;
                topLeftY = coordnateY;
                break;
            }
            else if (_tileGrid[y, x] == member.TileType)
            {
                //자기와 같은 타일이면 그전 위치에서 스탑
                topLeftX = x + 1;
                topLeftY = y + 1;
                break;
            }
        }
        #endregion

        #region BottomRight
        int bottomRightX = coordnateX;
        int bottomRightY = coordnateY;

        for (int x = coordnateX + 1, y = coordnateY + 1; x < 8 && y < 8; ++x, ++y)
        {
            if (_tileGrid[y, x] == E_TILE_TYPE.BLANK)
            {
                //BLANK 이면 원위치하고 break
                bottomRightX = coordnateX;
                bottomRightY = coordnateY;
                break;
            }
            else if (_tileGrid[y, x] == member.TileType)
            {
                //자기와 같은 타일이면 그전 위치에서 스탑
                bottomRightX = x - 1;
                bottomRightY = y - 1;
                break;
            }
        }
        #endregion

        //6. topY ~ bottomY 사이의 타일을 모두 바꾼다.
        for (int x = topLeftX, y = topLeftY; x <= bottomRightX && y <= bottomRightY; ++x, ++y)
        {
            if (_tileGrid[y, x] == member.OpponentTileType)
            {
                _tileGrid[y, x] = member.TileType;
                flipCoordnateSet.Add(new Coord(y, x));
            }
        }

        #region TopRight
        int topRightX = coordnateX;
        int topRightY = coordnateY;

        for (int x = coordnateX + 1, y = coordnateY - 1; x < 8 && y >= 0; ++x, --y)
        {
            if (_tileGrid[y, x] == E_TILE_TYPE.BLANK)
            {
                //BLANK 이면 원위치하고 break
                topRightX = coordnateX;
                topRightY = coordnateY;
                break;
            }
            else if (_tileGrid[y, x] == member.TileType)
            {
                //자기와 같은 타일이면 그전 위치에서 스탑
                topRightX = x - 1;
                topRightY = y + 1;
                break;
            }
        }
        #endregion

        #region BottomLeft
        int bottomLeftX = coordnateX;
        int bottomLeftY = coordnateY;

        for (int x = coordnateX - 1, y = coordnateY + 1; x >= 0 && y < 8; --x, ++y)
        {
            if (_tileGrid[y, x] == E_TILE_TYPE.BLANK)
            {
                //BLANK 이면 원위치하고 break
                bottomLeftX = coordnateX;
                bottomLeftY = coordnateY;
                break;
            }
            else if (_tileGrid[y, x] == member.TileType)
            {
                //자기와 같은 타일이면 그전 위치에서 스탑
                bottomLeftX = x + 1;
                bottomLeftY = y - 1;
                break;
            }
        }
        #endregion

        //6. topY ~ bottomY 사이의 타일을 모두 바꾼다.
        for (int x = topRightX, y = topRightY; x >= bottomLeftX && y <= bottomLeftY; --x, ++y)
        {
            if (_tileGrid[y, x] == member.OpponentTileType)
            {
                _tileGrid[y, x] = member.TileType;
                flipCoordnateSet.Add(new Coord(y, x));
            }
        }

        return flipCoordnateSet;
    }

    private async void DoCommandJoin(IChannelHandlerContext ctx, string name)
    {
        var index = s_channels.IndexOf(ctx.Channel);
        s_memberDict[index] = new Member()
        {
            Index = index,
            Channel = ctx.Channel,
            TileType = index == 0 ? E_TILE_TYPE.WHITE : E_TILE_TYPE.BLACK,
            OpponentTileType = index == 0 ? E_TILE_TYPE.BLACK : E_TILE_TYPE.WHITE,
            Name = name
        };

        GetMember(ctx.Channel).SendMessage($"CompleteJoin::{index}");
        BroadCastExceptMe(ctx, $"Message::Join {name}");

        var names = string.Join(',', s_memberDict.Values.Select(e => e.Name));
        BroadCastAsync($"Members::Current members are {names}.");
    }

    private async void DoCommandPut(IChannelHandlerContext ctx, string coordnate)
    {
        var member = GetMember(ctx.Channel);
        if (member.Index != s_currentChannelIndex)
        {
            member.SendMessage("FailPut::NotYourTurn");
            return;
        }

        var coordnates = coordnate.Split(",");
        var coordnateY = int.Parse(coordnates[0]);
        var coordnateX = int.Parse(coordnates[1]);

        if (coordnateX < 0 || coordnateX > 8 || coordnateY < 0 || coordnateY > 8 ||
            _tileGrid[coordnateY, coordnateX] != E_TILE_TYPE.BLANK)
        {
            member.SendMessage("FailPut::InvalidCoordnates");
            return;
        }

        var availableCoord = false;
        var centerCoord = new Coord(coordnateY, coordnateX);
        foreach (var dirCoord in s_eightDirectionCoordList)
        {
            var newDirCoord = centerCoord + dirCoord;
            if (newDirCoord.Y < 0 || newDirCoord.X < 0 ||
                newDirCoord.Y > 7 || newDirCoord.X > 7)
            {
                continue;
            }

            var isOpponentTile = _tileGrid[newDirCoord.Y, newDirCoord.X] == member.OpponentTileType;
            availableCoord |= isOpponentTile;
        }

        if (!availableCoord)
        {
            member.SendMessage("FailPut::InvalidCoordnates");
            return;
        }

        _tileGrid[coordnateY, coordnateX] = member.TileType;

        var flipTiles = FindFlipTiles(member, coordnateY, coordnateX);
        if (flipTiles.Count > 0)
        {
            BroadCastAsync($"SuccessPut::{coordnate},{member.TileType}");
            var message = string.Join('$', flipTiles);
            BroadCastAsync($"FlipTiles::{member.TileType}#{message}");

            //Todo flip tiles
            s_currentChannelIndex = s_currentChannelIndex == 0 ? 1 : 0;
            BroadCastAsync($"NextTurn::{s_currentChannelIndex}");
        }
        else
        {
            //뒤집어야할 타일이 없으므로 기존 타일로 변경하고 실패를 알린다.
            _tileGrid[coordnateY, coordnateX] = E_TILE_TYPE.BLANK;
            member.SendMessage("FailPut::InvalidCoordnates");
            return;
        }

    }

    private async void DoCommandStart(IChannelHandlerContext ctx)
    {
        var member = GetMember(ctx.Channel);
        if (s_status == E_GAME_STATUS.IN_GAME)
        {
            member.SendMessage("Message::The game has already started.");
            return;
        }

        if (!member.IsHost)
        {
            member.SendMessage("Message::You're not the host.");
            return;
        }

        if (s_channels.Count != 2)
        {
            member.SendMessage("Message::There must be 2 members.");
            return;
        }

        InitGrid();

        s_status = E_GAME_STATUS.IN_GAME;
        BroadCastAsync("StartGame");

        s_memberDict.Values.ToList().ForEach(e => e.SendMessage($"TileType::{e.TileType}"));
        BroadCastAsync($"NextTurn::{s_currentChannelIndex}");
    }

    private void DoCommandExit(IChannelHandlerContext ctx)
    {
        var member = GetMember(ctx.Channel);
        if (s_status == E_GAME_STATUS.IDLE)
        {
            member.SendMessage("Message::The game is idle.");
            return;
        }

        s_status = E_GAME_STATUS.IDLE;

        member.SendMessage("Message::You are exit now.");

        BroadCastExceptMe(ctx, "Message::Your opponent exit now.");
    }

    protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
    {
        var messages = msg.Split("::");

        string commandKey = messages[0];
        string commandValue = string.Empty;

        if (messages.Length > 1)
        {
            commandValue = messages[1];
        }

        if (commandKey.Equals("Join"))
        {
            DoCommandJoin(ctx, commandValue);
            return;
        }

        if (commandKey.Equals("Start"))
        {
            DoCommandStart(ctx);
            return;
        }

        if (commandKey.Equals("Exit"))
        {
            DoCommandExit(ctx);
            return;
        }

        if (commandKey.Equals("Put"))
        {
            DoCommandPut(ctx, commandValue);
        }
    }
}
