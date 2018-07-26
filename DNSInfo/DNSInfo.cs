using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DNS.Client;
using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using DNS.Protocol.Utils;

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

		private void EnableAllControl()
		{
			button1.Enabled = true;
			comboBox1.Enabled = true;
			textBox2.Enabled = true;
			textBox3.Enabled = true;
			numericUpDown1.Enabled = true;
		}

		private void DisableAllControl()
		{
			button1.Enabled = false;
			comboBox1.Enabled = false;
			textBox2.Enabled = false;
			textBox3.Enabled = false;
			numericUpDown1.Enabled = false;
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
				type = RecordType.NS;
			}
			else if (comboBox1.SelectedIndex == 3)
			{
				type = RecordType.CNAME;
			}
			else if (comboBox1.SelectedIndex == 4)
			{
				type = RecordType.PTR;
			}
			else if (comboBox1.SelectedIndex == 5)
			{
				type = RecordType.MX;
			}
			else if (comboBox1.SelectedIndex == 6)
			{
				type = RecordType.TXT;
			}
			else if (comboBox1.SelectedIndex == 7)
			{
				type = RecordType.ANY;
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

		private static string Response2String(IResponse response, IRequest request, IPEndPoint dns)
		{
			if (response == null)
			{
				Debug.WriteLine(@"无响应");
				return null;
			}
			IResponse res = Response.FromRequest(request);
			var resStr = new StringBuilder();
			foreach (var question in res.Questions)
			{
				var records = response.AnswerRecords;
				string str;

				if (records.Count == 0)
				{
					str = $@"DNS query {question.Name} no answer via {dns}";
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

		private async Task<IResponse> Query(IPEndPoint dns, IRequest request, int timeout = 5000)
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
				_iResponse = clientResponse;
				return clientResponse;
			}
		}

		private IResponse _iResponse = null;
		private void button1_Click(object sender, EventArgs e)
		{
			try
			{
				DisableAllControl();
				textBox1.Text = string.Empty;
				var querystr = textBox3.Text;
				var type = GetRecordType();
				var dns = Common.String2IPEndPoint(textBox2.Text, Convert.ToInt32(numericUpDown1.Value));

				string text;
				double latency;
				var request = GetRequest(dns, type, querystr);
				var istimeout = false;

				var stopwatch = new Stopwatch();
				stopwatch.Start();
				var t = new Task(() =>
				{
					try
					{
						Query(dns, request).Wait();
					}
					catch
					{
						istimeout = true;
					}

				});
				t.Start();
				t.ContinueWith(task =>
				{
					BeginInvoke(new VoidMethodDelegate(() =>
					{
						stopwatch.Stop();
						if (istimeout)
						{
							text=string.Empty;
							toolStripStatusLabel1.Text = $@"{NoResponse}";
						}
						else
						{
							latency = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2);
							text = Response2String(_iResponse, request, dns);
							toolStripStatusLabel1.Text = $@"{ResponseHead}({latency}ms)";
						}
						textBox1.Text = text;
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
