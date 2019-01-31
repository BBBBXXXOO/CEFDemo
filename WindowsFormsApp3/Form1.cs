using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Threading;
using Newtonsoft.Json;

namespace WindowsFormsApp3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            TimeSpan ts = new TimeSpan(System.Convert.ToInt32(textExp.Text) * 3600, 0, 0);
            String SAS = CreateSASToken(textKey.Text, "full", ts);
            textBox1.Text = SAS;
            returnLabel.Text = sendSMS(SAS, textPhone.Text);
        }
        public static string CreateSASToken(string key, string keyName, TimeSpan timeout)
        {
            const string Schema = "SharedAccessSignature";
            const string SignKey = "sig";
            const string KeyNameKey = "skn";
            const string ExpiryKey = "se";
            
            var values = new Dictionary<string, string>
             {
              { KeyNameKey, keyName },
              { ExpiryKey, (DateTimeOffset.UtcNow + timeout).ToUnixTimeSeconds().ToString() }
             };

            var signContent = string.Join("&", values
                .Where(pair => pair.Key != SignKey)
                .OrderBy(pair => pair.Key)
                .Select(pair => $"{pair.Key}={WebUtility.UrlEncode(pair.Value)}"));
            string sign;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                sign = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signContent)));
            }

            return $"{Schema} {SignKey}={WebUtility.UrlEncode(sign)}&{signContent}";
        }

        public static string sendSMS(string SASToken, String phoneNumber)
        {
          //HttpClient client = new HttpClient(new LoggingHandler(new HttpClientHandler()));
          String url = "https://cef.chinacloudapi.cn/services/sms/messages?api-version=2018-10-01";
          HttpClient client = new HttpClient();
          client.DefaultRequestHeaders.Add("Account", "ceftest");
          client.DefaultRequestHeaders.Add("Authorization", SASToken);
          JObject message = new JObject(
                new JProperty("phonenumber", 
                    new JArray(phoneNumber)
                  ),
                new JProperty("extend", "09"),
                new JProperty("messageBody",
                   new JObject(
                       new JProperty("templateName", "Notification1"),
                       new JProperty("templateParam",
                           new JObject(
                              new JProperty("username", "testuser"),
                              new JProperty("ticketnumber", "xxx0001")
                         )))));
            Console.WriteLine(message.ToString());
            StringContent content = new StringContent(message.ToString(), Encoding.UTF8, "application/json");
            var response = client.PostAsync(url, content).Result;
            var body = response.Content.ReadAsStringAsync().Result;
            return body.ToString();
          
        }


    }
    public class LoggingHandler : DelegatingHandler
    {
        public LoggingHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Console.WriteLine("Request:");
            Console.WriteLine(request.ToString());
            //request.Content = new StringContent("Hello world");
            
            if (request.Content != null)
            {
                Console.WriteLine(await request.Content.ReadAsStringAsync());
            }
            Console.WriteLine();
            
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            Console.WriteLine("Response:");
            Console.WriteLine(response.ToString());
            if (response.Content != null)
            {
                Console.WriteLine(await response.Content.ReadAsStringAsync());
            }
            Console.WriteLine();
            
            return response;
        }
    }
}

