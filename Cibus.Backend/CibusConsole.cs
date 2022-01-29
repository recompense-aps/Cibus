using System;
using System.Threading.Tasks;
using System.Reflection;

namespace Cibus.Backend
{
	public static class CibusConsole
	{
		private static List<MethodInfo> commands = new List<MethodInfo>();
		private static List<string> commandInputs = new List<string>();

		public static void Init()
		{
			FindCommands();
		}

		public static void Log(object content, ConsoleColor color = ConsoleColor.White, ConsoleColor backGroundColor = ConsoleColor.Black)
		{
			Console.ForegroundColor = color;
			Console.BackgroundColor = backGroundColor;
			Console.WriteLine(content);
			Console.ResetColor();
		}

		private static void FindCommands()
        {
            var methods = Assembly.GetExecutingAssembly().GetTypes()
                      .SelectMany(t => t.GetMethods())
                      .Where(m => m.GetCustomAttributes(typeof(CibusCommand), false).Length > 0)
                      .ToList();
            commands = methods;
            Log($"Found {commands.Count} commands.", ConsoleColor.Green);
        }

		public async static Task ProcessCommands(string input, bool verboseErrors)
        {
            if (input == "last")
            {
                input = commandInputs.Last();
            }
            else commandInputs.Add(input);

            foreach (var command in commands)
            {
                var (shouldNotRun, processor) = InputProcessor.Process(command.Name, input);

                if(!shouldNotRun)
                {
                    Log($"Running command [{command.Name}]...", ConsoleColor.Green);
                    try
                    {
                        var task = (Task)command.Invoke(null, new object[] { processor });
                        await task;
                    }
                    catch(Exception e)
                    {
                        if (verboseErrors)
                            Log(e, ConsoleColor.Red);
                        else Log(e.Message, ConsoleColor.Red);
                    }
                    Log("Done", ConsoleColor.Green);
                }
            }
        }

		[CibusCommand]
		public static async Task CreateRecipe(InputProcessor processor)
		{
			if (processor.Help("Creates a new recipe and saves to database",
				("n", "name of the new recipe", true)
			))
			{
				var savedRecipe = await DAL.DoAsync(async context => {
					var recipe = await context.GetOrCreateRecipe();

					recipe.Name = processor.Switch("n").Default("New Recipe").String;

					return recipe;
				});
			}
		}

		[CibusCommand]
		public static async Task ListAllRecipeNames(InputProcessor processor)
		{
			await Task.CompletedTask;
			if (processor.Help("Lists the name of every recipe in the database"))
			{
				DAL.Do(context => {
					Log(string.Join("\n", context.GetAllRecipeData().Select(x => x.Name)));
				});
			}
		}

		[CibusCommand]
		public static async Task ParseRecipe(InputProcessor processor)
		{
			if (processor.Help("Generates a recipe based on the given url",
				("u", "url for the recipe", true)
			))
			{
				var url = processor.Switch("u").Default("https://www.allrecipes.com/recipe/220751/quick-chicken-piccata/").String;
				await Parser.Factory(url).Parse();
			}
		}
	}

	public class CibusCommand : System.Attribute {  }
}