using IdentityModel.OidcClient;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Windows;

namespace WpfHttpSys
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private OidcClient _oidcClient;
        private string _refreshToken;
        private string _claims;
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // create a redirect URI using an available port on the loopback address.
            string redirectUri = string.Format("http://127.0.0.1:7890/");
            Console.WriteLine("redirect URI: " + redirectUri);

            // create an HttpListener to listen for requests on that redirect URI.
            var http = new HttpListener();
            http.Prefixes.Add(redirectUri);
            Console.WriteLine("Listening..");
            http.Start();

            var options = new OidcClientOptions()
            {
                Authority = "http://localhost:9001/sso/auth/realms/eco",
                ClientId = "eco-gen-ii",
                Scope = "openid profile email",
                RedirectUri = redirectUri
            };

            _oidcClient = new OidcClient(options);
            var state = await _oidcClient.PrepareLoginAsync();
            Console.WriteLine($"Start URL: {state.StartUrl}");

            // open system browser to start authentication
            var psi = new ProcessStartInfo(state.StartUrl)
            {
                UseShellExecute = true,
            };
            Process.Start(psi);

            // Wait for the authorization response.
            var context = await http.GetContextAsync();

            var formData = GetRequestPostData(context.Request);

            // Brings the Console to Focus.
            //BringConsoleToFront();

            // sends an HTTP response to the browser.
            var response = context.Response;
            string responseString = "<html><head><script type='text/javascript'>function closeMe() {window.close();} setTimeout(closeMe, 1000); </script></head><body>Login successful, please return to the app.</body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            await responseOutput.WriteAsync(buffer, 0, buffer.Length);
            responseOutput.Close();

            Console.WriteLine($"Form Data: {formData}");
            var result = await _oidcClient.ProcessResponseAsync(formData, state);

            if (result.IsError)
            {
                Console.WriteLine("\n\nError:\n{0}", result.Error);
            }
            else
            {
                Console.WriteLine("\n\nClaims:");
                _claims = string.Empty;
                foreach (var claim in result.User.Claims)
                {
                    Console.WriteLine("{0}: {1}", claim.Type, claim.Value);
                    _claims = $"{_claims}{claim.Type}: {claim.Value}{Environment.NewLine}";
                }
                txbMessage.Text = $"{_claims}{Environment.NewLine}";

                Console.WriteLine();
                Console.WriteLine("Access token:\n{0}", result.AccessToken);
                txbMessage.Text = $"{txbMessage.Text}AccessToken: {result.AccessToken}{Environment.NewLine}";

                if (!string.IsNullOrWhiteSpace(result.RefreshToken))
                {
                    _refreshToken = result.RefreshToken;
                    Console.WriteLine("Refresh token:\n{0}", result.RefreshToken);
                    txbMessage.Text = $"{txbMessage.Text}{Environment.NewLine}RefreshToken: {result.RefreshToken}";
                }
            }
            http.Stop();
        }
 
        private async void Btn_Click(object sender, RoutedEventArgs e)
        {
            var refreshTokenResult = await _oidcClient.RefreshTokenAsync(_refreshToken);
            if (!refreshTokenResult.IsError)
            {
                Console.WriteLine();
                txbMessage.Text = $"{_claims}{Environment.NewLine}";
                Console.WriteLine("Access token:\n{0}", refreshTokenResult.AccessToken);
                txbMessage.Text = $"{txbMessage.Text}AccessToken: {refreshTokenResult.AccessToken}{Environment.NewLine}";

                if (!string.IsNullOrWhiteSpace(refreshTokenResult.RefreshToken))
                {
                    _refreshToken = refreshTokenResult.RefreshToken;
                    Console.WriteLine("Refresh token:\n{0}", refreshTokenResult.RefreshToken);
                    txbMessage.Text = $"{txbMessage.Text}{Environment.NewLine}RefreshToken: {refreshTokenResult.RefreshToken}";
                }
            }
            else
            {
                Console.WriteLine();
                txbMessage.Text = refreshTokenResult.ErrorDescription;
            }
        }

        public static string GetRequestPostData(HttpListenerRequest request)
        {
            if (request.HasEntityBody)
            {
                using var body = request.InputStream;
                using var reader = new System.IO.StreamReader(body, request.ContentEncoding);
                return reader.ReadToEnd();
            }

            if(string.IsNullOrEmpty(request.RawUrl) || string.Equals(request.RawUrl, "/"))
            {
                return null;
            }

            return request.RawUrl.TrimStart('/').TrimStart('?');
        }
    }
}
