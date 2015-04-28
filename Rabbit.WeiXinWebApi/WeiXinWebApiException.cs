using System;

namespace Rabbit.WeiXinWebApi
{
    /// <summary>
    /// 微信WebApi异常信息。
    /// </summary>
    [Serializable]
    public class WeiXinWebApiException : ApplicationException
    {
        /// <summary>
        /// 初始化一个新的微信WebApi异常信息。
        /// </summary>
        /// <param name="message">异常消息。</param>
        public WeiXinWebApiException(string message)
            : base(message)
        {
        }
    }
}