using System;
using System.Net;

namespace DNSInfo
{
	public static class Common
	{
		public static IPAddress PTRName2IP(string str)
		{
			var s = str.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			return IPAddress.Parse($@"{s[3]}.{s[2]}.{s[1]}.{s[0]}");
		}

		public static IPEndPoint String2IPEndPoint(string strip, int port)
		{
			var ip = IPAddress.Parse(strip);
			return new IPEndPoint(ip,port);
		}
	}
}
