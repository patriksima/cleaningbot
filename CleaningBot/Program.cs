// See https://aka.ms/new-console-template for more information

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class Cleaning
{
    private readonly BotInput _botInput;

    private Mode _mode = Mode.Normal;

    private readonly CleaningResult _cleaningResult = new()
    {
        Cleaned = new HashSet<Position>(),
        Visited = new HashSet<Position>(),
        Final = new PositionFacing(0,0),
        Battery = 0
    };

    private readonly int _height;
    private readonly int _width;

    public Cleaning(BotInput botInput)
    {
        _botInput = botInput;

        _height = _botInput.Map.GetLength(0);
        _width = _botInput.Map.GetLength(1);


        _cleaningResult.Battery = _botInput.Battery;
        _cleaningResult.Final.X = _botInput.Start.X;
        _cleaningResult.Final.Y = _botInput.Start.Y;
        _cleaningResult.Final.Facing = _botInput.Start.Facing;
        
        _cleaningResult.Visited.Add(new Position(_botInput.Start.X, _botInput.Start.Y));
    }

    public CleaningResult Perform()
    {
        foreach (var command in _botInput.Commands)
        {
            if (_mode == Mode.Back)
            {
                break;
            }

            switch (command)
            {
                case Command.A:
                    Advance();
                    break;
                case Command.B:
                    Back();
                    break;
                case Command.C:
                    Clean();
                    break;
                case Command.TL:
                    TurnLeft();
                    break;
                case Command.TR:
                    TurnRight();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (IsOutsideLeft)
            {
                _mode = Mode.Back;
                _cleaningResult.Final.X = 0;
                BackStrategy();
            }

            if (IsOutsideTop)
            {
                _mode = Mode.Back;
                _cleaningResult.Final.Y = 0;
                BackStrategy();
            }

            if (IsOutsideRight)
            {
                _mode = Mode.Back;
                _cleaningResult.Final.X = _width - 1;
                BackStrategy();
            }

            if (IsOutsideBottom)
            {
                _mode = Mode.Back;
                _cleaningResult.Final.Y = _height - 1;
                BackStrategy();
            }

            if (IsEmpty || IsCleaned)
            {
                _mode = Mode.Back;
                BackStrategy();
            }
        }

        return _cleaningResult;
    }

    private bool IsNotEnoughEnergy(int consume) => _cleaningResult.Battery - consume < 0;

    private void Clean()
    {
        if (IsNotEnoughEnergy(5))
        {
            _mode = Mode.Back;
            return;
        }

        _cleaningResult.Cleaned.Add(new Position(_cleaningResult.Final.X, _cleaningResult.Final.Y));
        _cleaningResult.Battery -= 5;
    }

    private bool IsOutsideLeft => _cleaningResult.Final.X < 0;
    private bool IsOutsideRight => _cleaningResult.Final.X >= _width;
    private bool IsOutsideTop => _cleaningResult.Final.Y < 0;
    private bool IsOutsideBottom => _cleaningResult.Final.Y >= _height;
    private bool IsOutside => IsOutsideLeft || IsOutsideRight || IsOutsideTop || IsOutsideBottom;
    private bool IsEmpty => _botInput.Map[_cleaningResult.Final.Y, _cleaningResult.Final.X] == Cell.Null;
    private bool IsCleaned => _botInput.Map[_cleaningResult.Final.Y, _cleaningResult.Final.X] == Cell.C;
    private bool IsObstacle => IsOutside || IsEmpty || IsCleaned;

    private void TurnRight()
    {
        if (IsNotEnoughEnergy(1))
        {
            _mode = Mode.Back;
            return;
        }

        _cleaningResult.Battery -= 1;
        
        _cleaningResult.Final.Facing = _cleaningResult.Final.Facing switch
        {
            Direction.N => Direction.E,
            Direction.S => Direction.W,
            Direction.W => Direction.N,
            Direction.E => Direction.S,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void TurnLeft()
    {
        if (IsNotEnoughEnergy(1))
        {
            _mode = Mode.Back;
            return;
        }

        _cleaningResult.Battery -= 1;
        
        _cleaningResult.Final.Facing = _cleaningResult.Final.Facing switch
        {
            Direction.N => Direction.W,
            Direction.S => Direction.E,
            Direction.W => Direction.S,
            Direction.E => Direction.N,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void Back()
    {
        if (IsNotEnoughEnergy(3))
        {
            _mode = Mode.Back;
            return;
        }

        _cleaningResult.Battery -= 3;
        
        switch (_cleaningResult.Final.Facing = _cleaningResult.Final.Facing)
        {
            case Direction.N:
                _cleaningResult.Final.Y += 1;
                break;
            case Direction.S:
                _cleaningResult.Final.Y -= 1;
                break;
            case Direction.W:
                _cleaningResult.Final.X += 1;
                break;
            case Direction.E:
                _cleaningResult.Final.X -= 1;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (!IsObstacle)
        {
            _cleaningResult.Visited.Add(new Position(_cleaningResult.Final.X, _cleaningResult.Final.Y));
        }
    }

    private void Advance()
    {
        if (IsNotEnoughEnergy(2))
        {
            _mode = Mode.Back;
            return;
        }

        _cleaningResult.Battery -= 2;
        
        switch (_cleaningResult.Final.Facing = _cleaningResult.Final.Facing)
        {
            case Direction.N:
                _cleaningResult.Final.Y -= 1;
                break;
            case Direction.S:
                _cleaningResult.Final.Y += 1;
                break;
            case Direction.W:
                _cleaningResult.Final.X -= 1;
                break;
            case Direction.E:
                _cleaningResult.Final.X += 1;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        if (!IsObstacle)
        {
            _cleaningResult.Visited.Add(new Position(_cleaningResult.Final.X, _cleaningResult.Final.Y));
        }
    }

    private bool BackStrategyA()
    {
        TurnRight();
        Advance();
        if (IsObstacle) return false;
        TurnLeft();
        return true;
    }

    private bool BackStrategyB()
    {
        TurnRight();
        Advance();
        if (IsObstacle) return false;
        TurnRight();
        return true;
    }

    private bool BackStrategyC()
    {
        TurnRight();
        Back();
        if (IsObstacle) return false;
        TurnRight();
        Advance();
        if (IsObstacle) return false;
        return true;
    }

    private bool BackStrategyD()
    {
        TurnLeft();
        TurnLeft();
        Advance();
        if (IsObstacle) return false;
        return true;
    }

    private void BackStrategy()
    {
        if (BackStrategyA())
        {
            _mode = Mode.Normal;
            return;
        }

        if (BackStrategyB())
        {
            _mode = Mode.Normal;
            return;
        }

        if (BackStrategyB())
        {
            _mode = Mode.Normal;
            return;
        }

        if (BackStrategyC())
        {
            _mode = Mode.Normal;
            return;
        }

        if (BackStrategyD())
        {
            _mode = Mode.Normal;
            return;
        }
    }
}

public enum Mode
{
    Normal,
    Back
}

public class CleaningResult
{
    public HashSet<Position> Visited { get; set; }
    public HashSet<Position> Cleaned { get; set; }
    public PositionFacing Final { get; set; }
    public int Battery { get; set; }
}

public class Position : IEquatable<Position>
{
    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; set; }
    public int Y { get; set; }

    public bool Equals(Position? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Position)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}

public class PositionFacing : Position
{
    [JsonConverter(typeof(StringEnumConverter))]
    public Direction Facing { get; set; }

    public PositionFacing(int x, int y) : base(x, y)
    {
    }
}

internal class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine($"application <input.json> <output.json>");
            Environment.Exit(1);
        }

        var json = File.ReadAllText(args[0]);
        /*
        var json = """
{
    "map": [
        ["S", "S", "S", "S"],
        ["S", "S", "C", "S"],
        ["S", "S", "S", "S"],
        ["S", "null", "S", "S"]
        ],
    "start": {"X": 3, "Y": 0, "facing": "N"},
    "commands": [ "TL","A","C","A","C","TR","A","C"],
    "battery": 80
}
""";*/


        var botInput = JsonConvert.DeserializeObject<BotInput>(json);
        var cleaning = new Cleaning(botInput);
        var output = cleaning.Perform();
        
        //Console.WriteLine(JsonConvert.SerializeObject(output));
        
        File.WriteAllText(args[1], JsonConvert.SerializeObject(output));
    }
}

public class BotInput
{
    public Cell[,] Map { get; set; }
    public PositionFacing Start { get; set; }
    public List<Command> Commands { get; set; }
    public int Battery { get; set; }
}

public enum Cell
{
    S,
    C,
    Null
}

public enum Direction
{
    N,
    S,
    E,
    W
}

public enum Command
{
    A,
    B,
    C,
    TL,
    TR
}