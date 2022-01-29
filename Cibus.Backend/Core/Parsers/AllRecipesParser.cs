using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cibus
{
	public class AllRecipesParser : Parser
	{
		public override bool CanParse() => url?.Contains("www.allrecipes.com") == true;
		protected override RecipeData ToRecipe()
		{
			if (document == null) throw new ArgumentException("document is missing");

			var title = document.DocumentNode.SelectSingleNode("//head/title").InnerHtml;
			var scripts = document.DocumentNode.SelectNodes("//*[@type='application/ld+json']");
			var jsonNode = scripts.FirstOrDefault();
			var json = "{ \"items\": " + (jsonNode?.InnerText?.Replace("@", "") ?? "[]") + "}";
			var jObject = JObject.Parse(json);
			var recipeJObject = jObject.SelectToken("..items[?(@.type=='Recipe')]");

			return new RecipeData()
			{
				Name = recipeJObject?.SelectToken("$.name")?.ToString()
			};
		}
	}
}