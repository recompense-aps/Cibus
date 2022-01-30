using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

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
				ExternalSource = url,
				Name = recipeJObject?.SelectToken("$.name")?.ToString(),
				Description = recipeJObject?.SelectToken("$.description")?.ToString(),
				PrepTime = ConvertTimeString(recipeJObject?.SelectToken("$.prepTime")?.ToString()),
				CookTime = ConvertTimeString(recipeJObject?.SelectToken("$.cookTime")?.ToString()),
				Yield = decimal.Parse(recipeJObject?.SelectToken("$.recipeYield")?.ToString().Split(' ')[0])
			};
		}

		private decimal ConvertTimeString(string? timeString)
		{
			if (timeString == null) return 0;

			timeString = new string(timeString.Skip(1).ToArray());
			var days = new string(timeString.TakeWhile(c => c != 'D').ToArray());
			var hours = new string(timeString.Skip(days.Length + 2).TakeWhile(c => c != 'H').ToArray());
			var minutes = new string(timeString.Skip(days.Length + hours.Length + 3).TakeWhile(c => c != 'M').ToArray());

			return (decimal.Parse(days) * 1440) + (decimal.Parse(hours) * 60) + decimal.Parse(minutes);
		}
	}
}

/*
	AllRecipes uses some weird json format embedded in the html. Here is an example:
	{
      "@context": "http://schema.org",
      "@type": "Recipe",
      "mainEntityOfPage": "https://www.allrecipes.com/recipe/73047/yummy-bok-choy-salad/",
      "name": "Yummy Bok Choy Salad",
      "image": {
        "@type": "ImageObject",
        "url": "https://imagesvc.meredithcorp.io/v3/mm/image?url=https%3A%2F%2Fimages.media-allrecipes.com%2Fuserphotos%2F2432196.jpg",
        "width": 1704,
        "height": 1705
      },
      "datePublished": "2020-06-19T03:10:56.000Z",
      "description": "This is hands down the best salad that I've ever had.  It is definitely a family favourite, and I urge you to just give this one a try. You would think that raw baby bok choy would give this salad a bitter taste, but the dressing makes all the difference.",
      "prepTime": "P0DT0H20M",
      "cookTime": null,
      "totalTime": "P0DT0H20M",
      "recipeYield": "4 servings",
      "recipeIngredient": [
        "½ cup olive oil",
        "¼ cup white vinegar",
        "⅓ cup white sugar",
        "3 tablespoons soy sauce",
        "2 bunches baby bok choy, cleaned and sliced",
        "1 bunch green onions, chopped",
        "⅛ cup slivered almonds, toasted",
        "½ (6 ounce) package chow mein noodles"
      ],
      "recipeInstructions": [
        {
          "@type": "HowToStep",
          "text": "In a glass jar with a lid, mix together olive oil, white vinegar, sugar, and soy sauce.  Close the lid, and shake until well mixed.\n"
        },
        {
          "@type": "HowToStep",
          "text": "Combine the bok choy, green onions, almonds, and chow mein noodles in a salad bowl. Toss with dressing, and serve.\n"
        }
      ],
      "recipeCategory": [
        "Green Salads"
      ],
      "recipeCuisine": [],
      "author": [
        {
          "@type": "Person",
          "name": "SYS1",
          "url": "https://www.allrecipes.comhttps://www.allrecipes.com/cook/1112504/"
        }
      ],
      "aggregateRating": {
        "@type": "AggregateRating",
        "ratingValue": 4.655518394648829,
        "ratingCount": 295,
        "itemReviewed": "Yummy Bok Choy Salad",
        "bestRating": "5",
        "worstRating": "1"
      },
      "nutrition": {
        "@type": "NutritionInformation",
        "calories": "458 calories",
        "carbohydrateContent": "35.9 g",
        "cholesterolContent": null,
        "fatContent": "33.5 g",
        "fiberContent": "3.7 g",
        "proteinContent": "6.4 g",
        "saturatedFatContent": "4.8 g",
        "servingSize": null,
        "sodiumContent": "867.6 mg",
        "sugarContent": "18.7 g",
        "transFatContent": null,
        "unsaturatedFatContent": null
      },
	}
*/
