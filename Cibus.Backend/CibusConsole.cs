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
		private static Dictionary<string,string> aliases = new Dictionary<string, string>();

		public static void Init()
		{
			FindCommands();
			LoadAliases();
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

		private static void LoadAliases()
		{
			aliases = JsonConvert.DeserializeObject<Dictionary<string,string>>(
				File.ReadAllText("alias.json")
			) ?? new Dictionary<string, string>();
		}

		public async static Task ProcessCommands(string input, bool verboseErrors)
        {
			string firstToken = input?.Split(' ')?[0]?.Trim() ?? "";

			if (aliases.ContainsKey(firstToken))
			{
				input = aliases[firstToken];
			}
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
                        var task = command.Invoke(null, new object[] { processor }) as Task;
						if (task != null)
                        	await task;
						else throw new ArgumentException("Console command must return a task");
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
			File.WriteAllText("Out/" + name + "." + fileType, contents);
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
				var path = processor.Switch("p").Default("Out/crawler-out.json").String;
				var urls = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(path)) ?? new List<string>();
				Profiler.Profile("all");
				var recipes = await Task.WhenAll(
					urls.Select(async url => {
						Log($"Processing: {url}");
						Profiler.Profile(url);
						var recipe = await Parser.Factory<GenericParser>(url).Parse();
						Log($"Finished: {url} | {Task.CurrentId} | {Profiler.Results(url)/1000}s", ConsoleColor.Green);
						return recipe;
					})
				);
				Log($"Parsed {urls.Count} recipes in {Profiler.Results("all")/1000}s");

				Dump(nameof(ParseRecipeBatch), JsonConvert.SerializeObject(recipes, Formatting.Indented));
			}
		}

		[CibusCommand]
		public static async Task BuildKeywordReport(InputProcessor processor)
		{
			if (processor.Help("Generates a report on a set of recipes",
				("p", "path to json list of recipes", true)
			))
			{
				var path = processor.Switch("p").Default("Out/ParseRecipeBatch.json").String;
				var recipes = JsonConvert.DeserializeObject<List<RecipeData>>(File.ReadAllText(path)) ?? new List<RecipeData>();
				Profiler.Profile("all");

				var report = new Dictionary<string,int>();

				foreach(var recipe in recipes)
				{
					var ingredientsKeys = recipe?.Ingredients?.SelectMany(x => x.Name.ToLower().Split(' ').Select(x => x.Trim()));

					foreach(var key in ingredientsKeys)
					{
						if (!report.ContainsKey(key))
							report.Add(key, 0);
						report[key]++;
					}
				}

				report = report.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
				
				Log($"Parsed {recipes.Count} recipes in {Profiler.Results("all")/1000}s");
				await Task.CompletedTask;

				Dump(nameof(BuildKeywordReport), JsonConvert.SerializeObject(report, Formatting.Indented));
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
				var blackList = processor.Switch("b").Default("").String.Split(',');
				var limit = processor.Switch("l").Default(50).Int;
				var outFile = processor.Switch("o").Default("crawler-out.json").String;
				var indentJson = processor.Switch("i").Default(false).Bool;
				var startXPath = processor.Switch("x").String; // specific to simplyrecipes //*[@id=\"card-list-1_1-0\"]
				var crawler = new Crawler(url);

				Log($"Initializing crawling at: {url} | ({crawler.Domain})", ConsoleColor.Green);

				Profiler.Profile("crawl");
				var urls = await crawler.CrawlUrlsMulti(limit, url, 
					url => {
						bool valid = string.IsNullOrEmpty(keyWords.FirstOrDefault()) ? true : keyWords.Any(key => url.Contains(key));
						valid = valid && (string.IsNullOrEmpty(blackList.FirstOrDefault()) ? true : !blackList.Any(key => url.Contains(key)));
						return valid;
					}, 
					result => {
						if (result.exception != null)
						{
							Log($"Could not crawl: {result.url} ({result.exception?.Message})", ConsoleColor.Yellow);
						}
						else 
						{
							Log($"[{result.allUrls?.Count()}/{limit}] Crawled: {result.url} Found: {result.newUrls?.Count()}");
						}
					},
					startXPath
				);
				var time = Profiler.Results("crawl");
				Log($"Finished crawling {limit} urls in {time / 1000} seconds", ConsoleColor.Green);
				Dump(outFile, JsonConvert.SerializeObject(urls, indentJson ? Formatting.Indented : Formatting.None));
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