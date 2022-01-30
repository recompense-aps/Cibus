using System.Collections.Generic;
using Cibus;

namespace Cibus.Tests
{
	public static class Recipes
	{
		public static readonly RecipeData YummyBokChoySalad = new RecipeData()
		{
			Name = "Yummy Bok Choy Salad",
			Description = "This is hands down the best salad that I've ever had. It is definitely a family favourite, and I urge you to just give this one a try. You would think that raw baby bok choy would give this salad a bitter taste, but the dressing makes all the difference.",
			PrepTime = 20,
			CookTime = 0,
			Yield = 4,
			Ingredients = new List<IngredientData>()
			{
				new IngredientData(){ Amount = 0.5m, Unit="cup", Name="olive oil" },
				new IngredientData(){ Amount = 0.25m, Unit="cup", Name="white vinegar" },
				new IngredientData(){ Amount = 3, Unit="tablespoons", Name="soy sauce" },
				new IngredientData(){ Amount = 0.333m, Unit="cup", Name="white sugar" },
				new IngredientData(){ Amount = 1, Unit="bunch", Name="green onions, chopped" },
				new IngredientData(){ Amount = 0.125m, Unit="cup", Name="slivered almonds, toasted" },
				new IngredientData(){ Amount = 0.5m, Unit="(6 ounce) package", Name="chow mein noodles" }
			},
			Directions = new List<string>()
			{
				"In a glass jar with a lid, mix together olive oil, white vinegar, sugar, and soy sauce. Close the lid, and shake until well mixed.",
				"Combine the bok choy, green onions, almonds, and chow mein noodles in a salad bowl. Toss with dressing, and serve."
			},
			ExternalSource = "https://www.allrecipes.com/recipe/73047/yummy-bok-choy-salad/"
			
		};
	}
}