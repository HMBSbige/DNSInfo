using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using System.Net;

namespace DNSInfo.Utils
{
	public static class DnsValidation
	{
		public static bool IsSupportDnsSec(IPEndPoint dns)
		{
			var dnsClient = new DnsClient(dns.Address, 10000, dns.Port);

			var options = new DnsQueryOptions
			{
				IsRecursionDesired = true,
				IsCheckingDisabled = false,
				IsEDnsEnabled = true,
				IsDnsSecOk = true,
			};

			var dnsMessage = dnsClient.Resolve(DomainName.Parse(@"pir.org"), RecordType.A, RecordClass.INet, options);

			if (dnsMessage == null || dnsMessage.ReturnCode != ReturnCode.NoError && dnsMessage.ReturnCode != ReturnCode.NxDomain)
			{
				return false;
			}
			return dnsMessage.IsAuthenticData && dnsMessage.IsDnsSecOk;
		}

		public static bool IsSupportDnsSec(DomainName domain)
		{
			var dnsClient = new DnsClient(IPAddress.Parse(@"1.1.1.1"), 10000);

			var options = new DnsQueryOptions
			{
				IsRecursionDesired = true,
				IsCheckingDisabled = false,
				IsEDnsEnabled = true,
				IsDnsSecOk = true,
			};

			var dnsMessage = dnsClient.Resolve(domain, RecordType.A, RecordClass.INet, options);

			if (dnsMessage == null || dnsMessage.ReturnCode != ReturnCode.NoError && dnsMessage.ReturnCode != ReturnCode.NxDomain)
			{
				return false;
			}

			return dnsMessage.IsAuthenticData && dnsMessage.IsDnsSecOk;
		}

		public static bool IsSupportEcs(IPEndPoint dns)
		{
			var dnsClient = new DnsClient(dns.Address, 10000, dns.Port);
			var ecs = new ClientSubnetOption(32, IPAddress.Parse(@"202.96.199.133"));

			var options = new DnsQueryOptions
			{
				IsEDnsEnabled = true,
				IsRecursionDesired = true,
				EDnsOptions = new OptRecord { Options = { ecs } }
			};

			var dnsMessage = dnsClient.Resolve(DomainName.Parse(@"www.bing.com"), RecordType.A, RecordClass.INet, options);

			if (dnsMessage == null || dnsMessage.ReturnCode != ReturnCode.NoError && dnsMessage.ReturnCode != ReturnCode.NxDomain)
			{
				return false;
			}

			if (dnsMessage.EDnsOptions.Options.Count > 0)
			{
				if (dnsMessage.EDnsOptions.Options[0] is ClientSubnetOption a)
				{
					return a.Type == EDnsOptionType.ClientSubnet
						&& Equals(a.Address, ecs.Address)
						&& a.Family == ecs.Family
						&& a.SourceNetmask == ecs.SourceNetmask;
				}
			}

			return false;
		}
	}
}
