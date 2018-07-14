using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using LumiSoft.Net.DNS;
using LumiSoft.Net.DNS.Client;

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

		private void button1_Click(object sender, EventArgs e)
		{
			try
			{
				DisableAll();
				textBox1.Text = string.Empty;
				var querystr = textBox3.Text;
				DNS_QType type;
				if (comboBox1.SelectedIndex == 0)
				{
					type = DNS_QType.A;
				}
				else if (comboBox1.SelectedIndex == 1)
				{
					type = DNS_QType.NS;
				}
				else if (comboBox1.SelectedIndex == 2)
				{
					type = DNS_QType.CNAME;
				}
				else if (comboBox1.SelectedIndex == 3)
				{
					type = DNS_QType.SOA;
				}
				else if (comboBox1.SelectedIndex == 4)
				{
					type = DNS_QType.PTR;
				}
				else if (comboBox1.SelectedIndex == 5)
				{
					type = DNS_QType.HINFO;
				}
				else if (comboBox1.SelectedIndex == 6)
				{
					type = DNS_QType.MX;
				}
				else if (comboBox1.SelectedIndex == 7)
				{
					type = DNS_QType.TXT;
				}
				else if (comboBox1.SelectedIndex == 8)
				{
					type = DNS_QType.AAAA;
				}
				else if (comboBox1.SelectedIndex == 9)
				{
					type = DNS_QType.ANY;
				}
				else
				{
					throw new Exception(@"无效查询类型");
				}

				Dns_Client.DnsServers = textBox2.Text.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);
				Dns_Client.UseDnsCache = checkBox1.Checked;

				DnsServerResponse reponse = null;
				double latency = 0;

				var t = new Task(() =>
				{
					using (var dns = new Dns_Client())
					{
						var stopwatch = new Stopwatch();
						stopwatch.Start();
						reponse = dns.Query(querystr, type);
						stopwatch.Stop();
						latency = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2);
					}
				});
				t.Start();
				t.ContinueWith(task =>
				{
					BeginInvoke(new VoidMethodDelegate(() =>
					{
						if (reponse == null)
						{
							toolStripStatusLabel1.Text = Statushead + @"DNS 服务器未响应!";
							EnableAll();
							return;
						}
						if (!reponse.ConnectionOk)
						{
							toolStripStatusLabel1.Text = Statushead + @"连接到 DNS 服务器失败!";
							EnableAll();
							return;
						}
						toolStripStatusLabel1.Text = $@"{Statushead}{reponse.ResponseCode}({latency}ms)";

						var text = string.Empty;

						var tempStr = Records_To_String(reponse.Answers);
						if (!string.IsNullOrWhiteSpace(tempStr))
						{
							text += @"非权威应答:" + Environment.NewLine;
							text += tempStr;
						}

						tempStr = Records_To_String(reponse.AuthoritiveAnswers);
						if (!string.IsNullOrWhiteSpace(tempStr))
						{
							text += @"权威应答:" + Environment.NewLine;
							text += tempStr;
						}

						tempStr = Records_To_String(reponse.AdditionalAnswers);
						if (!string.IsNullOrWhiteSpace(tempStr))
						{
							text += @"额外应答:" + Environment.NewLine;
							text += tempStr;
						}

						textBox1.Text = text;
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

		private string Records_To_String(DNS_rr[] records)
		{
			var text = string.Empty;

			for (var i = 0; i < records.Length; ++i)
			{
				var record = records[i];

				text += @"Record " + (i + 1) + @": Name=""" + record.Name + @""", Type=" + record.RecordType + @", TTL=" + record.TTL + Environment.NewLine;

				if (record.RecordType == DNS_QType.A)
				{
					var aRec = (DNS_rr_A)record;

					text += @"IP: " + aRec.IP + Environment.NewLine;
				}
				else if (record.RecordType == DNS_QType.NS)
				{
					var nsRec = (DNS_rr_NS)record;

					text += @"名称服务器: " + nsRec.NameServer + Environment.NewLine;
				}
				else if (record.RecordType == DNS_QType.CNAME)
				{
					var cnameRec = (DNS_rr_CNAME)record;

					text += @"别名: " + cnameRec.Alias + Environment.NewLine;
				}
				else if (record.RecordType == DNS_QType.SOA)
				{
					var soaRec = (DNS_rr_SOA)record;

					text += @"域名服务器: " + soaRec.NameServer + Environment.NewLine;
					text += @"管理员邮箱: " + soaRec.AdminEmail + Environment.NewLine;
				}
				else if (record.RecordType == DNS_QType.PTR)
				{
					var ptrRec = (DNS_rr_PTR)record;

					text += @"域名: " + ptrRec.DomainName + Environment.NewLine;
				}
				else if (record.RecordType == DNS_QType.HINFO)
				{
					var hinfoRec = (DNS_rr_HINFO)record;

					text += @"操作系统: " + hinfoRec.OS + Environment.NewLine;
					text += @"CPU: " + hinfoRec.CPU + Environment.NewLine;
				}
				else if (record.RecordType == DNS_QType.MX)
				{
					var mxRec = (DNS_rr_MX)record;

					text += @"MX优先级: " + mxRec.Preference + Environment.NewLine;
					text += @"邮箱域名: " + mxRec.Host + Environment.NewLine;
				}
				else if (record.RecordType == DNS_QType.TXT)
				{
					var txtRec = (DNS_rr_TXT)record;

					text += @"文本: " + txtRec.Text + Environment.NewLine;
				}
				else if (record.RecordType == DNS_QType.AAAA)
				{
					var aRec = (DNS_rr_AAAA)record;

					text += @"IP: " + aRec.IP + Environment.NewLine;
				}

				text += Environment.NewLine;
			}

			return text;
		}

		private void DNSInfo_Resize(object sender, EventArgs e)
		{

		}
	}
}
