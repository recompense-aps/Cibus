namespace Cibus
{
	public static class Algorithms
	{
		private static bool collectMetaData = false;
		private static List<AlgorithmMetaData> metaData = new List<AlgorithmMetaData>();

		public static void Record() => collectMetaData = true;
		public static List<AlgorithmMetaData> Results()
		{
			collectMetaData = false;
			return metaData;
		}

		public static Func<List<ParserToken>,decimal> ScoreLineByLine(List<string> keywords)
		{
			const decimal shortListPenalty = 0.1m;
			return (tokens) => {
				var dif = 4 - tokens.Count;
				dif = dif < 0 ? 0 : dif;

				var sum = tokens
					.Select(x => x.contents.ToLower())
					.Select(x => keywords.Where(y => x.Contains(y)).Count())
					.Sum();

				var result = (sum / (decimal)tokens.Count()) - (shortListPenalty * dif);

				if (collectMetaData)
				{
					metaData.Add(new AlgorithmMetaData()
					{
						Algorithm = nameof(ScoreByStringLength),
						input = tokens,
						output = result,
						otherData = keywords
					});
				}

				return result;
			};
		}

		/// <summary>
		/// Given a list of string keywords:
		/// Returns a function that, given a list of ParserTokens, returns a decimal score by:
		/// - Counting how many times a keyword appears in a string formed of the combined list of ParserToken contents
		/// - Dividing that count by the length of the string
		/// </summary>
		/// <param name="keywords"></param>
		/// <returns></returns>
		public static Func<List<ParserToken>,decimal> ScoreByStringLength(List<string> keywords)
		{
			return (tokens) => {
				var combinedString = string.Join("", tokens.Select(x => x.contents)).ToLower();

				var sum = keywords
					.Select(x => (combinedString.Split(x).Length - 1) * 3)
					.Sum();
				var result = (decimal)sum;

				if (collectMetaData)
				{
					metaData.Add(new AlgorithmMetaData()
					{
						Algorithm = nameof(ScoreByStringLength),
						input = tokens,
						output = result,
						otherData = new {
							keywords,
							sum,
							combinedString
						}
					});
				}

				return result;
			};
		}
	}

	public class AlgorithmMetaData
	{
		public string? Algorithm { get; set; }
		public object? input { get; set; }
		public object? output { get; set; }
		public object? otherData { get; set; }
	}
}