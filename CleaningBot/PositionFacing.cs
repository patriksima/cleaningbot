using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CleaningBot;

public class PositionFacing : Position
{
    public PositionFacing(int x, int y) : base(x, y)
    {
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public Facing Facing { get; set; }
}