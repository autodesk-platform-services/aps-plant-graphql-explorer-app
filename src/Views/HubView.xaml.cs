using GraphQLClient.Commands;
using GraphQLClient.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;


namespace GraphQLClient.Views
{
    /// <summary>
    /// Interaction logic for HubView.xaml
    /// </summary>
    public partial class HubView : BaseView, INotifyPropertyChanged
    {
        private Dictionary<string, BaseView> _projectViewCache = new Dictionary<string, BaseView>();

        public HubView(AppView appView)
            :base(appView)
        {
            InitializeComponent();
            DataContext = this;
        }

        public override ViewTypes ViewType => ViewTypes.Hubs;
        public override Task LoadData() => LoadHubsAsync();
        public override Task FreshView() => LoadHubsAsync();
        public override string ViewTitle => "ACC account:";
        public override Action NextButtonAcion => () =>
        {
            var selectedHub = hubs.SelectedItem as Hub;
            if (selectedHub != null)
            {
                NextView(selectedHub.Id);
            }
        };

        private ObservableCollection<Hub> _hubs = new ObservableCollection<Hub>();
        public ObservableCollection<Hub> Hubs
        {
            get => _hubs;
            set
            {
                _hubs = value;
                OnPropertyChanged();
            }
        }

        private async Task LoadHubsAsync()
        {
            try
            {
                var response = await GQLRequest.Instance.QueryAsync<GraphQLResponse<HubsData>>(QueryCommands.Query_Hubs);

                if (response?.Data?.Hubs?.Results != null)
                {
                    Hubs.Clear();
                    foreach (var hub in response.Data.Hubs.Results)
                    {
                        Hubs.Add(hub);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error - could show in UI
                MessageBox.Show($"Error loading hubs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Hubs_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is Hub selectedHub)
            {
                NextView(selectedHub.Id);
            }
        }

        private void NextView(string hubId)
        {
            if (_projectViewCache.ContainsKey(hubId))
            {
                _appView.SetView(_projectViewCache[hubId]);
                return;
            }

            var projView = new ProjectsView(_appView, this, hubId);
            _projectViewCache[hubId] = projView;
            _appView.SetView(projView);
        }

    }
}
