using System;

namespace AzCopySnapshot
{
	public class Input
	{
		public string SnapshotName { get; set; }
		public Source Source { get; set; } = new Source();
		public Destination Destination { get; set; } = new Destination();
		public ServicePrincipal ServicePrincipal { get; set; } = new ServicePrincipal();
	}

	public class Source
	{
		public string SubscriptionId { get; set; }
		public string ResourceGroup { get; set; }
		public string Location { get; set; }
	}

	public class Destination
	{
		public string SubscriptionId { get; set; }
		public string ResourceGroup { get; set; }
	}

	public class ServicePrincipal
	{
		public string ActiveDirectoryDomain { get; set; }
		public string TenantId { get; set; }
		public string ApplicationId { get; set; }
		public string ApplicationPassword { get; set; }
	}
}
