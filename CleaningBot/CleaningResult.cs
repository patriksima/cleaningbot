namespace CleaningBot;

public class CleaningResult
{
    public HashSet<Position> Visited { get; set; }
    public HashSet<Position> Cleaned { get; set; }
    public PositionFacing Final { get; set; }
    public int Battery { get; set; }
}