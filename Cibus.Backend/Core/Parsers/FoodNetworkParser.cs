using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cibus
{
	public class FoodNetworkParser : Parser
	{
		public override bool CanParse() => url?.Contains("foodnetwork.com") == true;
		protected override RecipeData ToRecipe()
		{
			return new RecipeData()
			{
			};
		}
	}
}