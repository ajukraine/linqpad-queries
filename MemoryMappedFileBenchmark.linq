<Query Kind="Program">
  <Namespace>System.IO.MemoryMappedFiles</Namespace>
</Query>

void Main()
{
	var path = @"C:\LMDB\Map.mp";
	
	MemoryMappedFile Create(FileStream fs)
	{
		var security = new MemoryMappedFileSecurity();

		return MemoryMappedFile.CreateFromFile(
			fs,
			"MyMmap",
			1000 * 1024 * 1024,
			MemoryMappedFileAccess.ReadWrite,
			security,
			HandleInheritability.None,
			false);
	}
	
	using (var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 8 * 1024, FileOptions.WriteThrough))
	using (var mmap = Create(fs))
	//using (var view = mmap.CreateViewAccessor())
	{
		var position = 0;
		var counter = 0;

		var prevPosition = 0;
		var prevCounter = 0;
		void CheckStatus(object state)
		{
			var stamp = DateTime.Now.ToString("HH:mm:ss.ffff");
			var bytesCount = position - prevPosition;
			var count = counter - prevCounter;

			$"{stamp} {count} {bytesCount.ToKB()}".Dump();
			prevPosition = position;
			prevCounter = counter;
		}
		
		new Timer(CheckStatus, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

		for (var i = 0; i < 5000000; ++i, ++counter)
		{
			var value = $"index:{i},value:{i}";
			var bytes = Encoding.UTF8.GetBytes(value);

			using (var view = mmap.CreateViewAccessor(position, bytes.Length, MemoryMappedFileAccess.Write))
			{
				view.WriteArray(0, bytes, 0, bytes.Length);
				//view.Flush();
			}
			fs.Flush(true);

			position += bytes.Length;
		}
	}
}

static class Extensions
{
	public static double ToKB<T>(this T bytes) => Convert.ToDouble(bytes) / 1024;
	public static double ToMB<T>(this T bytes) => Convert.ToDouble(bytes) / (1024 * 1024);
}