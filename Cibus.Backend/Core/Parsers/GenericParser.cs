using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;

namespace Cibus
{
	public class GenericParser : Parser
	{
		public override bool CanParse() => true;
		public bool IsRecipe => RecipeMatchScore >= 1;
		public decimal RecipeMatchScore { get; private set; } = 0;
		public GenericParser(string parsingUrl)
		{
			url = parsingUrl;
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

		private List<List<ParserToken>> GroupTokensOfTagByParentNode(string tag)
		{
			return tokens?.Where(token => token.tag == tag)
				.GroupBy(x => x.source.ParentNode)
				.Select(x => x.ToList())
				.ToList();
		}

		private (int score, List<ParserToken> group) FindBestTokenGroup(Func<List<ParserToken>,int> scorer, List<List<ParserToken>> groups)
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

			RecipeMatchScore += (decimal)bestScore / (decimal)bestOption.Count;

			return (bestScore, bestOption);
		}

		private Func<List<ParserToken>,int> GetStandardTokenScorer(Dictionary<string, int> tokenScoreLookUp)
		{
			return (tokens) => {
				string combined = string.Join("", tokens.Select(x => x.contents));
			
				return tokenScoreLookUp.Select(x => {
					int occurances = combined.Split(x.Key).Length;

					if (occurances > 0) occurances--;

					return occurances * x.Value;
				}).Sum();
			};
		}

		private List<ParserToken>? FindBestTokenGroupWithTags(Func<List<ParserToken>,int> scorer, params string[] tags)
		{
			return tags
				.Select(GroupTokensOfTagByParentNode)
				.Select(x => FindBestTokenGroup(scorer, x))
				.MaxBy(x => x.score)
				.group
			;
		}

		private List<ParserToken>? FindIngredientTokens()
		{
			// probably should be a db thing?
			var scorer = GetStandardTokenScorer(new Dictionary<string, int>()
			{
				{ "½", 1},
				{ "1/4", 1},
				{ "1/2", 1},
				{ "1/3", 1},
				{ "¼", 1 },
				{ "cup", 1},
				{ "tablespoon", 1},
				{ "ounce", 1},
				{ "pound", 1},
				{ "salt", 1},
				{ "pepper", 1}
			});

			return FindBestTokenGroupWithTags(scorer, "li", "p");
		}

		private List<ParserToken>? FindDirectionTokens()
		{
			// probably should be a db thing?
			var scorer = GetStandardTokenScorer(new Dictionary<string, int>()
			{
				{ "roll", 1},
				{ "line", 1},
				{ "boil", 1},
				{ "cut", 1},
				{ "chop", 1 },
				{ "sir", 1},
				{ "fill", 1},
				{ "ounce", 1},
				{ "refrigerate ", 1},
			});

			return FindBestTokenGroupWithTags(scorer, "li", "p");
		}
	}
}