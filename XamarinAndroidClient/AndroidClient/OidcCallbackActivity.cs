using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace AndroidClient
{
    [Activity(Label = "OidcCallbackActivity")]
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "io.identitymodel.native",
        DataHost = "callback")]
    public class OidcCallbackActivity : Activity
    {
        public static event Action<string> Callbacks;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Finish();

            Callbacks?.Invoke(Intent.DataString);
        }
    }
}