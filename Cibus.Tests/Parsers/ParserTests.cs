using System;
using System.Threading.Tasks;
using Xunit;
using Cibus;

namespace Cibus.Tests
{
	public class ParserTests
	{
		[Theory]
		[InlineData("https://www.foodnetwork.com/recipes/recipes-a-z", typeof(FoodNetworkParser))]
		[InlineData("https://www.allrecipes.com/gallery/bok-choy-salad-recipes/", typeof(AllRecipesParser))]
		[InlineData("https://www.simplyrecipes.com/recipes/pork_schnitzel/", typeof(SimplyRecipesParser))]
		[InlineData("https://www.tasteofhome.com/recipes/favorite-chicken-potpie/", typeof(TasteOfHomeParser))]
		[InlineData("https://tasty.co/recipe/easily-the-best-garlic-herb-roasted-potatoes", typeof(TastyParser))]
		[InlineData("https://unsupportecrecipesight.com/recipe", typeof(GenericParser))]
		public void ParserFactory_Returns_CorrectParser(string url, Type correctType)
		{
			// Given
			var parser = Parser.Factory(url);
		
			// When
			var parserIsCorrectParser = parser.GetType() == correctType; 
		
			// Then
			Assert.True(parserIsCorrectParser, nameof(parserIsCorrectParser));
		}

		[Fact]
		public async Task AllRecipesParser_ParsesBokChoyRecipeCorrectly()
		{
			// Given
			var parser = Parser.Factory("https://www.allrecipes.com/recipe/73047/yummy-bok-choy-salad/");
		
			// When
			var recipe = await parser.Parse();
		
			// Then
			Assert.True(recipe.SubstanceIsEqualTo(Recipes.YummyBokChoySalad));
		}
	}
}