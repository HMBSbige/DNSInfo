using DNS.Client;
using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using DNS.Protocol.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DNSInfo
{
	public partial class DNSInfo : Form
	{
		public DNSInfo()
		{
			InitializeComponent();
			comboBox1.SelectedIndex = 0;
		}

		private delegate void VoidMethodDelegate();

		private const string ResponseHead = @"服务器响应：";
		private const string NoResponse = @"服务器未响应！";
		private const string ResponseTimeout = @"服务器响应超时！";
		private const string DnsServerError = @"DNS 服务器格式错误！";
		private const int DefaultTimeout = 1000;

		private void EnableAllControl()
		{
			button1.Enabled = true;
			comboBox1.Enabled = true;
			textBox2.Enabled = true;
			textBox3.Enabled = true;
		}

		private void DisableAllControl()
		{
			button1.Enabled = false;
			comboBox1.Enabled = false;
			textBox2.Enabled = false;
			textBox3.Enabled = false;
		}

		private RecordType GetRecordType()
		{
			RecordType type;
			if (comboBox1.SelectedIndex == 0)
			{
				type = RecordType.A;
			}
			else if (comboBox1.SelectedIndex == 1)
			{
				type = RecordType.AAAA;
			}
			else if (comboBox1.SelectedIndex == 2)
			{
				type = RecordType.CNAME;
			}
			else if (comboBox1.SelectedIndex == 3)
			{
				type = RecordType.PTR;
			}
			else
			{
				throw new Exception(@"无效查询类型");
			}

			return type;
		}

		private IRequest GetRequest(IPEndPoint dns, RecordType type, string querystr)
		{
			var request = new ClientRequest(dns);
			request.Questions.Add(new Question(Domain.FromString(querystr), type));
			request.RecursionDesired = true;
			return request;
		}

		private static string Response2String(IResponse response, IPEndPoint dns)
		{
			if (response == null)
			{
				var str = $@"*No Response from {dns}";
				Debug.WriteLine(str);
				Console.WriteLine(str);
				return str;
			}
			var resStr = new StringBuilder();
			foreach (var question in response.Questions)
			{
				var records = response.AnswerRecords;
				string str;

				if (records.Count == 0)
				{
					if (question.Type == RecordType.PTR)
					{
						str = $@"*DNS query {Common.PTRName2IP(question.Name.ToString())} no answer via {dns}";
					}
					else
					{
						str = $@"*DNS query {question.Name} no answer via {dns}";
					}
					Debug.WriteLine(str);
					Console.WriteLine(str);
					resStr.AppendLine(str);
				}
				else
				{
					foreach (var record in records)
					{
						if (record.Type == RecordType.A || record.Type == RecordType.AAAA)
						{
							var iprecord = (IPAddressResourceRecord)record;
							str = $@"DNS query {question.Name} answer {iprecord.IPAddress} via {dns}";
							Debug.WriteLine(str);
							Console.WriteLine(str);
							resStr.AppendLine(str);
						}
						else if (record.Type == RecordType.CNAME)
						{
							var cnamerecord = (CanonicalNameResourceRecord)record;
							str = $@"DNS query {question.Name} answer {cnamerecord.CanonicalDomainName} via {dns}";
							Debug.WriteLine(str);
							Console.WriteLine(str);
							resStr.AppendLine(str);
						}
						else if (record.Type == RecordType.PTR)
						{
							var ptrrecord = (PointerResourceRecord)record;
							str = $@"DNS query {Common.PTRName2IP(question.Name.ToString())} answer {ptrrecord.PointerDomainName} via {dns}";
							Debug.WriteLine(str);
							Console.WriteLine(str);
							resStr.AppendLine(str);
						}
						else
						{
							str = $@"DNS query {question.Name} {record.Type} via {dns}";
							Debug.WriteLine(str);
							Console.WriteLine(str);
							resStr.AppendLine(str);
						}
					}
				}
			}
			return resStr.ToString();
		}

		private static async Task<IResponse> Query(IPEndPoint dns, IRequest request, int timeout = DefaultTimeout)
		{
			using (var udp = new UdpClient())
			{
				await udp.SendAsync(request.ToArray(), request.Size, dns).WithCancellationTimeout(timeout);

				var result = await udp.ReceiveAsync().WithCancellationTimeout(timeout);

				if (!result.RemoteEndPoint.Equals(dns))
				{
					throw new IOException(@"Remote endpoint mismatch");
				}
				var buffer = result.Buffer;
				var response = Response.FromArray(buffer);

				if (response.Truncated)
				{
					return await new NullRequestResolver().Resolve(request);
				}
				var clientResponse = new ClientResponse(request, response, buffer);
				return clientResponse;
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			try
			{
				DisableAllControl();
				textBox1.Text = string.Empty;
				var querystr = textBox3.Text;
				var type = GetRecordType();

				if (type == RecordType.PTR)
				{
					querystr = Common.IPStr2PTRName(querystr);
				}

				if (string.IsNullOrWhiteSpace(querystr))
				{
					throw new IOException(@"Domain Error!");
				}

				var dnsS = Common.ToIPEndPoints(textBox2.Text, 53, new[] { ',', '，' }) as IPEndPoint[];
				if (dnsS.Length == 0)
				{
					throw new Exception(DnsServerError);
				}
				var text = string.Empty;
				var latency = 0.0;
				var t = new Task(() =>
				{
					foreach (var dns in dnsS)
					{
						var isover = false;
						var request = GetRequest(dns, type, querystr);
						IResponse clientResponse = null;
						var istimeout = false;

						var stopwatch = new Stopwatch();
						stopwatch.Start();
						try
						{
							clientResponse = Task.Run(() => Query(dns, request)).GetAwaiter().GetResult();
						}
						catch
						{
							istimeout = true;
						}
						stopwatch.Stop();
						if (istimeout)
						{
							text = string.Empty;
						}
						else
						{
							latency += stopwatch.Elapsed.TotalMilliseconds;
							text = Response2String(clientResponse, dns);
							if (text[0] != '*')
							{
								isover = true;
							}
						}
						if (isover)
						{
							break;
						}
					}
				});
				t.Start();
				t.ContinueWith(task =>
				{
					BeginInvoke(new VoidMethodDelegate(() =>
					{
						if (text.StartsWith(@"*No Response"))
						{
							toolStripStatusLabel1.Text = $@"{NoResponse}";
						}
						else if (string.IsNullOrWhiteSpace(text))
						{
							toolStripStatusLabel1.Text = $@"{ResponseTimeout}";
						}
						else
						{
							toolStripStatusLabel1.Text = $@"{ResponseHead}({Math.Round(latency, 2)}ms)";
							textBox1.Text += text;
						}
						EnableAllControl();
					}));
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, @"错误：" + ex.Message, @"错误：", MessageBoxButtons.OK, MessageBoxIcon.Error);
				EnableAllControl();
			}
		}
	}
}
