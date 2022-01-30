using HtmlAgilityPack;
using Newtonsoft.Json;

namespace Cibus
{
	public abstract class Parser
	{
		protected string? url { get; set; }
		protected HtmlDocument? document { get; private set; }
		protected string? allText => string.Join("", document?.DocumentNode?.SelectNodes("//*")?.Select(x => x.InnerText));
		protected IEnumerable<ParserToken>? tokens => document?.DocumentNode?.SelectNodes("//*")?.Select(x => {
			return new ParserToken()
			{
				source = x,
				contents = x.InnerText,
				tag = x.Name
			};
		}).Where(x => !new string[]{ "html", "meta" }.Contains(x.tag));
		private HtmlWeb web = new HtmlWeb();
		public async Task<RecipeData> Parse()
		{
			document = await web.LoadFromWebAsync(url);
			return ToRecipe();
		}

		public static Parser Factory(string? parsingUrl)
		{
			// var parser = new List<Parser>()
			// {
			// 	new AllRecipesParser(){ url = parsingUrl },
			// 	new FoodNetworkParser(){ url = parsingUrl },
			// 	new SimplyRecipesParser(){ url = parsingUrl },
			// 	new TasteOfHomeParser(){ url = parsingUrl },
			// 	new TastyParser(){ url = parsingUrl },
			// }.FirstOrDefault(x => x.CanParse());

			// parser = parser ?? new GenericParser(parsingUrl);

			return new GenericParser(parsingUrl);
		}

		public abstract bool CanParse();
		protected abstract RecipeData ToRecipe();
	}

	public class ParserToken
	{
		[JsonIgnore]
		public HtmlNode? source { get; set; }
		public string? tag { get; set; }
		public string? contents { get; set; }
	}
}