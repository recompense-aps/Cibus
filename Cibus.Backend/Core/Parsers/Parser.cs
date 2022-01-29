using HtmlAgilityPack;

namespace Cibus
{
	public abstract class Parser
	{
		protected string? url { get; private set; }
		protected HtmlDocument? document { get; private set; }
		private HtmlWeb web = new HtmlWeb();
		public async Task<RecipeData> Parse()
		{
			document = await web.LoadFromWebAsync(url);
			return ToRecipe();
		}

		public static Parser Factory(string? parsingUrl)
		{
			var parser = new List<Parser>()
			{
				new AllRecipesParser(){ url = parsingUrl }
			}.SingleOrDefault(x => x.CanParse());

			parser = parser ?? new AllRecipesParser(){ url = parsingUrl };

			return parser;
		}

		public abstract bool CanParse();
		protected abstract RecipeData ToRecipe();
	}
}