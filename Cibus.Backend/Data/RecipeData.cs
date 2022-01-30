using System.Linq;
namespace Cibus
{
	public class RecipeData : CibusData
	{
		public Guid? Guid { get; private set; }
		public string? Name { get; set; }
		public string? Description { get; set; }
		public decimal? PrepTime { get; set; }
		public decimal? CookTime { get; set; }
		public decimal? TotalTime => PrepTime + CookTime;
		public decimal? Yield { get; set; }
		public List<IngredientData>? Ingredients { get; set; }
		public List<string>? Directions { get; set; }
		public string? ExternalSource { get; set; }

		/// <summary>
		/// Checks if another recipe is equal in substance, i.e. Name and ingredients but not
		/// necessarily metadata
		/// </summary>
		/// <param name="recipe"></param>
		/// <returns></returns>
		public bool SubstanceIsEqualTo(RecipeData recipe)
		{
			if (recipe.Ingredients == null || Ingredients == null) throw new ArgumentException("recipe has no ingredients!");
			if (recipe.Directions == null || Directions == null) throw new ArgumentException("recipe has no directions!");

			return new List<bool>()
			{
				Name == recipe.Name,
				PrepTime == recipe.PrepTime,
				CookTime == recipe.CookTime,
				Yield == recipe.Yield,
				Ingredients.Zip(recipe.Ingredients).All(tuple => {
					return (
						tuple.First.Name == tuple.Second.Name &&
						tuple.First.Unit == tuple.Second.Unit &&
						tuple.First.Amount == tuple.Second.Amount
					);
				}),
				Directions.Zip(recipe.Directions).All(tuple => tuple.First == tuple.Second)
			}.All(x => x);
		}
	}

	public class IngredientData : CibusData
	{
		public string? Name { get; set; }
		public string? Unit { get; set; }
		public decimal? Amount { get; set; }
	}
}