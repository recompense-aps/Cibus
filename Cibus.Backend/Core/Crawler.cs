using HtmlAgilityPack;

namespace Cibus
{
	public class Crawler
	{
		public string? Url { get; private set; }
		public string? Domain { get; private set; }
		private HtmlWeb web = new HtmlWeb();
		public Crawler(string url)
		{
			Url = url;
			Domain = GetDomain(url);
		}

		public async Task<List<string>> CrawlUrls(int limit, string url, Func<string,bool>? filter = null, Action<CrawlResult>? progressCallback = null, string? startXPath = null)
		{
			var urls = new List<string>();
			var urlsToCheck = new Queue<string>();

			if (filter == null)
			{
				filter = (str) => true;
			}

			urlsToCheck.Enqueue(url);

			while(urlsToCheck.Count > 0 && urls.Count < limit)
			{
				var crawlUrl = urlsToCheck.Dequeue();

				var document = await web.LoadFromWebAsync(crawlUrl);
				var start = startXPath != null ? document.DocumentNode.SelectSingleNode(startXPath) : document.DocumentNode;
				
				var unfilteredUrls = start.Descendants()
					.Where(x => x.Attributes.Any(y => y.Name == "href"))
					.Select(x => x.Attributes.Single(a => a.Name == "href"))
					.Where(x => GetDomain(x.Value) == Domain && !x.Value.Contains("mailto"))
					.Select(x => x.Value)
				;
				var newUrls = unfilteredUrls
					.Where(filter)
					.Where(x => !urls.Contains(x))
					.ToList()
				;

				urls.AddRange(newUrls);

				if (progressCallback != null) progressCallback(new CrawlResult()
				{
					url = crawlUrl,
					newUrls = newUrls,
					allUrls = urls
				});

				foreach(var newUrl in newUrls)
					urlsToCheck.Enqueue(newUrl);
			}

			return urls;
		}

		public async Task<List<string>> CrawlUrlsMulti(int limit, string url, Func<string,bool>? filter = null, Action<CrawlResult>? progressCallback = null, string? startXPath = null)
		{
			var urls = new List<string>();
			var urlsToCheck = new List<string>();

			if (filter == null)
			{
				filter = (str) => true;
			}

			urlsToCheck.Add(url);

			async Task<List<string>> crawl_url(string crawlUrl)
			{
				var document = await web.LoadFromWebAsync(crawlUrl);
				var start = startXPath != null ? document.DocumentNode.SelectSingleNode(startXPath) : document.DocumentNode;

				if (start == null) return new List<string>();
				
				var unfilteredUrls = start.Descendants()
					.Where(x => x.Attributes.Any(y => y.Name == "href"))
					.Select(x => x.Attributes.Single(a => a.Name == "href"))
					.Where(x => GetDomain(x.Value) == Domain && !x.Value.Contains("mailto"))
					.Select(x => x.Value)
				;
				var newUrls = unfilteredUrls
					.Where(filter)
					.Where(x => !urls.Contains(x))
					.ToList()
				;

				urls.AddRange(newUrls);

				if (progressCallback != null) progressCallback(new CrawlResult()
				{
					url = crawlUrl,
					newUrls = newUrls,
					allUrls = urls
				});

				return newUrls;
			}

			while(urls.Count < limit)
			{
				var newUrlSets = await Task.WhenAll(urlsToCheck.Select(crawl_url));
				urlsToCheck = newUrlSets.SelectMany(x => x).ToList();
			}

			return urls.Take(limit).ToList();
		}

		private string GetDomain(string url)
		{
			return url.Replace("https://", "").Split('/')[0];
		}
	}

	public class CrawlResult
	{
		public string? url { get; set; }
		public IEnumerable<string>? newUrls { get; set; }
		public IEnumerable<string>? allUrls { get; set; }
	}
}