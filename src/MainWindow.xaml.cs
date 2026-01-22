using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoginView.LoginClicked += LoginView_LoginClicked;
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
                return;
            }

            ShowAppView();
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
            try
            {
                _tokenService = OAuthPasswordTokenService.CreateDefault(e.Environment, e.OAuthType);
                await AttemptLoginAsync();
            }
            catch (Exception ex)
            {
                ShowStatus($"{ex.Message}", Brushes.OrangeRed);
            }
        }
    }
}