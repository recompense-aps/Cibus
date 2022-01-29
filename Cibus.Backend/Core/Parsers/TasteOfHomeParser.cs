using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cibus
{
	public class TasteOfHomeParser : Parser
	{
		public override bool CanParse() => url?.Contains("tasteofhome.com") == true;
		protected override RecipeData ToRecipe()
		{
			return new RecipeData()
			{
			};
		}
	}
}