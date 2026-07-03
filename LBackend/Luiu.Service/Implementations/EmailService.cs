using Luiu.Service.Interfaces;
using Resend;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace Luiu.Service.Implementations
{

    public class EmailService : IEmailService
    {
        private readonly IResend _resend;
        public EmailService(IResend resend)
        {
            _resend = resend;
        }

        public async Task SendVerificationCodeAsync(string toEmail, string verificationToken)
        {
            // 
            var message = new EmailMessage();
            message.From = "Luiu 旅遊回憶記錄 <no-reply@luiu.online>"; // 免費帳號寄件者固定用這個
            message.To.Add(toEmail); 
            message.Subject = $"【Luiu】驗證碼【{verificationToken}】";

            message.HtmlBody = $@"
  <p style=""color: #191e3b; font-size: 14px; line-height: 144%; text-align: left; margin: 8px 32px 24px 32px;"">
    您好！
    <br />
    <br />以下為您的 Email 登入驗證碼，請使用此驗證碼登入您的帳號，驗證碼將於 <strong>10 分鐘</strong> 後失效，為保障帳戶安全，請勿與他人分享。。
    <br />
    <br />您的安全驗證碼是
    <strong>
      {verificationToken}。<br />
    </strong>
    <br />
    如果您沒有提出此要求，請忽略此電子郵件。
    <br />
  </p>

            ";
            //Body = $@"
            //<div style='font-family: sans-serif; max-width: 500px; margin: auto; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
            //    <h2 style='color: #4CAF50;'>歡迎加入 Luiu！</h2>
            //    <p>請點擊下方按鈕以驗證並啟用您的帳號：</p>
            //    <p><a href='{frontendVerifyUrl}' style='padding: 10px 20px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; display:inline-block;'>驗證我的信箱</a></p>
            //</div>";

            // 發送
            await _resend.EmailSendAsync(message);
        }
    }
}