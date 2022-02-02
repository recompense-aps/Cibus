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

		public static void Dump(string name, string contents, string fileType = "json")
		{
			File.WriteAllText("Out/" + name, contents);
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
				var parser = Parser.Factory(url) as GenericParser;
				var recipe = await parser.Parse();
				Log($"Score: {parser.RecipeMatchScore}", parser.IsRecipe ? ConsoleColor.Green : ConsoleColor.Red);
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

		[CibusCommand]
		public static async Task Crawl(InputProcessor processor)
		{
			if (processor.Help("Generates a recipe based on the given url",
				("u", "url to crawl", true),
				("k", "url keywords", true),
				("l", "limit recipes", true),
				("o", "path to write results to", true)
			))
			{
				var url = processor.Switch("u").Default("https://www.simplyrecipes.com/roasted-cabbage-steaks-with-garlic-breadcrumbs-recipe-5215499").String;
				var keyWords = processor.Switch("k").Default("").String.Split(',');
				var limit = processor.Switch("l").Default(50).Int;
				var outFile = processor.Switch("o").Default("crawler-out.json").String;
				var startXPath = processor.Switch("x").Default("//*[@id=\"card-list-1_1-0\"]").String; // specific to simplyrecipes
				var crawler = new Crawler(url);

				Log($"Initializing crawling at: {url} | ({crawler.Domain})", ConsoleColor.Green);

				Profiler.Profile("crawl");
				var urls = await crawler.CrawlUrlsMulti(limit, url, 
					url => {
						if (keyWords.Length == 0) return true;
						return keyWords.Any(key => url.Contains(key));
					}, 
					result => {
						Log($"[{result.allUrls?.Count()}/{limit}] Crawled: {result.url} Found: {result.newUrls?.Count()}");
					},
					startXPath
				);
				var time = Profiler.Results("crawl");
				Log($"Finished crawling {limit} urls in {time / 1000} seconds", ConsoleColor.Green);
				File.WriteAllText(outFile, JsonConvert.SerializeObject(urls, Formatting.Indented));
			}
		}

		[CibusCommand]
		public static async Task ParseRecipeCompareAlgorithms(InputProcessor processor)
		{
			if (processor.Help("Generates a recipe based on the given url genericly with each scoring algorithm",
				("u", "url for the recipe", true)
			))
			{
				var url = processor.Switch("u").Default("https://www.allrecipes.com/recipe/220751/quick-chicken-piccata/").String;
				
				var parsers = new List<GenericParser>()
				{
					Parser.Factory<GenericParser>(url)
						.Use(Algorithms.ScoreLineByLine).As(RecipeParsingSection.Ingredients)
						.Use(Algorithms.ScoreLineByLine).As(RecipeParsingSection.Directions)
					,
					Parser.Factory<GenericParser>(url)
						.Use(Algorithms.ScoreByStringLength).As(RecipeParsingSection.Ingredients)
						.Use(Algorithms.ScoreByStringLength).As(RecipeParsingSection.Directions)
				};

				Algorithms.Record();
				var results = await Task.WhenAll(
					parsers.Select(async x => {
						var parsedRecipe = await x.Parse();
						return new {
							x.ScoringAlgorithms.Keys,
							x.RecipeMatchScore,
							parsedRecipe
						};
						
					})
				);

				Dump(processor.CommandName, JsonConvert.SerializeObject(results, Formatting.Indented));
				Dump(processor.CommandName + "-meta", JsonConvert.SerializeObject(Algorithms.Results(), Formatting.Indented));
			}
		}
		
	}

	public class CibusCommand : System.Attribute {  }
}