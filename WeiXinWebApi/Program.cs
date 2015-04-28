using Rabbit.WeiXinWebApi;
using System;

namespace WeiXinWebApi
{
    internal class Program
    {
        private static void Main()
        {
            var service = new WeiXinService();

            //登录微信号。
            service.Login("chunsun_cc", "123456");

            //获取用户信息。
            var user = service.GetUserInfo();

            Console.WriteLine("名称：" + user.Name);
            Console.WriteLine("登录邮箱：" + user.LoginEmail);
            Console.WriteLine("原始ID：" + user.OriginalId);
            Console.WriteLine("微信号：" + user.UserName);
            Console.WriteLine("类型：" + user.Type);
            Console.WriteLine("认证情况：" + user.AuthenticateStatus);
            Console.WriteLine("主体信息：" + user.Body);
            Console.WriteLine("地区：" + user.Area);
            Console.WriteLine("功能介绍：" + user.Description);
            Console.WriteLine("是否有头像：" + ((user.Picture.Value == null || user.Picture.Value.Length <= 0) ? "否" : "是"));
            Console.WriteLine("二维码：" + ((user.QrCode.Value == null || user.QrCode.Value.Length <= 0) ? "获取二维码失败！" : "获取成功。"));

            Console.WriteLine();
            Console.WriteLine();

            /*//开启开发者模式。
            service.SetDevelopMode(true);
            //关闭开发者模式。
            service.SetDevelopMode(false);
            //开启编辑模式。
            service.SetEditMode(true);
            //关闭编辑模式。
            service.SetEditMode(false);
            //设置开发接口信息。
            service.SetDevelopInterface("{Url}", "{token}");*/

            //获取开发者凭据。
            var developCredential = service.GetDevelopCredential();
            if (developCredential != null)
            {
                Console.WriteLine("AppId：" + developCredential.AppId);
                Console.WriteLine("AppSecret：" + developCredential.AppSecret);
            }
            else
            {
                Console.WriteLine("用户不是服务号不具有开发者凭据。");
            }

            Console.WriteLine();

            //得到当天的关注信息。
            var attention = service.GetAttentions(DateTime.Now);

            Action<AttentionInfo> writerAttention = model =>
            {
                Console.WriteLine("时间：{0}", model.Date.ToString("yyyy-MM-dd"));
                Console.WriteLine("新关注人数：{0}", model.NewUser);
                Console.WriteLine("取消关注人数：{0}", model.CancelUser);
                Console.WriteLine("净增关注人数：{0}", model.NetUser);
                Console.WriteLine("累积关注人数：{0}", model.CumulateUser);
            };
            var defaultColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("当天关注信息：\r\n");
            Console.ForegroundColor = defaultColor;
            writerAttention(attention);

            Console.WriteLine("\r\n==============================");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("7天之内的关注信息：\r\n");
            Console.ForegroundColor = defaultColor;

            //得到7天之内的关注量
            var attentions = service.GetAttentions(DateTime.Now.Subtract(TimeSpan.FromDays(7)), DateTime.Now);

            foreach (var item in attentions)
            {
                writerAttention(item);
                Console.WriteLine("==============================");
            }
        }
    }
}