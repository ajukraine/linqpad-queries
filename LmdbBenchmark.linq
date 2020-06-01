<Query Kind="Program">
  <NuGetReference>LightningDB</NuGetReference>
  <Namespace>LightningDB</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

class DatabaseInfo
{
	public LightningEnvironment Environment;
	public string Name;
	public int Counter;
	
	public DatabaseInfo(LightningEnvironment env, string dbName)
	{
		Environment = env;
		Name = dbName;
		Counter = 0;
	}
}

void Main()
{
	var path = "c:/LMDB";
	var dbName = "test";
	var dbSize = 10;
	var numOfSamples = 50000u;
	
	DeleteAllDatabases(path);
	
	using (var env = new LightningEnvironment(path))
	{
		env.MaxDatabases = 1;
		env.MapSize = dbSize * 1024 * 1024;
		env.Open();
		
		var dbInfo = new DatabaseInfo(env, dbName);

		var monitor = SetupMonitor(dbInfo);
		new Timer(new TimerCallback(monitor), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

		CreateDatabase(dbInfo);
		HandleWrites(dbInfo, numOfSamples);
	}
}

byte[] GetBytes(string str) => Encoding.UTF8.GetBytes(str);
string FromBytes(byte[] bytes) => Encoding.UTF8.GetString(bytes);

Action<object> SetupMonitor(DatabaseInfo dbInfo)
{
	var prevCounter = 0;
	void CheckStatus(object state)
	{
		var timestamp = DateTime.Now.ToString("HH:mm:ss.ffff");
		var stepCount = dbInfo.Counter - prevCounter;

		Console.WriteLine($"{timestamp} {stepCount}");

		prevCounter = dbInfo.Counter;
	}
	
	return CheckStatus;
}

void HandleWrites(DatabaseInfo dbInfo, uint numOfSamples)
{
	using (var db = OpenDatabase(dbInfo))
	{
		using (var tx = dbInfo.Environment.BeginTransaction())
		{
			for (var i = 0u; i < numOfSamples; ++i)
			{
				//using (var tx = dbInfo.Environment.BeginTransaction())
				//using (var db = tx.OpenDatabase(dbInfo.Name, config))
				{
					tx.Put(db, GetBytes($"index{i}"), GetBytes($"value{i}"));
					//tx.Commit();
				}

				Interlocked.Increment(ref dbInfo.Counter);
			}
			tx.Commit();
		}
	}
}

LightningDatabase OpenDatabase(DatabaseInfo dbInfo)
{
	var config = new DatabaseConfiguration();
	
	using (var tx = dbInfo.Environment.BeginTransaction())
	{
		var db = tx.OpenDatabase(dbInfo.Name, config);
		
		tx.Commit();
		return db;
	}
}

void CreateDatabase(DatabaseInfo dbInfo)
{
	using (var tx = dbInfo.Environment.BeginTransaction())
	using (var db = tx.OpenDatabase(dbInfo.Name, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
	{
		tx.Commit();
	}
}

void DeleteAllDatabases(string path)
{
	Directory.Delete(path, true);
}