using System.Windows;
using System.Windows.Controls;

namespace GraphQLClient.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        public ComboBox EnvironmentComboBoxControl => EnvironmentComboBox;
        public Button LoginButtonControl => LoginButton;
        public ProgressBar LoginProgressBarControl => LoginProgressBar;
        public TextBlock StatusTextBlockControl => StatusTextBlock;

        public event RoutedEventHandler? LoginClicked;

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginClicked?.Invoke(sender, e);
        }
    }
}


