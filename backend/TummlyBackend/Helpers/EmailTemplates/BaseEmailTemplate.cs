namespace TummlyBackend.Helpers.EmailTemplates
{
    public static class BaseEmailTemplate
    {
        public static string GenerateTemplate(
            string title,
            string content,
            string otpCode = "",
            string expiryInfo = "This code expires in 10 minutes."
        )
        {
            /*
             =========================================
             OTP SECTION
             =========================================
            */

            string otpSection =
                !string.IsNullOrWhiteSpace(otpCode)
                ? $@"

<div class='otp-container'>

    <div class='otp-box'>
        {otpCode}
    </div>

    <p class='expiry-text'>
        {expiryInfo}
    </p>

</div>

"
                : "";

            /*
             =========================================
             HTML TEMPLATE
             =========================================
            */

            return $@"

<!DOCTYPE html>

<html>

<head>

<meta charset='UTF-8'>

<meta
    name='viewport'
    content='width=device-width, initial-scale=1.0'
>

<title>
Tummly Email
</title>

<style>

body {{
    margin: 0;
    padding: 0;
    background-color: #f4f4f4;
    font-family: Arial, Helvetica, sans-serif;
}}

.wrapper {{
    width: 100%;
    padding: 40px 15px;
    box-sizing: border-box;
}}

.email-container {{
    max-width: 620px;
    margin: auto;
    background-color: #ffffff;
    border-radius: 10px;
    overflow: hidden;
    border: 1px solid #e5e5e5;
}}

.header {{
    background-color: #111111;
    padding: 28px 35px;
}}

.logo {{
    color: #22c55e;
    font-size: 30px;
    font-weight: bold;
    letter-spacing: -0.5px;
}}

.content {{
    padding: 45px 40px 35px 40px;
}}

.title {{
    font-size: 28px;
    font-weight: 700;
    color: #111111;
    margin-bottom: 28px;
}}

.text {{
    font-size: 16px;
    color: #444444;
    line-height: 1.7;
    margin-bottom: 18px;
}}

.otp-container {{
    margin-top: 35px;
    margin-bottom: 35px;
}}

.otp-box {{
    display: inline-block;
    background-color: #f8f8f8;
    border: 1px solid #dddddd;
    border-radius: 10px;
    padding: 18px 34px;
    font-size: 34px;
    font-weight: bold;
    color: #111111;
    letter-spacing: 10px;
}}

.expiry-text {{
    margin-top: 18px;
    color: #666666;
    font-size: 14px;
}}

.footer {{
    border-top: 1px solid #eeeeee;
    padding: 35px 40px;
    position: relative;
}}

.footer-title {{
    font-size: 16px;
    font-weight: bold;
    color: #111111;
    margin-bottom: 12px;
}}

.footer-text {{
    font-size: 14px;
    color: #666666;
    line-height: 1.7;
}}

.footer-text a {{
    color: #444444;
    text-decoration: underline;
}}

.address {{
    margin-top: 24px;
    font-size: 13px;
    color: #888888;
    line-height: 1.8;
}}

.bottom-links {{
    margin-top: 28px;
    font-size: 13px;
}}

.bottom-links a {{
    color: #555555;
    text-decoration: underline;
    margin-right: 12px;
}}

.close-icon {{
    position: absolute;
    top: 30px;
    right: 35px;
    color: #999999;
    font-size: 18px;
    font-weight: bold;
}}

@media screen and (max-width: 600px)
{{
    .content {{
        padding: 35px 24px;
    }}

    .footer {{
        padding: 30px 24px;
    }}

    .title {{
        font-size: 24px;
    }}

    .otp-box {{
        font-size: 28px;
        padding: 16px 24px;
        letter-spacing: 7px;
    }}
}}

</style>

</head>

<body>

<div class='wrapper'>

<div class='email-container'>

    <!-- HEADER -->

    <div class='header'>

        <div class='logo'>
            tummly
        </div>

    </div>

    <!-- CONTENT -->

    <div class='content'>

        <div class='title'>
            {title}
        </div>

        <div class='text'>
            {content}
        </div>

        {otpSection}

    </div>

    <!-- FOOTER -->

    <div class='footer'>

        <div class='close-icon'>
            ×
        </div>

        <div class='footer-title'>
            Need help?
        </div>

        <div class='footer-text'>

            Contact us at

            <a href='mailto:Support@tummly.com'>
                Support@tummly.com
            </a>

            or visit our Help Center

        </div>

        <div class='address'>

            71-75 Shelton Street,
            Covent Garden,

            <br>

            London,
            United Kingdom,
            WC2H 9JQ

        </div>

        <div class='bottom-links'>

            <a href='#'>
                Privacy Policy
            </a>

            |

            <a href='#'>
                Terms of Service
            </a>

        </div>

    </div>

</div>

</div>

</body>

</html>

";
        }
    }
}