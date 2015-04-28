using HtmlAgilityPack;
using ScrapySharp.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace Rabbit.WeiXinWebApi
{
    /// <summary>
    /// 加密模式。
    /// </summary>
    public enum EncryptMode
    {
        /// <summary>
        /// 明文。
        /// </summary>
        Plaintext = 0,

        /// <summary>
        /// 兼容。
        /// </summary>
        Compatible = 1,

        /// <summary>
        /// 安全。
        /// </summary>
        Safe = 2
    }

    /// <summary>
    /// 微信Web服务接口实现。
    /// </summary>
    public sealed class WeiXinService : IWeiXinService
    {
        #region Field

        private CookieContainer _cookieContainer = new CookieContainer();
        private LoginResult _loginResult;
        private bool _isLogin;

        #endregion Field

        #region Property

        private LoginResult LoginResult
        {
            get { return GetLoginResult(); }
        }

        #endregion Property

        #region Public Method

        /// <summary>
        /// 登录微信。
        /// </summary>
        /// <param name="userName">用户名称。</param>
        /// <param name="password">用户密码。</param>
        /// <returns>登录结果。</returns>
        public LoginResult Login(string userName, string password)
        {
            _isLogin = true;
            _cookieContainer = new CookieContainer();
            using (var client = new LastingWebClient(_cookieContainer))
            {
                client.Headers.Add("Referer", "https://mp.weixin.qq.com/");
                client.Headers.Add("ContentType", "application/x-www-form-urlencoded");
                var data = client.UploadData("https://mp.weixin.qq.com/cgi-bin/login",
                    "POST",
                    Encoding.UTF8.GetBytes("username=" + userName + "&pwd=" + Md5Hash(password) + "&imgcode=&f=json"));

                var token = GetToken(Encoding.UTF8.GetString(data));
                return _loginResult = string.IsNullOrWhiteSpace(token) ? null : new LoginResult
                {
                    Token = token
                };
            }
        }

        /// <summary>
        /// 用户微信用户信息。
        /// </summary>
        /// <returns>用户信息。</returns>
        public UserInfo GetUserInfo()
        {
            using (var client = new LastingWebClient(_cookieContainer))
            {
                var html = Encoding.UTF8.GetString(client.DownloadData(
                    string.Format(
                        "https://mp.weixin.qq.com/cgi-bin/settingpage?t=setting/index&action=index&token={0}&lang=zh_CN",
                        LoginResult.Token)));
                var document = new HtmlDocument();
                document.LoadHtml(html);

                var offset = 0;
                var type = GetMetaContent(document, 4);
                switch (type)
                {
                    case "服务号":
                        offset = 0;
                        break;

                    default:
                        offset = 1;
                        break;
                }

                var user = new UserInfo
                {
                    Picture = new Lazy<byte[]>(() =>
                    {
                        using (var c = new LastingWebClient(_cookieContainer))
                        {
                            return c.DownloadData(
                                document.DocumentNode.CssSelect(".avatar").FirstOrDefault().GetAttributeValue("src").Insert(0, "https://mp.weixin.qq.com"));
                        }
                    }),
                    Name = GetMetaContent(document, 2),
                    LoginEmail = GetMetaContent(document, 10 - offset),
                    OriginalId = GetMetaContent(document, 11 - offset),
                    UserName = GetMetaContent(document, 3),
                    Type = type,
                    AuthenticateStatus = GetMetaContent(document, 6),
                    Body = GetMetaContent(document, 9 - offset),
                    Area = GetMetaContent(document, 8 - offset),
                    Description = GetMetaContent(document, 5),
                    QrCode = new Lazy<byte[]>(() =>
                    {
                        using (var c = new LastingWebClient(_cookieContainer))
                        {
                            return c.DownloadData(
                                document.DocumentNode.CssSelect(".verifyInfo").FirstOrDefault().GetAttributeValue("href").Insert(0, "https://mp.weixin.qq.com"));
                        }
                    })
                };
                return user;
            }
        }

        /// <summary>
        /// 设置开发者接口信息（会自动开启开发者模式）。
        /// </summary>
        /// <param name="url">Url地址。</param>
        /// <param name="token">令牌。</param>
        /// <returns>是否更新成功。</returns>
        public bool SetDevelopInterface(string url, string token)
        {
            return SetDevelopInterface(url, token, "kWdO1ZYB7JoQSCdqCTr20addBAXlWrqeCnQJll2MEM5", EncryptMode.Plaintext);
        }

        /// <summary>
        /// 设置开发者接口信息（会自动开启开发者模式）。
        /// </summary>
        /// <param name="url">Url地址。</param>
        /// <param name="token">令牌。</param>
        /// <param name="aeskey">EncodingAESKey。</param>
        /// <param name="encryptMode">加密模式。</param>
        /// <returns>是否更新成功。</returns>
        public bool SetDevelopInterface(string url, string token, string aeskey, EncryptMode encryptMode)
        {
            //开启开发者模式。
            if (!SetDevelopMode(true))
                throw new WeiXinWebApiException("更新开发者接口信息失败，因为开启开发者模式失败。");

            using (var client = new LastingWebClient(_cookieContainer))
            {
                client.Headers.Add("Referer", "https://mp.weixin.qq.com/");
                client.Headers.Add("ContentType", "application/x-www-form-urlencoded");
                var data = client.UploadData(
                    string.Format("https://mp.weixin.qq.com/advanced/callbackprofile?t=ajax-response&token={0}&lang=zh_CN", LoginResult.Token),
                    "POST", Encoding.UTF8.GetBytes(string.Format("url={0}&callback_token={1}&encoding_aeskey={2}&callback_encrypt_mode={3}", HttpUtility.UrlEncode(url), token, aeskey, (int)encryptMode)));
                var json = Encoding.UTF8.GetString(data);
                return json.Contains("\"err_msg\":\"ok\"");
            }
        }

        /// <summary>
        /// 设置编辑模式。
        /// </summary>
        /// <param name="enable">是否开启编辑模式。</param>
        /// <returns>是否更新成功。</returns>
        public bool SetEditMode(bool enable)
        {
            return SetEditOrDevelopMode(1, enable);
        }

        /// <summary>
        /// 设置开发者模式。
        /// </summary>
        /// <param name="enable">是否开启开发者模式。</param>
        /// <returns>是否更新成功。</returns>
        public bool SetDevelopMode(bool enable)
        {
            return SetEditOrDevelopMode(2, enable);
        }

        /// <summary>
        /// 获取开发者凭据。
        /// </summary>
        /// <returns>如果具有开发者凭据则返回否则返回null。</returns>
        public DevelopCredential GetDevelopCredential()
        {
            using (var client = new LastingWebClient(_cookieContainer))
            {
                var url = string.Format(
                    "https://mp.weixin.qq.com/advanced/advanced?action=dev&t=advanced/dev&token={0}&lang=zh_CN",
                    _loginResult.Token);
                var html = Encoding.UTF8.GetString(client.DownloadData(url));
                var document = new HtmlDocument();
                document.LoadHtml(html);

                var items = document.DocumentNode.CssSelect(".developer_info_item").ToArray();

                if (items.Count() < 4)
                    return null;

                var item = items.First();

                var controls = item.CssSelect(".frm_controls").ToArray();

                string appSecret;
                using (var reader = new StringReader(controls.Last().InnerText.Trim()))
                    appSecret = reader.ReadLine();

                if (!string.IsNullOrWhiteSpace(appSecret))
                {
                    var index = appSecret.Trim().IndexOf(" ", StringComparison.Ordinal);
                    if (index != -1)
                    {
                        appSecret = appSecret.Substring(0, index);
                    }
                }

                return new DevelopCredential
                {
                    AppId = controls.First().InnerText.Trim(),
                    AppSecret = appSecret
                };
            }
        }

        internal sealed class WeiXinAttentionInfo
        {
            public class Item
            {
                //由于微信可能会返回 "-" 数据导致int类型无法转换，故使用string类型，请不要更改类型。

                /// <summary>
                /// 时间。
                /// </summary>
                public DateTime RefDate { get; set; }

                /// <summary>
                /// 新增关注人数。
                /// </summary>
                public string NewUser { get; set; }

                /// <summary>
                /// 取消关注人数。
                /// </summary>
                public string CancelUser { get; set; }

                /// <summary>
                /// 净增关注人数。
                /// </summary>
                public string NetUser { get; set; }

                /// <summary>
                /// 累积关注人数。
                /// </summary>
                public string CumulateUser { get; set; }

                #region Public Method

                /// <summary>
                /// 获取新增关注人数。
                /// </summary>
                public int GetNewUser()
                {
                    return GetNumber(NewUser);
                }

                /// <summary>
                /// 获取取消关注人数。
                /// </summary>
                public int GetCancelUser()
                {
                    return GetNumber(CancelUser);
                }

                /// <summary>
                /// 获取净增关注人数。
                /// </summary>
                public int GetNetUser()
                {
                    return GetNumber(NetUser);
                }

                /// <summary>
                /// 获取累积关注人数。
                /// </summary>
                public long GetCumulateUser()
                {
                    return GetLongNumber(CumulateUser);
                }

                #endregion Public Method

                #region Private Method

                private static int GetNumber(string value)
                {
                    int number;
                    int.TryParse(value, out number);
                    return number;
                }

                private static long GetLongNumber(string value)
                {
                    long number;
                    long.TryParse(value, out number);
                    return number;
                }

                #endregion Private Method
            }

            public Item[] Data { get; set; }
        }

        /// <summary>
        /// 获取关注信息。
        /// </summary>
        /// <param name="startDate">起始日期。</param>
        /// <param name="endDate">结束日期。</param>
        /// <returns>关注信息数组。</returns>
        public AttentionInfo[] GetAttentions(DateTime startDate, DateTime endDate)
        {
            //得到AppId和插件Token。
            var info = GetAppIdAndPluginToken();

            var appId = info.Key;
            var pluginToken = info.Value;

            //关注数据请求Url。
            var url = string.Format(
                "https://mta.qq.com/mta/wechat/ctr_user_summary/get_table_data/?start_date={0}&end_date={1}&need_compare=0&start_compare_date=&end_compare_date=&app_id=&source_list=-1&source_show=35%2C3%2C43%2C17%2C0&appid={2}&pluginid=luopan&token={3}&from=&devtype=2&time_type=day&ajax=1",
                startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"), appId, pluginToken);

            using (var client = new LastingWebClient(_cookieContainer))
            {
                var data = client.DownloadData(url);
                var json = Encoding.UTF8.GetString(data);
                var weiXinAttention = new JavaScriptSerializer().Deserialize<WeiXinAttentionInfo>(json);

                return (weiXinAttention == null || weiXinAttention.Data == null) ? new AttentionInfo[0] : weiXinAttention.Data.Select(i =>
                    new AttentionInfo
                    {
                        CancelUser = i.GetCancelUser(),
                        CumulateUser = i.GetCumulateUser(),
                        Date = i.RefDate,
                        NewUser = i.GetNewUser(),
                        NetUser = i.GetNewUser(),
                    }).ToArray();
            }
        }

        /// <summary>
        /// 设置OAuth回调域名。
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public bool SetOAuthDomain(string domain)
        {
            using (var client = new LastingWebClient(_cookieContainer))
            {
                client.Headers.Add("Referer", "https://mp.weixin.qq.com/");
                var data = client.UploadData("https://mp.weixin.qq.com/merchant/myservice?action=set_oauth_domain&f=json",
                    Encoding.UTF8.GetBytes(
                        string.Format("token={0}&lang=zh_CN&f=json&ajax=1&random=0.18229547117417194&domain={1}",
                            _loginResult.Token, domain)));
                var content = Encoding.UTF8.GetString(data);
                return content.Contains("ok");
            }
        }

        #endregion Public Method

        #region Private Method

        private KeyValuePair<string, string> GetAppIdAndPluginToken()
        {
            using (var client = new LastingWebClient(_cookieContainer))
            {
                client.Headers.Add("Referer", string.Format("https://mp.weixin.qq.com/cgi-bin/home?t=home/index&lang=zh_CN&token={0}", _loginResult.Token));
                var url =
                    string.Format(
                        "https://mp.weixin.qq.com/misc/pluginloginpage?action=stat_user_summary&pluginid=luopan&t=statistics/index&token={0}&lang=zh_CN",
                        _loginResult.Token);
                var html = Encoding.UTF8.GetString(client.DownloadData(url));

                var appId = GetJsonValue(html, "appid : '");
                var pluginToken = GetJsonValue(html, "pluginToken : '");

                return new KeyValuePair<string, string>(appId, pluginToken);
            }
        }

        private static string GetJsonValue(string text, string str)
        {
            var startIndex = text.IndexOf(str, StringComparison.Ordinal);
            text = text.Substring(startIndex + str.Length).Trim();
            return text.Substring(0, text.IndexOf("'", StringComparison.Ordinal)).Trim();
        }

        /// <summary>
        /// 设置编辑或者开发者模式。
        /// </summary>
        /// <param name="type">类型，1代表编辑模式，2代表开发者模式。</param>
        /// <param name="enable">是否开启对应的模式。</param>
        private bool SetEditOrDevelopMode(int type, bool enable)
        {
            using (var client = new LastingWebClient(_cookieContainer))
            {
                client.Headers.Add("ContentType", "application/x-www-form-urlencoded");
                client.Headers.Add("Referer", "https://mp.weixin.qq.com/");

                var data = client.UploadData("https://mp.weixin.qq.com/misc/skeyform?form=advancedswitchform&lang=zh_CN", "POST",
                    Encoding.UTF8.GetBytes(string.Format("flag={0}&type={1}&token={2}", enable ? 1 : 0, type, LoginResult.Token)));
                var json = Encoding.UTF8.GetString(data);
                return json.StartsWith("{\"base_resp\":{\"ret\":0,\"err_msg\":\"ok\"}");
            }
        }

        private static string GetMetaContent(HtmlDocument document, int skip)
        {
            var content = document.DocumentNode.CssSelect(".account_setting_item")
                .Skip(skip)
                .FirstOrDefault()
                .CssSelect(".meta_content")
                .FirstOrDefault();

            return content == null ? string.Empty : FixText(content.InnerText);
        }

        private static string FixText(string text)
        {
            return text.Trim(' ', '\n');
        }

        private static string Md5Hash(string inputString)
        {
            var md5 = new MD5CryptoServiceProvider();
            var encryptedBytes = md5.ComputeHash(Encoding.ASCII.GetBytes(inputString));
            var sb = new StringBuilder();
            foreach (var t in encryptedBytes)
                sb.AppendFormat("{0:x2}", t);
            return sb.ToString();
        }

        private static string GetToken(string jsonText)
        {
            if (string.IsNullOrWhiteSpace(jsonText))
                throw new ArgumentNullException("jsonText");
            const string key = "token=";
            var index = jsonText.IndexOf(key, StringComparison.Ordinal);
            if (index == -1)
                return null;
            return string.Join("", jsonText.Substring(index + key.Length).Where(i =>
            {
                int number;
                return int.TryParse(i.ToString(CultureInfo.InvariantCulture), out number);
            }));
        }

        private LoginResult GetLoginResult()
        {
            if (!_isLogin)
                throw new WeiXinWebApiException("还没有进行登录，请调用Login方法进行登录。");
            if (_loginResult == null)
                throw new WeiXinWebApiException("登录没有成功，请检查用户名或账号密码。");
            return _loginResult;
        }

        #endregion Private Method

        #region Help Class

        /// <summary>
        /// 持久的WebClient。
        /// </summary>
        private class LastingWebClient : WebClient
        {
            private readonly CookieContainer _cookieContainer;

            public LastingWebClient()
                : this(null)
            {
            }

            public LastingWebClient(CookieContainer cookieContainer)
            {
                _cookieContainer = cookieContainer ?? new CookieContainer();
            }

            #region Overrides of WebClient

            /// <summary>
            /// 为指定资源返回一个 <see cref="T:System.Net.WebRequest"/> 对象。
            /// </summary>
            /// <returns>
            /// 一个新的 <see cref="T:System.Net.WebRequest"/> 对象，用于指定的资源。
            /// </returns>
            /// <param name="address">一个 <see cref="T:System.Uri"/>，用于标识要请求的资源。</param>
            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address);
                if (!(request is HttpWebRequest))
                    return request;
                var webRequest = request as HttpWebRequest;
                webRequest.CookieContainer = _cookieContainer;
                return request;
            }

            #endregion Overrides of WebClient
        }

        #endregion Help Class
    }
}