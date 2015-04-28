using System;
using System.Linq;

namespace Rabbit.WeiXinWebApi
{
    /// <summary>
    /// 微信登录结果。
    /// </summary>
    public class LoginResult
    {
        /// <summary>
        /// 令牌。
        /// </summary>
        public string Token { get; set; }
    }

    /// <summary>
    /// 微信用户信息。
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// 头像。
        /// </summary>
        public Lazy<byte[]> Picture { get; set; }

        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 登录邮箱。
        /// </summary>
        public string LoginEmail { get; set; }

        /// <summary>
        /// 原始Id。
        /// </summary>
        public string OriginalId { get; set; }

        /// <summary>
        /// 微信号。
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 账号类型。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 地区。
        /// </summary>
        public string Area { get; set; }

        /// <summary>
        /// 说明。
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 二维码。
        /// </summary>
        public Lazy<byte[]> QrCode { get; set; }

        /// <summary>
        /// 认证状态。
        /// </summary>
        public string AuthenticateStatus { get; set; }

        /// <summary>
        /// 主体信息。
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// 是否通过微信认证。
        /// </summary>
        /// <returns>如果通过返回true，否则返回false。</returns>
        public bool IsAuthenticate()
        {
            return !AuthenticateStatus.Equals("未认证");
        }
    }

    /// <summary>
    /// 用户关注信息。
    /// </summary>
    public class AttentionInfo
    {
        /// <summary>
        /// 日期。
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 新增关注人数。
        /// </summary>
        public int NewUser { get; set; }

        /// <summary>
        /// 取消关注人数。
        /// </summary>
        public int CancelUser { get; set; }

        /// <summary>
        /// 净增关注人数。
        /// </summary>
        public int NetUser { get; set; }

        /// <summary>
        /// 累积关注人数。
        /// </summary>
        public long CumulateUser { get; set; }
    }

    /// <summary>
    /// 开发者凭据。
    /// </summary>
    public class DevelopCredential
    {
        /// <summary>
        /// 应用Id。
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// App密钥。
        /// </summary>
        public string AppSecret { get; set; }
    }

    /// <summary>
    /// 微信Web服务接口。
    /// </summary>
    public interface IWeiXinService
    {
        /// <summary>
        /// 登录微信。
        /// </summary>
        /// <param name="userName">用户名称。</param>
        /// <param name="password">用户密码。</param>
        /// <returns>登录结果。</returns>
        LoginResult Login(string userName, string password);

        /// <summary>
        /// 用户微信用户信息。
        /// </summary>
        /// <returns>用户信息。</returns>
        UserInfo GetUserInfo();

        /// <summary>
        /// 设置开发者接口信息（会自动开启开发者模式）。
        /// </summary>
        /// <param name="url">Url地址。</param>
        /// <param name="token">令牌。</param>
        /// <returns>是否更新成功。</returns>
        bool SetDevelopInterface(string url, string token);

        /// <summary>
        /// 设置开发者接口信息（会自动开启开发者模式）。
        /// </summary>
        /// <param name="url">Url地址。</param>
        /// <param name="token">令牌。</param>
        /// <param name="aeskey">EncodingAESKey。</param>
        /// <param name="encryptMode">加密模式。</param>
        /// <returns>是否更新成功。</returns>
        bool SetDevelopInterface(string url, string token, string aeskey, EncryptMode encryptMode);

        /// <summary>
        /// 设置编辑模式。
        /// </summary>
        /// <param name="enable">是否开启编辑模式。</param>
        /// <returns>是否更新成功。</returns>
        bool SetEditMode(bool enable);

        /// <summary>
        /// 设置开发者模式。
        /// </summary>
        /// <param name="enable">是否开启开发者模式。</param>
        /// <returns>是否更新成功。</returns>
        bool SetDevelopMode(bool enable);

        /// <summary>
        /// 获取开发者凭据。
        /// </summary>
        /// <returns>如果具有开发者凭据则返回否则返回null。</returns>
        DevelopCredential GetDevelopCredential();

        /// <summary>
        /// 获取关注信息。
        /// </summary>
        /// <param name="startDate">起始日期。</param>
        /// <param name="endDate">结束日期。</param>
        /// <returns>关注信息数组。</returns>
        AttentionInfo[] GetAttentions(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 设置OAuth回调域名。
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        bool SetOAuthDomain(string domain);
    }

    /// <summary>
    /// 微信服务扩展方法。
    /// </summary>
    public static class WeiXinServiceExtensions
    {
        /// <summary>
        /// 获取关注信息。
        /// </summary>
        /// <param name="weiXinService">微信服务。</param>
        /// <param name="date">日期。</param>
        /// <returns>关注信息数组。</returns>
        public static AttentionInfo GetAttentions(this IWeiXinService weiXinService, DateTime date)
        {
            if (weiXinService == null)
                throw new ArgumentNullException("weiXinService");

            var list = weiXinService.GetAttentions(date.Subtract(TimeSpan.FromDays(1)), date);
            return list == null ? null : list.FirstOrDefault();
        }
    }
}