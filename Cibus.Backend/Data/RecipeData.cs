namespace Cibus
{
	public class RecipeData : CibusData
	{
		public Guid Guid { get; private set; }
		public string? Name { get; set; }
	}
}