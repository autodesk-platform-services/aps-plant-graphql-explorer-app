using GraphQLClient.Services;
using System.Windows;
using System.Windows.Controls;

namespace GraphQLClient.Views
{
    public class LoginEventArgs : RoutedEventArgs
    {
        public GraphQLEnvironment Environment { get; set; }
        public OAuthType OAuthType { get; set; }
        public LoginEventArgs(GraphQLEnvironment environment, OAuthType oAuthType)
        {
            Environment = environment;
            OAuthType = oAuthType;
        }
    }

    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            Loaded += delegate { LoginButton.IsEnabled = IsClientConfigurationSet(); };
        }

        private GraphQLEnvironment _currentEnv;
        private OAuthType _currentOAuthType;
        public ComboBox EnvironmentComboBoxControl => EnvironmentComboBox;
        public Button LoginButtonControl => LoginButton;
        public ProgressBar LoginProgressBarControl => LoginProgressBar;
        public TextBlock StatusTextBlockControl => StatusTextBlock;

        public delegate void LoginHandler(object sender, LoginEventArgs args);
        public event LoginHandler? LoginClicked;

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginClicked?.Invoke(sender, new LoginEventArgs(_currentEnv, _currentOAuthType));
        }

        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigBorder.Visibility = ConfigBorder.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            if (ConfigBorder.Visibility == Visibility.Visible)
            {
                SetClientConfiguration(_currentEnv);
            }
            else
            {
                SaveClientConfiguration(_currentEnv);
            }

            LoginButton.IsEnabled = IsClientConfigurationSet();
        }

        private void EnvironmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EnvironmentComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _currentEnv = Enum.TryParse<GraphQLEnvironment>(selectedItem.Tag.ToString(), out var env) ? env : GraphQLEnvironment.Staging;

                if (ConfigBorder != null)
                {
                    SetClientConfiguration(_currentEnv);
                }
            }
        }

        private void SetClientConfiguration(GraphQLEnvironment currentEnv)
        {
            if (_currentOAuthType == OAuthType.OAuth)
            {
                ClientIdTextBox.Text = currentEnv switch
                {
                    GraphQLEnvironment.Staging => ConfigurationStg.Default.ClientID,
                    GraphQLEnvironment.Production => Configuration.Default.ClientID,
                    _ => ""
                };
                CallbackUrlTextBox.Text = currentEnv switch
                {
                    GraphQLEnvironment.Staging => ConfigurationStg.Default.CallbackURL,
                    GraphQLEnvironment.Production => Configuration.Default.CallbackURL,
                    _ => ""
                };
                ClientSecretTextBox.Password = currentEnv switch
                {
                    GraphQLEnvironment.Staging => ConfigurationStg.Default.ClientSecret,
                    GraphQLEnvironment.Production => Configuration.Default.ClientSecret,
                    _ => ""
                };
            }
            else
            {
                ClientIdPKCETextBox.Text = currentEnv switch
                {
                    GraphQLEnvironment.Staging => ConfigurationStg.Default.ClientID_PKCE,
                    GraphQLEnvironment.Production => Configuration.Default.ClientID_PKCE,
                    _ => ""
                };
                CallbackPKCEUrlTextBox.Text = currentEnv switch
                {
                    GraphQLEnvironment.Staging => ConfigurationStg.Default.CallbackURL_PKCE,
                    GraphQLEnvironment.Production => Configuration.Default.CallbackURL_PKCE,
                    _ => ""
                };
            }

            LoginButton.IsEnabled = IsClientConfigurationSet();
        }

        private void SaveClientConfiguration(GraphQLEnvironment currentEnv)
        {
            if (currentEnv == GraphQLEnvironment.Staging)
            {
                if (_currentOAuthType == OAuthType.OAuth)
                {
                    ConfigurationStg.Default.ClientID = ClientIdTextBox.Text;
                    ConfigurationStg.Default.CallbackURL = CallbackUrlTextBox.Text;
                    ConfigurationStg.Default.ClientSecret = ClientSecretTextBox.Password;
                }
                else
                {
                    ConfigurationStg.Default.ClientID_PKCE = ClientIdPKCETextBox.Text;
                    ConfigurationStg.Default.CallbackURL_PKCE = CallbackPKCEUrlTextBox.Text;
                }
                ConfigurationStg.Default.Save();
            }
            else
            {
                if (_currentOAuthType == OAuthType.OAuth_PKCE)
                {
                    Configuration.Default.ClientID = ClientIdTextBox.Text;
                    Configuration.Default.CallbackURL = CallbackUrlTextBox.Text;
                    Configuration.Default.ClientSecret = ClientSecretTextBox.Password;
                }
                else
                {
                    Configuration.Default.ClientID_PKCE = ClientIdPKCETextBox.Text;
                    Configuration.Default.CallbackURL_PKCE = CallbackPKCEUrlTextBox.Text;
                }
                Configuration.Default.Save();
            }
        }

        private bool IsClientConfigurationSet()
        {
            if (_currentOAuthType == OAuthType.OAuth)
            {
                var clientId = _currentEnv == GraphQLEnvironment.Staging ? ConfigurationStg.Default.ClientID : Configuration.Default.ClientID;
                var clientSecret = _currentEnv == GraphQLEnvironment.Staging ? ConfigurationStg.Default.ClientSecret : Configuration.Default.ClientSecret;
                var callbackUrl = _currentEnv == GraphQLEnvironment.Staging ? ConfigurationStg.Default.CallbackURL : Configuration.Default.CallbackURL;
                return !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(callbackUrl);
            }
            else
            {
                var clientIdPKCE = _currentEnv == GraphQLEnvironment.Staging ? ConfigurationStg.Default.ClientID_PKCE : Configuration.Default.ClientID_PKCE;
                var callbackPKCEUrl = _currentEnv == GraphQLEnvironment.Staging ? ConfigurationStg.Default.CallbackURL_PKCE : Configuration.Default.CallbackURL_PKCE;
                return !string.IsNullOrEmpty(clientIdPKCE) && !string.IsNullOrEmpty(callbackPKCEUrl);
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTab = AuthTabControl.SelectedItem as TabItem;
            if (selectedTab != null && selectedTab.Header.ToString() == "OAuth")
            {
                _currentOAuthType = OAuthType.OAuth;
            }
            else
            {
                _currentOAuthType = OAuthType.OAuth_PKCE;
            }
        }
    }
}


