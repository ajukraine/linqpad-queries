<Query Kind="Program">
  <Connection>
    <ID>843d7ec3-4209-4139-b218-b93993236ef9</ID>
    <Persist>true</Persist>
    <Server>(localdb)\MSSQLLocalDB</Server>
  </Connection>
  <NuGetReference>DbExtensions</NuGetReference>
  <NuGetReference>FluentAssertions</NuGetReference>
  <NuGetReference>NUnit</NuGetReference>
  <Namespace>DbExtensions</Namespace>
  <Namespace>FluentAssertions</Namespace>
  <Namespace>FluentAssertions.Collections</Namespace>
  <Namespace>FluentAssertions.Common</Namespace>
  <Namespace>FluentAssertions.Equivalency</Namespace>
  <Namespace>FluentAssertions.Events</Namespace>
  <Namespace>FluentAssertions.Execution</Namespace>
  <Namespace>FluentAssertions.Extensions</Namespace>
  <Namespace>FluentAssertions.Formatting</Namespace>
  <Namespace>FluentAssertions.Numeric</Namespace>
  <Namespace>FluentAssertions.Primitives</Namespace>
  <Namespace>FluentAssertions.Reflection</Namespace>
  <Namespace>FluentAssertions.Specialized</Namespace>
  <Namespace>FluentAssertions.Types</Namespace>
  <Namespace>FluentAssertions.Xml</Namespace>
  <Namespace>NUnit.Framework</Namespace>
</Query>

void Main()
{
	/*var query = new SqlBuilder()
		.FROM("member_data")
		.WHERE("FIELD = {0}", "VALUE")
		.SELECT("count(*)")
		.ToString();*/
	
	var db = new Database(Connection);
	var query = db
		.From<MemberCreditClassAudit>("[dbo].[rp_member_creditclass_audit]")
		.Where("MemberId = {0}", 1)
		.OrderBy("ChangeDate DESC")
		.Take(1);
	
	var row = query.Single().Dump();
	
	row.ChangeDate.Should().BeAfter(new DateTime(2018, 3, 24, 21, 27, 24));
	row.NewCreditClass.Should().Be(1);
}

class MemberCreditClassAudit
{
	public DateTime ChangeDate {get; set;}
	public byte NewCreditClass {get; set;}
}
// Define other methods and classes here
