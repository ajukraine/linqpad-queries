<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\Microsoft.Transactions.Bridge.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\SMDiagnostics.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Configuration.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.EnterpriseServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.IdentityModel.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.IdentityModel.Selectors.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Messaging.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.DurableInstancing.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Serialization.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Security.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.ServiceProcess.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.ApplicationServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.Services.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Xaml.dll</Reference>
  <NuGetReference>LiteDB</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>Tavis.UriTemplates</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>System</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Tavis.UriTemplates</Namespace>
  <Namespace>LiteDB</Namespace>
</Query>

async Task Main()
{
	ConfigureNetworkOptions();

	var credentials = new Dictionary<string, Credentials>
	{
		["ajukraine"] = new Credentials { username = "restless-archmage-4753", token = "{secret}" },
		["nexus"] = new Credentials { username = "morning-timber-wolf-6550", token = "{secret}" }
	};
	
	var dbFilePath = @"E:\Hearthstone\Trackobot\api-snapshot.db";

	var uriTemplate = new UriTemplate("https://trackobot.com/profile/history.json{?username,token,page}");
	uriTemplate.AddParameters(credentials["ajukraine"]);

	var client = new HttpClient() { MaxResponseContentBufferSize = 1000000 };
	
	await SynchronizeData(client, dbFilePath, uriTemplate);
	var gameEntries = GetData(dbFilePath);

	gameEntries.Count().Dump("Entries count");
	
	var stats = new List<Game>();
	foreach (var entry in gameEntries)
	{
		dynamic game = JValue.Parse(entry.Json);
		
		stats.Add(new Game
		{
			Result = game.result == "win" ? GameResult.Win : GameResult.Loss,
			Coin = game.coin,
			TurnsCount = ((int?)(game.card_history as JArray).Max((dynamic turn) => turn.turn == null ? 0 : turn.turn)).GetValueOrDefault(),
			Duration = game.duration,
			Added = DateTime.Parse((string)game.added),
			Hero = game.hero,
			HeroDeck = game.hero_deck
		});
	}

	stats.GroupBy(g => new { g.Hero, g.HeroDeck }).Select(g => new { g.Key, Count = g.Count() }).Dump();
	
	/*
	var total = stats.Count.Dump("Count");
	
	var seconds = stats.Sum(g => g.Duration);
	
	((seconds.Dump("Seconds")
		/ 60.0m).Dump("Minutes")
		/ 60.0m).Dump("Hours");

	var weekdays = stats.Where(x => x.Added.DayOfWeek != DayOfWeek.Sunday && x.Added.DayOfWeek != DayOfWeek.Saturday);
	var weekdaysTotal = weekdays.Count();
	
	var grouped = weekdays
		.GroupBy(x => x.Added.Hour).Select(g => new
		{
			g.Key,
			Duration = g.Sum(y => y.Duration),
			Count = g.Count(),
			Rate = (g.Count() * 1.0 / total).ToString("P"),
			Winrate = (g.Count(x => x.Result == GameResult.Win) * 1.0) / g.Count()
		})
		.OrderBy(x => x.Key)
		.Dump();
	
	(grouped.Where(x => x.Key >= 10 && x.Key <= 18).Sum(x => x.Duration) * 1.0m / seconds).Dump("Interlogic hours :)");
	*/
	/*
	var byCoin = stats.GroupBy(game => game.Coin);

	byCoin
		// .Dump()
		.Select(g => new { Coin = g.Key, Winrate = (g.Count(x => x.Result == GameResult.Win) * 1.0) / g.Count(), Games = g.Count() })
		.Dump();
	*/
}

void ConfigureNetworkOptions()
{
	ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
	ServicePointManager.DefaultConnectionLimit = 50;
	ServicePointManager.MaxServicePointIdleTime = 400;
	ServicePointManager.Expect100Continue = false;
	ServicePointManager.CheckCertificateRevocationList = false;
}


async Task SynchronizeData(HttpClient httpClient, string fileName, UriTemplate uriTemplate)
{	
	int totalPages = 1;
	
	var localDb = new LiteDatabase(fileName);

	var entries = localDb.GetCollection<GameEntry>("gameEntries");
	entries.Count();
	entries.EnsureIndex(x => x.CreatedOn);
	
	var latestDate = entries.Max("CreatedOn").AsDateTime;
	var newEntriesCounter = 0;

	for (var i = 1; i <= totalPages; ++i)
	{
		var rawString = await httpClient.GetStringAsync(uriTemplate.AddParameter("page", i).Resolve());
		dynamic page = JValue.Parse(rawString);
		
		var pagesCount = (int)page.meta.total_pages;

		if (pagesCount != totalPages)
		{
			Debug.Write($"Total pages count is different. Old = {totalPages}. New = {pagesCount}");
			totalPages = pagesCount;
		}

		foreach (dynamic game in (page.history as JArray))
		{
			var gameEntry = new GameEntry
			{
				Id = game.id,
				Json = game.ToString(),
				CreatedOn = DateTime.Parse((string)game.added)
			};

			if (latestDate >= gameEntry.CreatedOn)
			{
				goto loop;
			}
			
			++newEntriesCounter;
			entries.Insert(gameEntry);
		}
	}
	loop:;
	
	newEntriesCounter.Dump("New entries");
	
	localDb.Dispose();
}

IEnumerable<GameEntry> GetData(string fileName)
{
	var localDb = new LiteDatabase(fileName);
	var entries = localDb.GetCollection<GameEntry>("gameEntries");
	return entries.FindAll();
}

public class GameEntry
{
	public string Id { get; set; }

	public string Json { get; set; }

	public DateTime CreatedOn { get; set; }
}

class Game
{
	public GameResult Result { get; set; }

	public bool Coin { get; set; }

	public int TurnsCount { get; set; }

	public int Duration { get; set; }

	public DateTime Added { get; set; }

	public string Hero { get; set; }

	public string HeroDeck { get; set; }
}

class Credentials
{
	public string username { get; set; }

	public string token { get; set; }
}

enum GameResult
{
	Win, Loss
}

// Define other methods and classes here