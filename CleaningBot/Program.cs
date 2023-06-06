using Newtonsoft.Json;

namespace CleaningBot;

internal class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("CleaningBot <input.json> <output.json>");
            Environment.Exit(1);
        }

        var json = File.ReadAllText(args[0]);

        var input = JsonConvert.DeserializeObject<BotInput>(json);
        var bot = new Roomba(input);
        var result = bot.PerformCleaning();

        File.WriteAllText(args[1], JsonConvert.SerializeObject(result));
    }
}