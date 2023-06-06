namespace CleaningBot;

public class BotInput
{
    public Cell[,] Map { get; set; }
    public PositionFacing Start { get; set; }
    public List<Command> Commands { get; set; }
    public int Battery { get; set; }
}