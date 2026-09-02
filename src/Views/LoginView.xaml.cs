using GraphQLClient.Commands;
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
            Loaded += delegate
            {
                var envs = new List<GraphQLEnvironment> { GraphQLEnvironment.Production };
                _currentEnv = GraphQLEnvironment.Production;
                EnvironmentComboBox.ItemsSource = envs;

                _currentOAuthType = OAuthType.OAuth_PKCE;

                SetClientConfiguration(_currentEnv);
                GQLRequest.Environment = _currentEnv;
            };
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
            SaveClientConfiguration(_currentEnv);
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
        }

        private void EnvironmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EnvironmentComboBox.SelectedItem != null)
            {
                _currentEnv = Enum.TryParse<GraphQLEnvironment>(EnvironmentComboBox.SelectedItem.ToString(), out var env) ? env : GraphQLEnvironment.Production;
                Request.Environment = _currentEnv;

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
                   GraphQLEnvironment.Production => Configuration.Default.ClientID,
                   _ => ""
                };
                CallbackUrlTextBox.Text = currentEnv switch
                {
                   GraphQLEnvironment.Production => Configuration.Default.CallbackURL,
                   _ => ""
                };
                ClientSecretTextBox.Password = currentEnv switch
                {
                   GraphQLEnvironment.Production => Configuration.Default.ClientSecret,
                   _ => ""
                };
            }
            else
            {
                ClientIdPKCETextBox.Text = Configuration.Default.ClientID_PKCE;
                CallbackPKCEUrlTextBox.Text = Configuration.Default.CallbackURL_PKCE;
            }
        }

        private void SaveClientConfiguration(GraphQLEnvironment currentEnv)
        {
            dynamic config = Configuration.Default;
            if (_currentOAuthType == OAuthType.OAuth)
            {
                config.ClientID = ClientIdTextBox.Text;
                config.ClientSecret = ClientSecretTextBox.Password;
                config.CallbackURL = CallbackUrlTextBox.Text;
            }
            else
            {
                config.ClientID_PKCE = ClientIdPKCETextBox.Text;
                config.CallbackURL_PKCE = CallbackPKCEUrlTextBox.Text;
            }
            config.Save();
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

            SetClientConfiguration(_currentEnv);
        }
    }
}


