using GraphQLClient.Commands;
using GraphQLClient.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace GraphQLClient.Views
{
    /// <summary>
    /// Interaction logic for ProjectView.xaml
    /// </summary>
    public partial class ProjectsView : BaseView
    {
        private string _hubId;
        private Dictionary<string, BaseView> _folderViewCache = new Dictionary<string, BaseView>();

        public ProjectsView(AppView appView, BaseView parentView, string hubId)
            : base(appView, parentView)
        {
            InitializeComponent();
            DataContext = this;
            _hubId = hubId;
        }

        public override ViewTypes ViewType => ViewTypes.Projects;
        public override Task LoadData() => LoadProjectsAsync();
        public override string ViewTitle => "ACC projects:";
        public override string BackButtonTitle => "<< Choose ACC Account";
        public override Action NextButtonAcion => () =>
        {
            var selectedProject = projects.SelectedItem as Project;
            if (selectedProject != null)
            {
                NextView(selectedProject);
            }
        };

        public override Task FreshView()
        {
            _projects.Clear();
            return LoadProjectsAsync();
        }

        public ObservableCollection<Project> _projects { get; set; } = new ObservableCollection<Project>();
        public ObservableCollection<Project> Projects
        {
            get => _projects;
            set
            {
                _projects = value;
                OnPropertyChanged();
            }
        }

        private async Task LoadProjectsAsync()
        {
            try
            {
                var response = await GQLRequest.Instance.QueryAsync<GraphQLResponse<ProjectsData>>(QueryCommands.Query_Projects, new { hubId = _hubId });

                if (response?.Data?.Projects?.Results != null)
                {
                    Projects.Clear();
                    foreach (var project in response.Data.Projects.Results)
                    {
                        Projects.Add(project);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error - could show in UI
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void projects_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is Project selectedProject)
            {
                NextView(selectedProject);
            }
        }

        private void NextView(Project selectedProject)
        {
            if (_folderViewCache.ContainsKey(selectedProject.Id))
            {
                _appView.SetView(_folderViewCache[selectedProject.Id]);
                return;
            }

            var projView = new FolderView(_appView, this, selectedProject.Id, selectedProject.AlternativeIdentifiers.DataManagementAPIProjectId);
            _folderViewCache[selectedProject.Id] = projView;
            _appView.SetView(projView);
        }
    }
}
