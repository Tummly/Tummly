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
            string otpSection =
                !string.IsNullOrWhiteSpace(otpCode)
                ? $@"
                    <div style='margin: 35px 0;'>

                        <div style='display: inline-block;
                                    background-color: #f8f8f8;
                                    border: 1px solid #dddddd;
                                    padding: 18px 34px;
                                    font-size: 34px;
                                    font-weight: bold;
                                    color: #111111;
                                    letter-spacing: 10px;'>

                            {otpCode}

                        </div>

                        <p style='margin-top: 18px;
                                  color: #666666;
                                  font-size: 14px;'>

                            {expiryInfo}

                        </p>

                    </div>"
                : "";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Tummly Email</title>
</head>

<body style='margin: 0;
             padding: 20px;
             background-color: #f4f4f4;
             font-family: Arial, Helvetica, sans-serif;'>

    <div style='max-width: 600px;
                margin: 0 auto;
                background-color: #ffffff;
                border: 1px solid #e5e5e5;
                border-radius: 0;
                overflow: hidden;'>

        <!-- HEADER -->
        <div style='background-color: #111111;
                    padding: 32px 40px;'>

            <div style='color: #22c55e;
                        font-size: 28px;
                        font-weight: 700;'>

                tummly

            </div>

        </div>

        <!-- CONTENT -->
        <div style='padding: 40px 32px;'>

            <h1 style='font-size: 28px;
                       font-weight: 700;
                       color: #111111;
                       margin: 0 0 20px 0;'>

                {title}

            </h1>

            <p style='font-size: 16px;
                      color: #111111;
                      margin: 0 0 25px 0;'>

                {content}

            </p>

            {otpSection}

        </div>

        <!-- FOOTER -->
        <div style='padding: 48px 32px 38px 32px;
                    border-top: 1px solid #eeeeee;
                    background-color: #f9f9f9;'>

            <div style='font-size: 16px;
                        font-weight: 700;
                        color: #111111;
                        margin-bottom: 12px;'>

                Need help?

            </div>

            <div style='font-size: 14px;
                        color: #666666;
                        line-height: 1.5;'>

                Contact us at
                <a href='mailto:support@tummly.com'
                   style='color: #111111;
                          text-decoration: underline;
                          text-decoration-thickness: 1px;'>

                    support@tummly.com

                </a>

                or visit our
                <a href='#'
                   style='color: #111111;
                          text-decoration: underline;
                          text-decoration-thickness: 1px;'>

                    Help Centre

                </a>.

            </div>

            <div style='margin-top: 24px;
                        font-size: 12px;
                        color: #999999;'>

                If you did not try to sign in to Tummly, contact support.

            </div>

        </div>

    </div>

</body>
</html>
";
        }
    }
}