using GraphQLClient.Commands;
using GraphQLClient.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
    /// Interaction logic for FolderView.xaml
    /// </summary>
    public partial class FolderView : BaseView, INotifyPropertyChanged
    {
        private Stack<KeyValuePair<string, List<Folder>>> _foldersCache = new Stack<KeyValuePair<string, List<Folder>>>();
        private string _projectId;
        private string _folderUrn;

        public FolderView(AppView appView, BaseView parentView, string projectId, string folderUrn = default)
            : base(appView, parentView)
        {
            InitializeComponent();
            _projectId = projectId;
            _folderUrn = folderUrn;
            DataContext = this;
        }

        public override ViewTypes ViewType => ViewTypes.Folder;
        public override Task LoadData() => LoadFoldersAsync();
        public override Task FreshView() => LoadFoldersAsync();

        public ObservableCollection<Folder> _folders { get; set; } = new ObservableCollection<Folder>();
        public ObservableCollection<Folder> Folders
        {
            get => _folders;
            set
            {
                _folders = value;
                OnPropertyChanged();
            }
        }

        public async Task<bool> BackToParentFolder()
        {
            bool hasCached = _foldersCache.Count > 0;
            if (hasCached)
            {
                var cached = _foldersCache.Pop();
                _folderUrn = cached.Key;
                Folders.Clear();
                foreach (var folder in cached.Value)
                {
                    Folders.Add(folder);
                }
            }
            return await Task.FromResult(hasCached);
        }

        private async Task LoadFoldersAsync()
        {
            try
            {
                dynamic response = _foldersCache.Count == 0 ? 
                    await GQLRequest.Instance.QueryAsync<GraphQLResponse<ProjectFolderData>>(QueryCommands.Query_FolderByProject, new { projectId = _projectId }) :
                    await GQLRequest.Instance.QueryAsync<GraphQLResponse<ProjectFolderByFolderData>>(QueryCommands.Query_SpecialFolder, new { projectId = _projectId, folderId = _folderUrn });


                if (response?.Data?.Folders?.Results != null)
                {
                    var p3d = (response.Data.Folders.Results as IEnumerable<Folder>).FirstOrDefault(c => string.Compare(c.Name, "Plant 3D Models", StringComparison.OrdinalIgnoreCase) == 0);
                    var pid =(response.Data.Folders.Results as IEnumerable<Folder>).FirstOrDefault(c => string.Compare(c.Name, "PID DWG", StringComparison.OrdinalIgnoreCase) == 0);

                    if (p3d != null || pid != null)
                    {
                        // show earch view
                        //
                        _appView.SetView(new SearchView(_appView, this, _projectId, pid?.Id, p3d?.Id));
                        _foldersCache.Pop();
                    }
                    else
                    {
                        Folders.Clear();
                        foreach (var folder in response.Data.Folders.Results)
                        {
                            Folders.Add(folder);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error - could show in UI
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void folders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is Folder selectedFolder)
            {
                // cache current folder list
                //
                _foldersCache.Push(new KeyValuePair<string, List<Folder>>(_folderUrn, Folders.ToList()));

                // next level
                //
                _folderUrn = selectedFolder.Id;
                await LoadFoldersAsync();
            }
        }
    }
}
