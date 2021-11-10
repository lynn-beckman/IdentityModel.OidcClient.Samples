using Android.App;
using Android.Widget;
using Android.OS;
using System;
using IdentityModel.OidcClient;
using System.Net.Http;
using IdentityModel.Client;
using System.Security.Claims;

namespace AndroidClient
{
    public class State
    {
        public string IdToken { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public ClaimsPrincipal User { get; set; }
        public bool IsError => Error != null;
        public string Error { get; set; }
    }

    [Activity(Label = "AndroidClient", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private TextView _output;
        private static State _state;
        private OidcClientOptions _options;
        private string _authority = "https://demo.duendesoftware.com";

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
                Browser = new ChromeCustomTabsBrowser(this)
            };
        }

        private async void _loginButton_Click(object sender, System.EventArgs e)
        {
            try
            {
                var oidcClient = new OidcClient(_options);
                var result = await oidcClient.LoginAsync();
                _state = new State 
                { 
                    IdToken = result.IdentityToken,
                    AccessToken = result.AccessToken,
                    RefreshToken = result.RefreshToken,
                    User = result.User,
                    Error = result.Error,
                };

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
            if (_state != null)
            {
                if (_state.IsError)
                {
                    Log("Error:" + _state.Error, true);
                }
                else
                {
                    Log("Claims:", true);
                    foreach (var claim in _state.User.Claims)
                    {
                        Log($"   {claim.Type}:{claim.Value}");
                    }
                    Log("Access Token: " + _state.AccessToken);
                    Log("Refresh Token: " + _state.RefreshToken);
                }
            }
        }

        private async void _apiButton_Click(object sender, EventArgs e)
        {
            if (_state?.IsError == false)
            {
                var apiUrl = "https://demo.duendesoftware.com/api/test";

                var client = new HttpClient();
                client.SetBearerToken(_state.AccessToken);

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
            if (_state?.RefreshToken != null)
            {
                var client = new HttpClient();
                var result = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
                {
                    Address = _authority + "/connect/token",
                    ClientId = _options.ClientId,
                    RefreshToken = _state.RefreshToken
                });

                Log("Refresh Token Result", clear: true);
                if (result.IsError)
                {
                    Log("Error: " + result.Error);
                    return;
                }

                _state.RefreshToken = result.RefreshToken;
                _state.AccessToken = result.AccessToken;

                Log("Access Token: " + _state.AccessToken);
                Log("Refresh Token: " + _state.RefreshToken);
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

