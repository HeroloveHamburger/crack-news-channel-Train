
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using static TVBS.MainWindow;
using Microsoft.Web.WebView2.Wpf;
using WebView2 = Microsoft.Web.WebView2.Wpf.WebView2;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Windows.Threading;
using System.Collections;
using System.Diagnostics;
using System.Web;

namespace TVBS
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// code on https://stackoverflow.com/questions/66914165/how-to-decrypt-aes-cbc
        /// </summary>
        public class AESHelper
        {
            private AesCryptoServiceProvider _aes;
            private ICryptoTransform _crypto;
            
            public AESHelper(string key, string IV)
            {
                _aes = new AesCryptoServiceProvider();
                _aes.BlockSize = 128;
                _aes.KeySize = 256;
                _aes.Key = ASCIIEncoding.ASCII.GetBytes(key);
                if (!string.IsNullOrEmpty(IV))
                {
                    _aes.IV = ASCIIEncoding.ASCII.GetBytes(IV);
                }
                _aes.Padding = PaddingMode.PKCS7;
                _aes.Mode = CipherMode.CBC;
            }

            public string encrypt(string message)
            {
                _crypto = _aes.CreateEncryptor(_aes.Key, _aes.IV);
                byte[] encrypted = _crypto.TransformFinalBlock(
                    ASCIIEncoding.UTF8.GetBytes(message), 0, ASCIIEncoding.UTF8.GetBytes(message).Length);
                _crypto.Dispose();
                return System.Convert.ToBase64String(encrypted);
            }

            public string decrypt(string message)
            {
                _crypto = _aes.CreateDecryptor(_aes.Key, _aes.IV);
                byte[] decrypted = _crypto.TransformFinalBlock(
                    System.Convert.FromBase64String(message), 0, System.Convert.FromBase64String(message).Length);
                _crypto.Dispose();
                return ASCIIEncoding.ASCII.GetString(decrypted);
            }
        }
        /// <summary>
        /// initialization vector
        /// </summary>
        string siv { get;set;}= "JUMxvVMmszqUTeKn";
        /// <summary>
        /// encrypt decrypt key
        /// </summary>
        string skey { get; set; } = "ilyB29ZdruuQjC45JhBBR7o2Z8WJ26Vg";

        /// <summary>
        /// 儲存需破解的字串
        /// </summary>
        static string sneedcrack { get; set; } = "";
        /// <summary>
        /// 儲存電視頻道網址用的變數
        /// </summary>
        static string TVBSUrl { get; set; } = "";
        /// <summary>
        /// 固定破解用的javascript 功能
        /// </summary>
        string javascriptFirst { get; set; } = "var jsonData ; \r\n async function logJSONData() {\r\n  const response = await fetch(\"https://api2.4gtv.tv//Channel/GetChannelUrl3\", {\r\n  \"headers\": {\r\n    \"accept\": \"*/*\",\r\n    \"accept-language\": \"zh-TW,zh;q=0.9,en-US;q=0.8,en;q=0.7,zh-CN;q=0.6\",\r\n    \"cache-control\": \"no-cache\",\r\n    \"content-type\": \"application/x-www-form-urlencoded; charset=UTF-8\",\r\n    \"pragma\": \"no-cache\",\r\n    \"sec-ch-ua\": \"\\\"Not.A/Brand\\\";v=\\\"8\\\", \\\"Chromium\\\";v=\\\"114\\\", \\\"Google Chrome\\\";v=\\\"114\\\"\",\r\n    \"sec-ch-ua-mobile\": \"?0\",\r\n    \"sec-ch-ua-platform\": \"\\\"Windows\\\"\",\r\n    \"sec-fetch-dest\": \"empty\",\r\n    \"sec-fetch-mode\": \"cors\",\r\n    \"sec-fetch-site\": \"same-site\"\r\n  },\r\n  \"referrer\": \"https://www.4gtv.tv/\",\r\n  \"referrerPolicy\": \"strict-origin-when-cross-origin\",\r\n  \"body\": \"value=";
        /// <summary>
        /// 會因頻道不同而替換的value變數 (預設TVBS新聞)
        /// </summary>
        string javascriptMid { get; set; } = "AWjB%2BiPGYznmXp6O%2B9Bl3%2FvYTY9PoCH%2BsnaP5iPvEumCXNFg4%2FOMHvazNv%2Btr8NcqJDslT8L7SOdx7kvNP6ceoCIkhuMklo3EguX8ibpPyoOA7LUi3v%2BDsJYArl4yAhCVo3WenZWqP5inqHDwPN74sAI1UYwx7Vzkxb0hNmx2vU%3D";
        /// <summary>
        /// 串接最後串接變數
        /// </summary>
        string javascriptLast { get; set; } = "\",\r\n  \"method\": \"POST\",\r\n  \"mode\": \"cors\",\r\n  \"credentials\": \"omit\"\r\n});\r\n  jsonData = await response.json();\r\n  console.log(jsonData);\r\n    return jsonData;\r\n}";
        /// <summary>
        /// 儲存需加密的變數
        /// </summary>
        string sneedencrypt { get; set; } = "";

        /// <summary>
        /// 預設129頻道
        /// </summary>
        string channel = "129";
        /// <summary>
        /// 網頁位址
        /// </summary>
        string url = "https://api2.4gtv.tv/Channel/GetChannel/";

        DispatcherTimer _timer = new DispatcherTimer();

        DispatcherTimer _timer2 = new DispatcherTimer();

        Process process = new Process();

        JObject o = new JObject();

        /// <summary>
        /// 封裝加密資料
        /// </summary>
        /// <param name="channel">頻道</param>
        /// <param name="fsASSET_ID">編號</param>
        /// <returns></returns>
        public string encryptJsonString(string channel,string fsASSET_ID)
        {
            string numString = "{\"fnCHANNEL_ID\":\""+channel+"\",\"fsASSET_ID\":\""+ fsASSET_ID + "\",\"fsDEVICE_TYPE\":\"pc\",\"clsIDENTITY_VALIDATE_ARUS\":{\"fsVALUE\":\"\"}}";
            return numString;
        }

        /// <summary>
        /// 異步獲取電視頻道資料
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task get(string url)
        {
            await webview.EnsureCoreWebView2Async(null);

            webview.CoreWebView2.Navigate(url);

            TVBSUrl = "";
            sneedcrack = "";
        }
        /// <summary>
        /// 若頁面成功將執行_timer與_timer2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        async void NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            _timer.Start();
            _timer2.Start();
        }

        async void _timer_Tick(object sender, EventArgs e)
        {
            //串接javascript
            string javascript = javascriptFirst + javascriptMid + javascriptLast;
            //在CoreWebView2 執行串接javascript 注入
            await webview.CoreWebView2.ExecuteScriptAsync(javascript);
            //在CoreWebView2 執行串接的javascript
            await webview.CoreWebView2.ExecuteScriptAsync("logJSONData()");
            //異步等待jsonData.Data 並存進 sneedcrack
            sneedcrack = await webview.CoreWebView2.ExecuteScriptAsync("jsonData.Data");
            //清空 javascript裡的 jsonData變數
            await webview.CoreWebView2.ExecuteScriptAsync("var jsonData = ''");
            //判斷是否為空字串或 "null"
            if (sneedcrack != "" && sneedcrack != "null")
            {
                sneedcrack = sneedcrack.Replace('\"', ' ');
                _timer.Stop();
                //解密
                AESHelper aESHelper = new AESHelper(skey, siv);
                string check = aESHelper.decrypt(sneedcrack);
                //轉為json
                JObject json = JObject.Parse(check);
                //獲取可看的網頁並存進 TVBSUrl
                TVBSUrl = json["flstURLs"][json["flstURLs"].Count() - 1].ToString();
                //清空 sneedcrack
                sneedcrack = "";
            }
        }

        void _timer_Tick2(object sender, EventArgs e)
        {
            //判斷 TVBSUrl不為空字串
            if (TVBSUrl != "")
            {
                _timer2.Stop();
                //播放
                process = Process.Start("mpv.exe", TVBSUrl);
                TVBSUrl = "";
            }
        }


        async void InitializeAsync()
        {
            var fuckhandler = new HttpClientHandler() {UseCookies = true };

            HttpClient getFuckClient = new HttpClient(fuckhandler);

            string fuckurl = "https://api2.4gtv.tv/Channel/GetChannelBySetId/1/pc/L";

            var fuckhtml = await getFuckClient.GetStringAsync(fuckurl);

            o = JObject.Parse(fuckhtml);

            Dictionary<string, string> BoxArray = new Dictionary<string, string>();

            for (int i = 0; i < o["Data"].Count();i++)
            {
                
                BoxArray.Add(o["Data"][i]["fsNAME"].ToString(), o["Data"][i]["fnID"].ToString());
                
            }
            TVList.ItemsSource = BoxArray;
        }

        public MainWindow()
        {
            InitializeComponent();

            Uri uri = new Uri(url);

            webview.NavigationCompleted += NavigationCompleted;

            _timer.Interval = TimeSpan.FromMilliseconds(1);

            _timer.Tick += _timer_Tick;

            _timer2.Interval = TimeSpan.FromMilliseconds(1);

            _timer2.Tick += _timer_Tick2;
            
            InitializeAsync();

        }

        private void TVList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox TVList = sender as ListBox;

            KeyValuePair<string,string> keyValuePair = (KeyValuePair<string, string>)e.AddedItems[0];
            
            channel = keyValuePair.Value;

            sneedencrypt = encryptJsonString(channel, o["Data"][TVList.SelectedIndex]["fs4GTV_ID"].ToString());
            AESHelper aESHelper = new AESHelper(skey, siv);
            string check = aESHelper.encrypt(sneedencrypt);
            check = HttpUtility.UrlEncode(check);
            javascriptMid = check;
        }

        private async void 選擇_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                process.Kill();
            }
            catch
            {

            }
            await get(url + channel);
        }
    }

}
