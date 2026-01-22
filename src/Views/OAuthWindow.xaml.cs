using System.Windows;

namespace GraphQLClient.Views
{
    /// <summary>
    /// Interaction logic for OAuthWindow.xaml
    /// </summary>
    public partial class OAuthWindow : Window
    {
        public OAuthWindow(Uri authorizeUri, Uri redirectUri)
        {
            InitializeComponent();

            Loaded += async (_, __) =>
            {
                await Browser.EnsureCoreWebView2Async();
                Browser.Source = authorizeUri;
            };

            Browser.NavigationStarting += (_, e) =>
            {
                if (e.Uri.StartsWith(redirectUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    Dispatcher.BeginInvoke(() => DialogResult = true);
                }
            };
        }
    }
}
