using ARSoft.Tools.Net.Dns;
using DNSInfo.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

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
				PureDns = new DnsClient(IPAddress.Parse(@"123.206.209.64"), 3000, 5533),
				UpStreamDns = new DnsClient(IPAddress.Parse(@"101.226.4.6"), 3000, 53)
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
			Console.ReadLine();
		}
	}
}
