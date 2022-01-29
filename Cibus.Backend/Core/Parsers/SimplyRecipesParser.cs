using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cibus
{
	public class SimplyRecipesParser : Parser
	{
		public override bool CanParse() => url?.Contains("simplyrecipes.com") == true;
		protected override RecipeData ToRecipe()
		{
			return new RecipeData()
			{
			};
		}
	}
}