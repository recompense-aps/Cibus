using System.Diagnostics;
namespace Cibus
{
	public static class Profiler
	{
		private static Dictionary<string, Stopwatch> timers = new();

		public static void Profile(string id)
		{
			if (!timers.ContainsKey(id))
				timers.Add(id, new Stopwatch());

			timers[id].Restart();
		}

		public static long Results(string id)
		{
			timers[id].Stop();
			return timers[id].ElapsedMilliseconds;
		}
	}
}