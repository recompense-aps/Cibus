using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;

namespace Cibus
{
	public enum RecipeParsingSection { Ingredients, Directions }
	public class GenericParser : Parser
	{
		public override bool CanParse() => true;
		public decimal MatchingTolerance { get; set; } = 0.9m;
		public bool IsRecipe => RecipeMatchScore >= MatchingTolerance;
		public decimal RecipeMatchScore => matchingScores.Sum() / matchingScores.Count();
		public Dictionary<RecipeParsingSection, Func<List<ParserToken>,decimal>> ScoringAlgorithms { get; private set; } = new Dictionary<RecipeParsingSection, Func<List<ParserToken>, decimal>>()
		{
			{ RecipeParsingSection.Ingredients, Algorithms.ScoreLineByLine(Keywords.IngredientKeywords) },
			{ RecipeParsingSection.Directions, Algorithms.ScoreLineByLine(Keywords.DirectionsKeywords) }
		};
		private List<decimal> matchingScores = new List<decimal>();
		private object? useFor = null;

		public GenericParser(string parsingUrl)
		{
			url = parsingUrl;
		}
		public GenericParser Use(object thing)
		{
			useFor = thing;
			return this;
		}
		public GenericParser As(RecipeParsingSection parsingSection)
		{
			var algorithm = useFor as Func<List<string>,Func<List<ParserToken>,decimal>>;

			if (algorithm == null) 
				throw new ArgumentException($"Could not use ${useFor?.GetType()?.Name ?? "null"} for {parsingSection}");

			List<string> keywords = Keywords.IngredientKeywords;
			
			if (parsingSection == RecipeParsingSection.Directions)
				keywords = Keywords.DirectionsKeywords;

			ScoringAlgorithms[parsingSection] = algorithm(keywords);

			return this;
		}
		protected override RecipeData ToRecipe()
		{
			if (document == null) throw new ArgumentException("document is missing");

			return new RecipeData()
			{
				ExternalSource = url,
				Name = HttpUtility.HtmlDecode(GetName()),
				Ingredients = GetIngredients(),
				Directions = GetDirections()
			};
		}

		private List<IngredientData> GetIngredients()
		{
			var tokens = FindIngredientTokens();
			var data = new List<IngredientData>();

			foreach(var token in tokens)
			{
				data.Add(new IngredientData()
				{
					Name = token.contents.Trim()
				});
			}

			return data;
		}

		private List<string> GetDirections()
		{
			var tokens = FindDirectionTokens();
			var data = new List<string>();

			foreach(var token in tokens)
			{
				data.Add(HttpUtility.HtmlDecode(token.contents.Trim()));
			}

			return data;
		}

		private string GetName()
		{
			return document?.DocumentNode?.SelectSingleNode("//h1")?.InnerText?.Trim() ??
				document?.DocumentNode?.SelectSingleNode("//h2")?.InnerText?.Trim() ?? 
				document?.DocumentNode?.SelectSingleNode("//h3")?.InnerText?.Trim() ??
				document?.DocumentNode?.SelectSingleNode("//h4")?.InnerText?.Trim() ?? 
				"Unable to find title";
		}

		private List<List<ParserToken>> GroupTokensOfTagByParentNode(string tag, Func<ParserToken, bool>? filter = null)
		{
			if (filter == null) filter = (token) => true;

			return tokens?
				.Where(token => token.tag == tag && filter(token))
				.GroupBy(x => x.source.ParentNode)
				.DistinctBy(x => string.Join("", x.ToList().Select(y => y.contents)).ToLower())
				.Select(x => x.ToList())
				.ToList();
		}

		private (decimal score, List<ParserToken> group) FindBestTokenGroup(Func<List<ParserToken>,decimal> scorer, List<List<ParserToken>> groups)
		{
			var bestOption = groups.First();
			var bestScore = scorer(bestOption); 

			foreach(var group in groups)
			{
				var newScore = scorer(group);

				if (newScore > bestScore)
				{
					bestScore = newScore;
					bestOption = group;
				}
			}

			matchingScores.Add(bestScore);

			return (bestScore, bestOption);
		}

		private List<ParserToken>? FindBestTokenGroupWithTags(Func<List<ParserToken>,decimal> scorer, params string[] tags)
		{
			return tags
				.Select(x => GroupTokensOfTagByParentNode(x, token => !token.source.DescendantNodes().Any(x => x.Name == "a")))
				.Select(x => FindBestTokenGroup(scorer, x))
				.MaxBy(x => x.score)
				.group
			;
		}

		private List<ParserToken>? FindIngredientTokens()
		{
			return FindBestTokenGroupWithTags(ScoringAlgorithms[RecipeParsingSection.Ingredients], "li", "p");
		}

		private List<ParserToken>? FindDirectionTokens()
		{
			return FindBestTokenGroupWithTags(ScoringAlgorithms[RecipeParsingSection.Directions], "li", "p");
		}
	}
}