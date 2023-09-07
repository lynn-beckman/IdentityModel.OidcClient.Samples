using IdentityModel.OidcClient;
using System;
using System.Text;
using System.Windows.Forms;

namespace WinFormsSample
{
    public partial class Form1 : Form
    {
        OidcClient _oidcClient;

        public Form1()
        {
            InitializeComponent();

            var options = new OidcClientOptions
            {
                Authority = "http://localhost:9001/sso/auth/realms/eco",
                ClientId = "eco-gen-ii",
                Scope = "openid email profile",
                RedirectUri = "http://localhost/winforms.client",

                Browser = new WinFormsWebView()
            };

            _oidcClient = new OidcClient(options);
        }

        private async void LoginButton_Click(object sender, EventArgs e)
        {
            var result = await _oidcClient.LoginAsync();

            if (result.IsError)
            {
                MessageBox.Show(this, result.Error, "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                var sb = new StringBuilder(128);
                foreach (var claim in result.User.Claims)
                {
                    sb.AppendLine($"{claim.Type}: {claim.Value}");
                }

                if (!string.IsNullOrWhiteSpace(result.RefreshToken))
                {
                    sb.AppendLine();
                    sb.AppendLine($"refresh token: {result.RefreshToken}");
                }

                if (!string.IsNullOrWhiteSpace(result.IdentityToken))
                {
                    sb.AppendLine();
                    sb.AppendLine($"identity token: {result.IdentityToken}");
                }

                if (!string.IsNullOrWhiteSpace(result.AccessToken))
                {
                    sb.AppendLine();
                    sb.AppendLine($"access token: {result.AccessToken}");
                }

                Output.Text = sb.ToString();
            }
        }
    }
}