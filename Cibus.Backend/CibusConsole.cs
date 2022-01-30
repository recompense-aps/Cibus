using System;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json;

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
				var recipe = await Parser.Factory(url).Parse();
				Log(JsonConvert.SerializeObject(recipe, Formatting.Indented));
			}
		}

		[CibusCommand]
		public static async Task ParseRecipeGeneric(InputProcessor processor)
		{
			if (processor.Help("Generates a recipe based on the given url",
				("u", "url for the recipe", true)
			))
			{
				var url = processor.Switch("u").Default("https://www.allrecipes.com/recipe/220751/quick-chicken-piccata/").String;
				var recipe = await (new GenericParser(url)).Parse();
				var json = JsonConvert.SerializeObject(recipe, Formatting.Indented);
				Log(json);
				File.WriteAllText("outtest.txt", json);
			}
		}

		[CibusCommand]
		public static async Task ParseRecipeBatch(InputProcessor processor)
		{
			if (processor.Help("Generates a recipe based on the given url",
				("p", "path to json batch", true)
			))
			{
				var path = processor.Switch("p").Default("generic-batch-test-in.json").String;
				var urls = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(path));
				var recipes = new List<RecipeData>();

				foreach(string url in urls ?? new List<string>())
				{
					var parser = Parser.Factory(url) as GenericParser;
					var recipe = await parser.Parse();

					if (parser.IsRecipe)
					{
						recipes.Add(recipe);
						Log($"Parsed (score: {Math.Round(parser.RecipeMatchScore, 2)}) {url}", ConsoleColor.Green);
					}
					else
					{
						Log($"Not a recipe (score: {Math.Round(parser.RecipeMatchScore, 2)}) {url}", ConsoleColor.Red);
					}
					
				}

				await File.WriteAllTextAsync("batch-out.json", JsonConvert.SerializeObject(recipes, Formatting.Indented));
			}
		}
		
	}

	public class CibusCommand : System.Attribute {  }
}