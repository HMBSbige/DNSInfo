using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using DNSInfo.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace UnitTest
{
	[TestClass]
	public class DnsValidationTest
	{
		private readonly DnsClient googleDns = new DnsClient(IPAddress.Parse(@"8.8.8.8"), 10000);
		private readonly DnsClient cloudflareDns = new DnsClient(IPAddress.Parse(@"1.1.1.1"), 10000);
		private readonly DnsClient dnspod = new DnsClient(IPAddress.Parse(@"119.29.29.29"), 10000);
		private readonly DnsClient lugDns = new DnsClient(IPAddress.Parse(@"202.141.162.123"), 10000, 5353);
		private readonly DnsClient tunaDns = new DnsClient(IPAddress.Parse(@"101.6.6.6"), 10000, 5353);

		[TestMethod]
		public void DnsServerDnsSecTest()
		{
			Assert.IsTrue(DnsValidation.IsSupportDnsSec(googleDns));
			Assert.IsTrue(DnsValidation.IsSupportDnsSec(cloudflareDns));
			Assert.IsFalse(DnsValidation.IsSupportDnsSec(dnspod));
			Assert.IsFalse(DnsValidation.IsSupportDnsSec(lugDns));
			Assert.IsTrue(DnsValidation.IsSupportDnsSec(tunaDns));
		}

		[TestMethod]
		public void DomainDnsSecTest()
		{
			var dnsClient = lugDns;
			dnsClient.IsUdpEnabled = false;
			var supportedDomain1 = DomainName.Parse(@"pir.org");
			var supportedDomain2 = DomainName.Parse(@"www.isoc.org");
			var supportedDomain3 = DomainName.Parse(@"paypal.com");
			var unsupportedDomain1 = DomainName.Parse(@"www.google.com");
			var unsupportedDomain2 = DomainName.Parse(@"www.bing.com");

			Assert.IsTrue(DnsValidation.IsSupportDnsSec(supportedDomain1, dnsClient));
			Assert.IsTrue(DnsValidation.IsSupportDnsSec(supportedDomain2, dnsClient));
			Assert.IsTrue(DnsValidation.IsSupportDnsSec(supportedDomain3, dnsClient));
			Assert.IsFalse(DnsValidation.IsSupportDnsSec(unsupportedDomain1, dnsClient));
			Assert.IsFalse(DnsValidation.IsSupportDnsSec(unsupportedDomain2, dnsClient));
		}

		[TestMethod]
		public void EcsTest()
		{
			Assert.IsTrue(DnsValidation.IsSupportEcs(googleDns));
			Assert.IsFalse(DnsValidation.IsSupportEcs(cloudflareDns));
			Assert.IsTrue(DnsValidation.IsSupportEcs(dnspod));
			Assert.IsFalse(DnsValidation.IsSupportEcs(lugDns));
			Assert.IsTrue(DnsValidation.IsSupportEcs(tunaDns));
		}

		[TestMethod]
		public void PoisonTest()
		{
			var pollutedIp1 = IPAddress.Parse(@"88.191.249.183");
			var normalIp = IPAddress.Parse(@"172.217.5.68");
			Assert.IsTrue(DnsValidation.IsPoison(pollutedIp1));
			Assert.IsFalse(DnsValidation.IsPoison(normalIp));
		}
	}
}
