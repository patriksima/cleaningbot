namespace CleaningBot;

public class Roomba
{
    private readonly BotInput _botInput;

    private readonly CleaningResult _cleaningResult = new()
    {
        Cleaned = new HashSet<Position>(),
        Visited = new HashSet<Position>(),
        Final = new PositionFacing(0, 0),
        Battery = 0
    };

    private readonly int _mapHeight;
    private readonly int _mapWidth;

    private Mode _mode = Mode.Normal;

    public Roomba(BotInput botInput)
    {
        _botInput = botInput;

        _mapHeight = _botInput.Map.GetLength(0);
        _mapWidth = _botInput.Map.GetLength(1);

        _cleaningResult.Battery = _botInput.Battery;

        _cleaningResult.Final.X = _botInput.Start.X;
        _cleaningResult.Final.Y = _botInput.Start.Y;
        _cleaningResult.Final.Facing = _botInput.Start.Facing;

        _cleaningResult.Visited.Add(new Position(_botInput.Start.X, _botInput.Start.Y));
    }

    private bool IsOutsideLeft => _cleaningResult.Final.X < 0;
    private bool IsOutsideRight => _cleaningResult.Final.X >= _mapWidth;
    private bool IsOutsideTop => _cleaningResult.Final.Y < 0;
    private bool IsOutsideBottom => _cleaningResult.Final.Y >= _mapHeight;
    private bool IsOutside => IsOutsideLeft || IsOutsideRight || IsOutsideTop || IsOutsideBottom;
    private bool IsEmpty => _botInput.Map[_cleaningResult.Final.Y, _cleaningResult.Final.X] == Cell.Null;
    private bool IsCleaned => _botInput.Map[_cleaningResult.Final.Y, _cleaningResult.Final.X] == Cell.C;
    private bool IsObstacle => IsOutside || IsEmpty || IsCleaned;

    public CleaningResult PerformCleaning()
    {
        foreach (var command in _botInput.Commands)
        {
            if (_mode == Mode.Back) break;

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


            if (IsObstacle)
            {
                _mode = Mode.Back;
                // revert move commands
                switch (command)
                {
                    case Command.A:
                        InternalBack();
                        break;
                    case Command.B:
                        InternalAdvance();
                        break;
                }

                BackoffStrategy();
            }
        }

        return _cleaningResult;
    }

    private bool DrainEnergy(int energy)
    {
        if (_cleaningResult.Battery - energy < 0)
        {
            _mode = Mode.Back;
            return false;
        }

        _cleaningResult.Battery -= energy;
        return true;
    }

    private void Clean()
    {
        if (!DrainEnergy(Energy.Clean)) return;

        LogCleaned();
    }

    private void LogCleaned()
    {
        _cleaningResult.Cleaned.Add(new Position(_cleaningResult.Final.X, _cleaningResult.Final.Y));
    }

    private void TurnRight()
    {
        if (!DrainEnergy(Energy.Turn)) return;

        _cleaningResult.Final.Facing = _cleaningResult.Final.Facing switch
        {
            Facing.N => Facing.E,
            Facing.S => Facing.W,
            Facing.W => Facing.N,
            Facing.E => Facing.S,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void TurnLeft()
    {
        if (!DrainEnergy(Energy.Turn)) return;


        _cleaningResult.Final.Facing = _cleaningResult.Final.Facing switch
        {
            Facing.N => Facing.W,
            Facing.S => Facing.E,
            Facing.W => Facing.S,
            Facing.E => Facing.N,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void Back()
    {
        if (!DrainEnergy(Energy.Back)) return;

        InternalBack();

        LogVisited();
    }

    private void LogVisited()
    {
        if (!IsObstacle) _cleaningResult.Visited.Add(new Position(_cleaningResult.Final.X, _cleaningResult.Final.Y));
    }

    private void InternalBack()
    {
        switch (_cleaningResult.Final.Facing = _cleaningResult.Final.Facing)
        {
            case Facing.N:
                _cleaningResult.Final.Y += 1;
                break;
            case Facing.S:
                _cleaningResult.Final.Y -= 1;
                break;
            case Facing.W:
                _cleaningResult.Final.X += 1;
                break;
            case Facing.E:
                _cleaningResult.Final.X -= 1;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Advance()
    {
        if (!DrainEnergy(Energy.Advance)) return;

        InternalAdvance();

        LogVisited();
    }

    private void InternalAdvance()
    {
        switch (_cleaningResult.Final.Facing = _cleaningResult.Final.Facing)
        {
            case Facing.N:
                _cleaningResult.Final.Y -= 1;
                break;
            case Facing.S:
                _cleaningResult.Final.Y += 1;
                break;
            case Facing.W:
                _cleaningResult.Final.X -= 1;
                break;
            case Facing.E:
                _cleaningResult.Final.X += 1;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool BackoffStrategyA()
    {
        TurnRight();
        Advance();
        if (IsObstacle)
        {
            InternalBack();
            return false;
        }

        TurnLeft();
        return true;
    }

    private bool BackoffStrategyB()
    {
        TurnRight();
        Advance();
        if (IsObstacle)
        {
            InternalBack();
            return false;
        }

        TurnRight();
        return true;
    }

    private bool BackoffStrategyC()
    {
        TurnRight();
        Back();
        if (IsObstacle)
        {
            InternalAdvance();
            return false;
        }

        TurnRight();
        Advance();
        if (IsObstacle)
        {
            InternalBack();
            return false;
        }

        return true;
    }

    private bool BackoffStrategyD()
    {
        TurnLeft();
        TurnLeft();
        Advance();
        if (IsObstacle)
        {
            InternalBack();
            return false;
        }

        return true;
    }

    private void BackoffStrategy()
    {
        if (BackoffStrategyA())
        {
            _mode = Mode.Normal;
            return;
        }

        if (BackoffStrategyB())
        {
            _mode = Mode.Normal;
            return;
        }

        if (BackoffStrategyB())
        {
            _mode = Mode.Normal;
            return;
        }

        if (BackoffStrategyC())
        {
            _mode = Mode.Normal;
            return;
        }

        if (BackoffStrategyD()) _mode = Mode.Normal;
    }
}