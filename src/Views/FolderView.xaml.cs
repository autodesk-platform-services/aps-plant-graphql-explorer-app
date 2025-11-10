using GraphQLClient.Commands;
using GraphQLClient.Data;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
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
                    if (current.PIDDataset != null || current.PipingDataset != null)
                    {
                        // show earch view
                        //
                        _appView.SetView(new SearchView(_appView, this, _projectId, current.PIDDataset, current.PipingDataset));
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

                            var tasks = folders.Where(c => c.IsPlantProject).Select(async c =>
                            {
                                var (pidUrn, p3dUrn) = await GetPlantFilePulsFolderUrns(c);
                                c.PIDDataset = pidUrn;
                                c.PipingDataset = p3dUrn;
                            });
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
                var requestBody = new
                {
                    folderUrns = new string[] { folder.Id },
                    searchText = "PipingPart.xml",
                    recursive = false
                };
                var query = JsonSerializer.Serialize(requestBody);
                try
                {

                    var result = await DocsRequest.Instance.QueryAsync<FilePlus>(query, _dmProjectId);
                    if (result.Documents.Count > 0)
                    {
                        folder.IsPlantProject = true;
                    }
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

            var filePlus = await DocsRequest.Instance.QueryAsync<FilePlus>(query, _dmProjectId);
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
