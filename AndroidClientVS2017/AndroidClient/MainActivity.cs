using Android.App;
using Android.Widget;
using Android.OS;
using System;
using IdentityModel.OidcClient;
using System.Net.Http;
using IdentityModel.Client;

namespace AndroidClient
{
    [Activity(Label = "AndroidClient", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private TextView _output;
        private static LoginResult _result;
        private OidcClientOptions _options;
        private string _authority = "https://demo.identityserver.io";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            var loginButton = FindViewById<Button>(Resource.Id.LoginButton);
            loginButton.Click += _loginButton_Click;

            var apiButton = FindViewById<Button>(Resource.Id.ApiButton);
            apiButton.Click += _apiButton_Click;

            var refreshButton = FindViewById<Button>(Resource.Id.RefreshButton);
            refreshButton.Click += _refreshButton_Click;

            _output = FindViewById<TextView>(Resource.Id.Output);

            ShowResults();

            _options = new OidcClientOptions
            {
                Authority = _authority,
                ClientId = "interactive.public",
                Scope = "openid profile api offline_access",
                RedirectUri = "io.identitymodel.native://callback",
                Flow = OidcClientOptions.AuthenticationFlow.AuthorizationCode,
                ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect,
                Browser = new ChromeCustomTabsBrowser(this)
            };
        }

        private async void _loginButton_Click(object sender, System.EventArgs e)
        {
            try
            {
                var oidcClient = new OidcClient(_options);
                _result = await oidcClient.LoginAsync();

                // used to redisplay this app if it's hidden by browser
                StartActivity(GetType());
            }
            catch (Exception ex)
            {
                Log("Exception: " + ex.Message, true);
                Log(ex.ToString());
            }
        }

        private void ShowResults()
        {
            if (_result != null)
            {
                if (_result.IsError)
                {
                    Log("Error:" + _result.Error, true);
                }
                else
                {
                    Log("Claims:", true);
                    foreach (var claim in _result.User.Claims)
                    {
                        Log($"   {claim.Type}:{claim.Value}");
                    }
                    Log("Access Token: " + _result.AccessToken);
                    Log("Refresh Token: " + _result.RefreshToken);
                }
            }
        }

        private async void _apiButton_Click(object sender, EventArgs e)
        {
            if (_result?.IsError == false)
            {
                var apiUrl = "https://demo.identityserver.io/api/test";

                var client = new HttpClient();
                client.SetBearerToken(_result.AccessToken);

                try
                {
                    var result = await client.GetAsync(apiUrl);
                    if (result.IsSuccessStatusCode)
                    {
                        Log("API Results:", true);

                        var json = await result.Content.ReadAsStringAsync();
                        Log(json);
                    }
                    else
                    {
                        Log("API Error: " + (int)result.StatusCode, true);
                    }
                }
                catch (Exception ex)
                {
                    Log("Exception: " + ex.Message, true);
                    Log(ex.ToString());
                }
            }
            else
            {
                Log("Login to call API");
            }
        }

        private async void _refreshButton_Click(object sender, EventArgs e)
        {
            if (_result?.RefreshToken != null)
            {
                var client = new TokenClient(_authority + "/connect/token", _options.ClientId);
                var result = await client.RequestRefreshTokenAsync(_result.RefreshToken);

                Log("Refresh Token Result", clear: true);
                if (result.IsError)
                {
                    Log("Error: " + result.Error);
                    return;
                }

                _result.RefreshToken = result.RefreshToken;
                _result.AccessToken = result.AccessToken;

                Log("Access Token: " + _result.AccessToken);
                Log("Refresh Token: " + _result.RefreshToken);
            }
            else
            {
                Log("No Refresh Token", true);
            }
        }

        public void Log(string msg, bool clear = false)
        {
            if (clear)
            {
                _output.Text = "";
            }
            else
            {
                _output.Text += "\r\n";
            }

            _output.Text += msg;
        }
    }
}

