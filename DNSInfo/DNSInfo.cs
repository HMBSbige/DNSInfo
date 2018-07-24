using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
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

		private const string Statushead = @"服务器响应：";

		private string str;

		private void EnableAll()
		{
			button1.Enabled = true;
			comboBox1.Enabled = true;
			textBox2.Enabled = true;
			textBox3.Enabled = true;
			checkBox1.Enabled = true;
		}

		private void DisableAll()
		{
			button1.Enabled = false;
			comboBox1.Enabled = false;
			textBox2.Enabled = false;
			textBox3.Enabled = false;
			checkBox1.Enabled = false;
		}

		private RecordType GetType()
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

		private async Task<IResponse> Query(IPEndPoint dns, string querystr, RecordType type)
		{
			var request = new ClientRequest(dns);
			request.Questions.Add(new Question(Domain.FromString(querystr), type));
			request.RecursionDesired = true;

			IResponse res = Response.FromRequest(request);
			var question = res.Questions[0];

			using (var udp = new UdpClient())
			{
				await udp.SendAsync(request.ToArray(), request.Size, dns).WithCancellationTimeout(5000);

				var result = await udp.ReceiveAsync().WithCancellationTimeout(5000);

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
				var re = new ClientResponse(request, response, buffer);

				var records = re.AnswerRecords;
				if (records.Count == 0)
				{
					Debug.WriteLine($@"DNS query {question.Name} no answer via {dns}");
					Console.WriteLine($@"DNS query {question.Name} no answer via {dns}");
				}
				else
				{
					foreach (var record in records)
					{
						if (record.Type == RecordType.A || record.Type == RecordType.AAAA)
						{
							var iprecord = (IPAddressResourceRecord)record;
							Debug.WriteLine($@"DNS query {question.Name} answer {iprecord.IPAddress} via {dns}");
							Console.WriteLine($@"DNS query {question.Name} answer {iprecord.IPAddress} via {dns}");
						}
						else if (record.Type == RecordType.CNAME)
						{
							var cnamerecord = (CanonicalNameResourceRecord)record;
							Debug.WriteLine($@"DNS query {question.Name} answer {cnamerecord.CanonicalDomainName} via {dns}");
							Console.WriteLine($@"DNS query {question.Name} answer {cnamerecord.CanonicalDomainName} via {dns}");
						}
						else if (record.Type == RecordType.PTR)
						{
							var ptrrecord = (PointerResourceRecord)record;
							Debug.WriteLine($@"DNS query {Common.PTRName2IP(question.Name.ToString())} answer {ptrrecord.PointerDomainName} via {dns}");
							Console.WriteLine($@"DNS query {Common.PTRName2IP(question.Name.ToString())} answer {ptrrecord.PointerDomainName} via {dns}");
						}
						else
						{
							Debug.WriteLine($@"DNS query {question.Name} {record.Type} via {dns}");
							Console.WriteLine($@"DNS query {question.Name} {record.Type} via {dns}");
						}
					}
				}
				return re;
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			try
			{
				DisableAll();
				textBox1.Text = string.Empty;
				var querystr = textBox3.Text;
				var type = GetType();
				var dns = Common.String2IPEndPoint(textBox2.Text);

				var text = string.Empty;
				var t = new Task(() =>
				{
					Query(dns, querystr, type).Wait();
				});
				t.Start();
				t.ContinueWith(task =>
				{
					BeginInvoke(new VoidMethodDelegate(() =>
					{
						textBox1.Text = str;
						EnableAll();
					}));
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, @"错误：" + ex.Message, @"错误：", MessageBoxButtons.OK, MessageBoxIcon.Error);
				EnableAll();
			}
		}
	}
}
