using GraphQLClient.Commands;
using GraphQLClient.Services;
using System.Windows;
using System.Windows.Controls;

namespace GraphQLClient.Views
{
    public enum ViewTypes
    {
        None,
        Hubs,
        Projects,
        Folder,
        Search
    }

    public partial class AppView : UserControl
    {
        private BaseView _currentFreshView;
        private Dictionary<ViewTypes, BaseView> _viewCache = new Dictionary<ViewTypes, BaseView>();

        public AppView(OAuthPasswordTokenService tokenService)
        {
            GQLRequest.TokenService = tokenService;
            InitializeComponent();

            GraphQLRegionCombobox.ItemsSource = new List<string> { "US", "EMEA", "APAC" };
        }

        public void SetView(BaseView view)
        {
            DataContext = view;
            if (view != null)
            {
                ViewGrid.Children.Clear();
                ViewGrid.Children.Add(view);
                _currentFreshView = view;
            }
        }

        private void freshViewButton_Click(object sender, RoutedEventArgs e)
        {
            _currentFreshView?.FreshView();
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            SetView(_currentFreshView.ParentView!);
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            _currentFreshView.NextButtonAcion();
        }

        private void GraphQLRegionCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GQLRequest.Region = (string)GraphQLRegionCombobox.SelectedItem ?? "US";
            _currentFreshView?.FreshView();
        }
    }
}


