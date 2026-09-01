using GraphQLClient.Commands;
using GraphQLClient.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GraphQLClient.Views
{
    /// <summary>
    /// Interaction logic for FolderView.xaml
    /// </summary>
    public partial class FolderView : BaseView, INotifyPropertyChanged
    {
        private string _projectId;
        private string _dmProjectId;
        private string _folderUrn;
        private bool _isLoading;

        public FolderView(AppView appView, BaseView parentView, string projectId, string dmProjectId)
            : base(appView, parentView)
        {
            InitializeComponent();
            _projectId = projectId;
            _dmProjectId = dmProjectId;
            DataContext = this;
        }

        public override ViewTypes ViewType => ViewTypes.Folder;
        public override Task LoadData() => LoadFoldersAsync();
        public override string ViewTitle => "Plant 3D projectss:";
        public override string BackButtonTitle => "<< Choose ACC project";
        public override Action NextButtonAcion => async () =>
        {
            var selectedFolder = folders.SelectedItem as Folder;
            if (selectedFolder != null)
            {
                _folderUrn = selectedFolder.Id;
                await LoadFoldersAsync(selectedFolder);
            }
        };

        public override Task FreshView()
        {
            _folders.Clear();
            return LoadFoldersAsync();
        }

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

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        private async Task LoadFoldersAsync(Folder current = default)
        {
            try
            {
                IsLoading = true;
                if (current != null && current.IsPlantProject)
                {
                    if (!string.IsNullOrEmpty(current.PIDElementGroupId) && !string.IsNullOrEmpty(current.P3DElementGroupId))
                    {
                        // show earch view
                        //
                        _appView.SetView(new SearchView(_appView, this, _projectId, current.PIDElementGroupId, current.P3DElementGroupId));
                    }
                }
                else
                {
                    if (current == null)
                    {
                        var response = await GQLRequest.Instance.QueryAsync<GraphQLResponse<ProjectFolderData>>
                        (
                            QueryCommands.Query_FolderByProject, new { projectId = _projectId }
                        );

                        var folders = response.Data.Folders.Results as IEnumerable<Folder>;
                        if (folders != null)
                        {
                            await CheckPlantProjectFoldersAsync(folders);

                            var tasks = folders.Where(c => c.IsPlantProject).Select(c => SetDMItems(c));
                            await Task.WhenAll(tasks);

                            Folders.Clear();
                            foreach (var folder in folders)
                            {
                                Folders.Add(folder);
                            }
                        }
                    }
                    else
                    {
                        var response = await GQLRequest.Instance.QueryAsync<GraphQLResponse<ProjectFolderByFolderData>>
                        (
                            QueryCommands.Query_SpecialFolder, new { projectId = _projectId, folderId = current.Id }
                        );

                        var folders = response.Data.Folders.Results as IEnumerable<Folder>;
                        if (folders != null)
                        {
                            await CheckPlantProjectFoldersAsync(folders);
                            var tasks = folders.Where(c => c.IsPlantProject).Select(c => SetDMItems(c));
                            await Task.WhenAll(tasks);

                            current.Children = new List<Folder>(folders);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error - could show in UI
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SetDMItems(Folder c)
        {
            var (pidfoldUrn, p3dfolderUrn) = await DocsRequest.Instance.FindFilePlusFoldersAsync(c.Id, _dmProjectId);
            //var pidUrn = await PlantDMRequest.Instance.GetFilePlusDataAsync(pidfoldUrn, _dmProjectId);
            //var p3dUrn = await PlantDMRequest.Instance.GetFilePlusDataAsync(p3dfolderUrn, _dmProjectId);
            //var folderUrns = await Task.WhenAll(
            //    PlantDMRequest.Instance.GetFilePlusDataAsync(pidfoldUrn, _dmProjectId),
            //    PlantDMRequest.Instance.GetFilePlusDataAsync(p3dfolderUrn, _dmProjectId)
            //);

            //var (pidUrn, p3dUrn) = (folderUrns[0], folderUrns[1]);
            //if (string.IsNullOrEmpty(pidUrn) || string.IsNullOrEmpty(p3dUrn))
            //{
            //    return;
            //}

            var groups = await Task.WhenAll(GetGroupElement(pidfoldUrn), GetGroupElement(p3dfolderUrn));
            var (pidGroupId, p3dGroupId) = (groups[0], groups[1]);
            if (!string.IsNullOrEmpty(pidGroupId) && !string.IsNullOrEmpty(p3dGroupId))
            {
                c.PIDElementGroupId = pidGroupId;
                c.P3DElementGroupId = p3dGroupId;
            }
        }

        private async Task<string> GetGroupElement(string folderUrn)
        {
            try
            {
                var response = await GQLRequest.Instance.QueryAsync<GraphQLResponse<ElementGroupData>>(QueryCommands.Query_ElementGroup,
                    new { projectId = _projectId, folderId = folderUrn, filter = new { searchType = "ALL" } });

                if (response?.Data?.ElementGroups?.Results != null)
                {
                    foreach (var elementGroup in response.Data.ElementGroups.Results)
                    {
                        return elementGroup.Id;
                    }
                }
                return string.Empty;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async void folders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeView listView && listView.SelectedItem is Folder selectedFolder)
            {
                // next level
                //
                _folderUrn = selectedFolder.Id;
                await LoadFoldersAsync(selectedFolder);
            }
        }

        private async Task CheckPlantProjectFoldersAsync(IEnumerable<Folder>? folders)
        {
            if (folders == null)
            {
                return;
            }

            var requestFunc = async (Folder folder) =>
            {
                //var requestBody = new
                //{
                //    folderUrns = new string[] { folder.Id },
                //    searchText = "PipingPart.xml",
                //    recursive = false
                //};
                //var query = JsonSerializer.Serialize(requestBody);
                try
                {
                    folder.IsPlantProject = await DocsRequest.Instance.FindItemByNameAsync(folder.Id, _dmProjectId);
                }
                catch (Exception)
                {
                    // ignore errors
                }
            };

            foreach (var chunk in folders.Chunk(5))
            {
                var tasks = chunk.Select(c => requestFunc(c));
                await Task.WhenAll(tasks);
            }
        }

        private async Task<(string pid, string p3d)> GetPlantFilePulsFolderUrns(Folder folder)
        {
            var result = (string.Empty, string.Empty);
            
            if (folder == null)
            {
                return result;
            }

            // Create proper GraphQL request body
            var requestBody = new
            {
                folderUrns = new string[] { folder.Id },
                filters = new
                {
                    entityType = new
                    {
                        value = new string[] { "CONTAINER" }
                    },
                    mimeType = new
                    {
                        value = "application/vnd.autodesk.aecdm"
                    }
                },
                recursive = true
            };
            var query = JsonSerializer.Serialize(requestBody);

            var filePlus = await DMRequest.Instance.QueryAsync<FilePlus>(query, _dmProjectId);
            foreach (var doc in filePlus.Documents)
            {
                if (string.Compare(doc.Name, "P&ID Data Set", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    result.Item1 = doc.ParentFolderUrn;
                }
                else if (string.Compare(doc.Name, "3D Piping Data Set", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    result.Item2 = doc.ParentFolderUrn;
                }
            }

            return result;
        }

    }
}
