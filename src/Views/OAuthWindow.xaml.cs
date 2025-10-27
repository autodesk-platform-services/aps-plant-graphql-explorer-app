using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
                    Dispatcher.BeginInvoke(Close);
                }
            };
        }
    }
}
