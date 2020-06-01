<Query Kind="Program">
  <NuGetReference>Akka</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Akka.Actor</Namespace>
  <Namespace>Akka.Routing</Namespace>
</Query>

void Main()
{
	using (var system = ActorSystem.Create("First"))
	{
		var props = new RoundRobinPool(5).Props(Props.Create<MemberActor>());
		var member = system.ActorOf(props, "member");
		
		"Start".Dump(DateTime.UtcNow.ToString("hh:mm:ss.fff"));

		Enumerable.Range(1, 10).ToList().ForEach(i => member.Tell($"DoIt [{i}]"));
		
		Thread.Sleep(TimeSpan.FromSeconds(10));
		
		"Finish".Dump(DateTime.UtcNow.ToString("hh:mm:ss.fff"));
	}
}

class MemberActor : UntypedActor
{
	protected override void OnReceive(object message)
	{
		Thread.Sleep(10);
		$"{Self.Path}: {message}".Dump(DateTime.UtcNow.ToString("hh:mm:ss.fff"));
	}
}