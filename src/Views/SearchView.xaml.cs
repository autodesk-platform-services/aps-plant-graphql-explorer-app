using GraphQLClient.Commands;
using GraphQLClient.Data;
using Microsoft.Win32;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace GraphQLClient.Views
{
    /// <summary>
    /// Interaction logic for SearchView.xaml
    /// </summary>
    public partial class SearchView : BaseView
    {
        private readonly string _projectId;
        private readonly string _folder2dUrn;
        private readonly string _folder3dUrn;
        private string _currentPartTypeUrn;
        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public DataTable ElementTable { get; } = new DataTable();
        public DataView ElementTableView => ElementTable.DefaultView;

        public override ViewTypes ViewType => ViewTypes.Search;
        public override Task LoadData() => LoadSearchResultsAsync();
        public override Task FreshView() => LoadSearchResultsAsync();

        public SearchView(AppView appView, BaseView parentView, string projectId, string folder2dUrn, string folder3dUrn)
            : base(appView, parentView)
        {
            InitializeComponent();
            DataContext = this;
            _projectId = projectId;
            _folder2dUrn = folder2dUrn;
            _folder3dUrn = folder3dUrn;
        }

        private void partTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selectedPartType = (e.AddedItems[0] as ComboBoxItem)?.Content?.ToString();
                _currentPartTypeUrn = string.Compare(selectedPartType, "Piping", StringComparison.OrdinalIgnoreCase) == 0 ? _folder3dUrn : _folder2dUrn;
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (ElementTable.Rows.Count == 0 || ElementTable.Columns.Count == 0)
            {
                MessageBox.Show("No results to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Export Search Results",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = $"search-results-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                ExportToCsv(dialog.FileName);
                MessageBox.Show("Search results exported successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export results: {ex.Message}", "Export", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadSearchResultsAsync();
        }

        private async Task<string> GetGroupElement(string folderUrn)
        {
            try
            {
                string elementGroupId = string.Empty;
                var response = await GQLRequest.Instance.QueryAsync<GraphQLResponse<ElementGroupData>>(QueryCommands.Query_ElementGroup,
                    new { projectId = _projectId, folderId = folderUrn, filter = new { searchType = "ALL" } });

                if (response?.Data?.ElementGroups?.Results != null)
                {
                    foreach (var elementGroup in response.Data.ElementGroups.Results)
                    {
                        elementGroupId = elementGroup.Id;
                    }
                }
                return elementGroupId;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task LoadSearchResultsAsync()
        {
            if (string.IsNullOrEmpty(_currentPartTypeUrn))
            {
                return;
            }

            IsLoading = true;
            try
            {
                var elementGroupId = await GetGroupElement(_currentPartTypeUrn);
                var textRange = new TextRange(searchTextBox.Document.ContentStart, searchTextBox.Document.ContentEnd);
                await SearchDataAsync(elementGroupId, textRange.Text.Trim().Replace("\r\n", " "));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchDataAsync(string elementGroupId, string searchTerm)
        {
            try
            {
                ElementTable.Clear();
                ElementTable.Columns.Clear();

                var response = await GQLRequest.Instance.QueryAsync<GraphQLResponse<SearchData>>(QueryCommands.Query_SearchElements,
                    new { groupId = elementGroupId, filter = new { query = searchTerm } });
                while (true)
                {
                    var cursor = response?.Data?.ElementWraps?.Pagination?.Cursor;
                    var elementWraps = response?.Data?.ElementWraps?.Results;
                    if (elementWraps != null && elementWraps.Count > 0)
                    {
                        foreach (var elementWrap in elementWraps)
                        {
                            var row = ElementTable.NewRow();

                            foreach (var column in elementWrap.RowData.Columns)
                            {
                                var columnName = column.Definition?.Name;
                                if (string.IsNullOrWhiteSpace(columnName))
                                {
                                    // skip if the column name is null
                                    continue;
                                }

                                if (!ElementTable.Columns.Contains(columnName))
                                {
                                    ElementTable.Columns.Add(columnName, typeof(string));
                                }

                                row[columnName] = column.Value;
                            }

                            ElementTable.Rows.Add(row);
                        }
                    }

                    if (cursor == null)
                    {
                        break;
                    }

                    response = await GQLRequest.Instance.QueryAsync<GraphQLResponse<SearchData>>(QueryCommands.Query_SearchElements,
                        new { groupId = elementGroupId, filter = new { query = searchTerm }, pagination = new { cursor = cursor } });

                }

                resultsDataGrid.ItemsSource = ElementTable.DefaultView;
                resultsDataGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
                // Handle error - could show in UI
                MessageBox.Show($"Error loading search results: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: false);

            WriteCsvHeaders(writer);
            WriteCsvRows(writer);
        }

        private void WriteCsvHeaders(StreamWriter writer)
        {
            var headerBuilder = new StringBuilder();
            for (int i = 0; i < ElementTable.Columns.Count; i++)
            {
                if (i > 0)
                {
                    headerBuilder.Append(',');
                }

                headerBuilder.Append(EscapeForCsv(ElementTable.Columns[i].ColumnName));
            }

            writer.WriteLine(headerBuilder.ToString());
        }

        private void WriteCsvRows(StreamWriter writer)
        {
            foreach (DataRow row in ElementTable.Rows)
            {
                var rowBuilder = new StringBuilder();
                for (int i = 0; i < ElementTable.Columns.Count; i++)
                {
                    if (i > 0)
                    {
                        rowBuilder.Append(',');
                    }

                    var cellValue = row[i]?.ToString() ?? string.Empty;
                    rowBuilder.Append(EscapeForCsv(cellValue));
                }

                writer.WriteLine(rowBuilder.ToString());
            }
        }

        private static string EscapeForCsv(string value)
        {
            var needsEscaping = value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r');
            if (!needsEscaping)
            {
                return value;
            }

            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
    }
}
