using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using System;
using System.Collections.Generic;
using System.Net;

namespace DNSInfo.Utils
{
	public static class DnsValidation
	{
		public static bool IsSupportDnsSec(DnsClient dnsClient)
		{
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
			return dnsMessage.IsAuthenticData/* && dnsMessage.IsDnsSecOk*/;
		}

		public static bool IsSupportDnsSec(DomainName domain, DnsClient dnsClient = null)
		{
			IDnsSecResolver resolver = new SelfValidatingInternalDnsSecStubResolver(dnsClient);
			var result = resolver.ResolveSecure<SshFpRecord>(domain, RecordType.SshFp);
			return result.ValidationResult == DnsSecValidationResult.Signed;
		}

		public static bool IsSupportEcs(DnsClient dnsClient, ClientSubnetOption ecs = null)
		{
			if (ecs == null)
			{
				ecs = new ClientSubnetOption(32, IPAddress.Parse(@"202.96.199.133"));
			}

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

		private const string PollutedListUrl = @"https://raw.githubusercontent.com/HMBSbige/Text_Translation/master/PollutedIPv4.txt";

		private static HashSet<IPAddress> _pollutedIp;

		public static bool IsPoison(IPAddress ip)
		{
			if (_pollutedIp == null)
			{
				_pollutedIp = new HashSet<IPAddress>();
				using var client = new WebClient();
				var str = client.DownloadString(PollutedListUrl);
				var ips = str.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var s in ips)
				{
					if (IPAddress.TryParse(s, out var outIp))
					{
						_pollutedIp.Add(outIp);
					}
				}
			}
			return _pollutedIp.Contains(ip);
		}
	}
}
