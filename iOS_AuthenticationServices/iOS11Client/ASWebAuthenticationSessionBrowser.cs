using Foundation;
using System.Threading.Tasks;
using IdentityModel.OidcClient.Browser;
using AuthenticationServices;
using System;
using UIKit;
using System.Threading;

namespace iOS11Client
{
    public class ASWebAuthenticationSessionBrowser : IBrowser
    {
        ASWebAuthenticationSession _af;
        
        public Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            var wait = new TaskCompletionSource<BrowserResult>();

            _af = new ASWebAuthenticationSession(
                new NSUrl(options.StartUrl),
                options.EndUrl,
                (callbackUrl, error) =>
                {
                    if (error != null)
                    {
                        var errorResult = new BrowserResult
                        {
                            ResultType = BrowserResultType.UserCancel,
                            Error = error.ToString()
                        };

                        wait.SetResult(errorResult);
                    }
                    else
                    {
                        var result = new BrowserResult
                        {
                            ResultType = BrowserResultType.Success,
                            Response = callbackUrl.AbsoluteString
                        };

                        wait.SetResult(result);
                    }
                });

            // iOS 13 requires the PresentationContextProvider set
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                _af.PresentationContextProvider = new PresentationContextProviderToSharedKeyWindow();
            }
                
            _af.Start();
            return wait.Task;
        }

        class PresentationContextProviderToSharedKeyWindow : NSObject, IASWebAuthenticationPresentationContextProviding
        {
            public UIWindow GetPresentationAnchor(ASWebAuthenticationSession session)
            {
                return UIApplication.SharedApplication.KeyWindow;
            }
        }
    }
}