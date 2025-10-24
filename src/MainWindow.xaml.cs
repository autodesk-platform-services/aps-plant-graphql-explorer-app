using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GraphQLClient.Commands;
using GraphQLClient.Services;
using GraphQLClient.Views;

namespace GraphQLClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OAuthPasswordTokenService _tokenService;
        private CancellationTokenSource? _loginCancellation;
        private AppView? _appView;

        public MainWindow()
        {
            InitializeComponent();
        }

        private LoginView LoginView => LoginViewControl;
        //private AppView AppView => AppViewControl;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoginView.LoginClicked += LoginView_LoginClicked;
            //AppView.SearchClicked += AppView_SearchClicked;
            //AppView.CopyTokenClicked += CopyTokenButton_Click;
        }

        private async Task AttemptLoginAsync()
        {
            _loginCancellation?.Cancel();
            _loginCancellation?.Dispose();
            _loginCancellation = new CancellationTokenSource();

            try
            {
                SetBusyState(true);
                ShowStatus("Signing in…", Brushes.DodgerBlue);

                var result = await _tokenService.RequestTokenAsync(_loginCancellation.Token);
                HandleTokenResult(result);
            }
            catch (OperationCanceledException)
            {
                ShowStatus("Sign-in cancelled.", Brushes.DimGray);
            }
            catch (Exception ex)
            {
                ShowStatus($"Unexpected error: {ex.Message}", Brushes.OrangeRed);
            }
            finally
            {
                SetBusyState(false);
                _loginCancellation?.Dispose();
                _loginCancellation = null;
            }
        }

        private void SetBusyState(bool isBusy)
        {
            LoginView.LoginButtonControl.IsEnabled = !isBusy;
            LoginView.EnvironmentComboBoxControl.IsEnabled = !isBusy;
            LoginView.LoginProgressBarControl.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
            //AppView.CopyTokenButtonControl.IsEnabled = !isBusy && AppView.TokenPanelControl.Visibility == Visibility.Visible && !string.IsNullOrWhiteSpace(AppView.AccessTokenTextBoxControl.Text);
        }

        private void ShowStatus(string message, Brush brush)
        {
            LoginView.StatusTextBlockControl.Text = message;
            LoginView.StatusTextBlockControl.Foreground = brush;
        }

        protected override void OnClosed(EventArgs e)
        {
            _loginCancellation?.Cancel();
            _loginCancellation?.Dispose();

            LoginView.LoginClicked -= LoginView_LoginClicked;
            //AppView.SearchClicked -= AppView_SearchClicked;
            //AppView.CopyTokenClicked -= CopyTokenButton_Click;

            _tokenService?.Dispose();

            base.OnClosed(e);
        }

        private void HandleTokenResult(OAuthTokenResult result)
        {
            if (!result.IsSuccess)
            {
                var message = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? $"Token request failed ({result.StatusCode})."
                    : result.ErrorMessage;
                ShowStatus(message, Brushes.OrangeRed);
                //AppView.TokenPanelControl.Visibility = Visibility.Collapsed;
                //AppView.AccessTokenTextBoxControl.Text = string.Empty;
                //AppView.TokenMetadataTextBlockControl.Text = string.Empty;
                //AppView.CopyTokenButtonControl.IsEnabled = false;
                return;
            }

            ShowAppView();
            //AppView.AccessTokenTextBoxControl.Text = result.AccessToken;
            //AppView.TokenPanelControl.Visibility = Visibility.Visible;

            // var expiresInfo = result.ExpiresIn.HasValue
            //     ? $"expires in {TimeSpan.FromSeconds(result.ExpiresIn.Value).Minutes:D2}:{TimeSpan.FromSeconds(result.ExpiresIn.Value).Seconds:D2}"
            //     : "no expiry provided";

            // //AppView.TokenMetadataTextBlockControl.Text = $"Type: {result.TokenType}, {expiresInfo}";
            // //ShowStatus("Access token issued.", Brushes.SeaGreen);
            // //AppView.CopyTokenButtonControl.IsEnabled = true;

            // LoginView.Visibility = Visibility.Collapsed;
            // AppView.Visibility = Visibility.Visible;
            //// AppView.AppStatusTextBlockControl.Text = $"Authenticated as {result.TokenType} token";
        }

        private void ShowAppView()
        {
            if (_appView == null)
            {
                _appView = new AppView(_tokenService);
                // Show the Hubs view by default
                _appView.SetView(new HubView(_appView));
            }

            Content = _appView;
        }

        private async void LoginView_LoginClicked(object? sender, LoginEventArgs e)
        {
            _tokenService = OAuthPasswordTokenService.CreateDefault(e.Environment, e.OAuthType);
            await AttemptLoginAsync();
        }

        private void AppView_SearchClicked(object? sender, RoutedEventArgs e)
        {
            //AppView.SearchResultsTextBlockControl.Text = "Search executed (stub).";
        }

        private GraphQLEnvironment GetSelectedEnvironment()
        {
            if (LoginView.EnvironmentComboBoxControl.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                return Enum.TryParse<GraphQLEnvironment>(tag, out var env) ? env : GraphQLEnvironment.Staging;
            }

            return GraphQLEnvironment.Staging;
        }

        private void CopyTokenButton_Click(object? sender, RoutedEventArgs e)
        {
            //if (!string.IsNullOrWhiteSpace(AppView.AccessTokenTextBoxControl.Text))
            //{
            //    Clipboard.SetText(AppView.AccessTokenTextBoxControl.Text);
            //    ShowStatus("Access token copied to clipboard.", Brushes.DodgerBlue);
            //}
        }
    }
}