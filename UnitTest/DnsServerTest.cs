using ARSoft.Tools.Net.Dns;
using DNSInfo.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
	[TestClass]
	public class DnsServerTest
	{
		[TestMethod]
		public void Test()
		{
			var path = @"D:\Cloud\Git\Text_Translation\chndomains.txt";
			var server = new ConditionalForwardingDnsServer(10, 10)
			{
				PureEcs = new ClientSubnetOption(32, IPAddress.Parse(@"38.143.0.121")),
				UpStreamEcs = new ClientSubnetOption(32, IPAddress.Parse(@"202.96.199.133")),
				PureDns = DnsValidationTest.tunaDns,
				UpStreamDns = DnsValidationTest.paiDns
			};
			var list = new List<string>();
			using (var sr = new StreamReader(path, Encoding.UTF8))
			{
				string line;
				while ((line = sr.ReadLine()) != null)
				{
					var domain = line;
					if (!string.IsNullOrWhiteSpace(domain))
					{
						list.Add(domain);
					}
				}
			}
			server.LoadDomains(list);
			server.Start();
			Task.Delay(1000).Wait();
			var client = new DnsClient(IPAddress.Loopback, 10000);
			Assert.IsTrue(DnsValidation.IsSupportDnsSec(client));
			Assert.IsTrue(DnsValidation.IsSupportEcs(client));

			IDnsResolver resolver = new DnsStubResolver(client);
			var addresses = resolver.ResolveHost(@"www.google.com");
			foreach (var ipAddress in addresses)
			{
				Console.WriteLine(ipAddress.ToString());
				Assert.IsFalse(DnsValidation.IsPoison(ipAddress));
			}
		}
	}
}
