using ARSoft.Tools.Net;
using DNSInfo.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace UnitTest
{
	[TestClass]
	public class DnsValidationTest
	{
		private readonly IPEndPoint googleDns = new IPEndPoint(IPAddress.Parse(@"8.8.8.8"), 53);
		private readonly IPEndPoint cloudflareDns = new IPEndPoint(IPAddress.Parse(@"1.1.1.1"), 53);
		private readonly IPEndPoint dnspod = new IPEndPoint(IPAddress.Parse(@"119.29.29.29"), 53);

		[TestMethod]
		public void DnsServerDnsSecTest()
		{
			Assert.IsTrue(DnsValidation.IsSupportDnsSec(cloudflareDns));
			Assert.IsFalse(DnsValidation.IsSupportDnsSec(dnspod));
		}

		[TestMethod]
		public void DomainDnsSecTest()
		{
			var supportedDomain1 = DomainName.Parse(@"pir.org");
			var supportedDomain2 = DomainName.Parse(@"www.isoc.org");
			var unsupportedDomain = DomainName.Parse(@"www.google.com");
			Assert.IsTrue(DnsValidation.IsSupportDnsSec(supportedDomain1));
			Assert.IsTrue(DnsValidation.IsSupportDnsSec(supportedDomain2));
			Assert.IsFalse(DnsValidation.IsSupportDnsSec(unsupportedDomain));
		}

		[TestMethod]
		public void EcsTest()
		{
			Assert.IsTrue(DnsValidation.IsSupportEcs(googleDns));
			Assert.IsFalse(DnsValidation.IsSupportEcs(cloudflareDns));
			Assert.IsTrue(DnsValidation.IsSupportEcs(dnspod));
		}
	}
}
