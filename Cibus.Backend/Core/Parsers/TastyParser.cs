using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cibus
{
	public class TastyParser : Parser
	{
		public override bool CanParse() => url?.Contains("tasty.co") == true;
		protected override RecipeData ToRecipe()
		{
			return new RecipeData()
			{
			};
		}
	}
}