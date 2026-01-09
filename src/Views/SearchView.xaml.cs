using GraphQLClient.Commands;
using GraphQLClient.Data;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace GraphQLClient.Views
{
    /// <summary>
    /// Interaction logic for SearchView.xaml
    /// </summary>
    public partial class SearchView : BaseView
    {
        private readonly string _projectId;
        private readonly string? _folder2dUrn;
        private readonly string? _folder3dUrn;
        private string? _currentPartTypeUrn;
        private bool _isLoading;
        private bool _suppressSuggestionRefresh;
        private readonly ObservableCollection<string> _searchHints = new(Schemas.SchemaList);
        private readonly ICollectionView _filteredSuggestions;
        private bool _isSuggestionsOpen;
        private string _activeToken = string.Empty;
        private TextPointer? _tokenStartPointer;
        private TextPointer? _tokenEndPointer;
        private SchemaObject _schemaObject;
        private ExamplesObject _examplesObject;

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
        public ICollectionView FilteredSuggestions => _filteredSuggestions;
        public List<string> SchemaGroups { get; set; }

        public override ViewTypes ViewType => ViewTypes.Search;
        //public override Task LoadData() => LoadSearchResultsAsync();
        //public override Task FreshView() => LoadSearchResultsAsync();

        public bool IsSuggestionsOpen
        {
            get => _isSuggestionsOpen;
            set
            {
                if (_isSuggestionsOpen != value)
                {
                    _isSuggestionsOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public SearchView(AppView appView, BaseView parentView, string projectId, string? folder2dUrn, string? folder3dUrn)
            : base(appView, parentView)
        {
            InitializeComponent();

            // schemas
            //
            _schemaObject = GetResource<SchemaObject>("pack://application:,,,/Schema.json");
            SchemaGroups = _schemaObject.Data.Select(c => c.GroupName).ToList();
            SchemaGroups.Sort();

            // examples
            //
            _examplesObject = GetResource<ExamplesObject>("pack://application:,,,/Examples.json");

            _filteredSuggestions = CollectionViewSource.GetDefaultView(_searchHints);
            _filteredSuggestions.Filter = FilterSuggestion;
            DataContext = this;
            _projectId = projectId;
            _folder2dUrn = folder2dUrn;
            _folder3dUrn = folder3dUrn;
            referenceCombobox.SelectedIndex = 0;
        }

        private T GetResource<T>(string uristr)
        {
            var uri = new Uri(uristr);
            var resourceStream = Application.GetResourceStream(uri);
            using var reader = new StreamReader(resourceStream.Stream);
            var jsonString = reader.ReadToEnd();
            return JsonSerializer.Deserialize<T>(jsonString);
        }

        private string GetSearchText()
        {
            if (searchTextBox == null)
            {
                return string.Empty;
            }

            var textRange = new TextRange(searchTextBox.Document.ContentStart, searchTextBox.Document.ContentEnd);
            var text = textRange.Text ?? string.Empty;
            text = text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
            return text.Trim();
        }

        private void SetSearchText(string text)
        {
            if (searchTextBox == null)
            {
                return;
            }

            _suppressSuggestionRefresh = true;
            try
            {
                searchTextBox.Document.Blocks.Clear();
                var paragraph = new Paragraph(new Run(text))
                {
                    Margin = new Thickness(0)
                };
                searchTextBox.Document.Blocks.Add(paragraph);
                var caret = searchTextBox.Document.ContentEnd;
                caret = caret.GetNextInsertionPosition(LogicalDirection.Backward) ?? caret;
                searchTextBox.CaretPosition = caret;
                searchTextBox.Selection.Select(searchTextBox.CaretPosition, searchTextBox.CaretPosition);
            }
            finally
            {
                _suppressSuggestionRefresh = false;
            }
        }

        private static bool IsTokenSeparator(char character)
        {
            return char.IsWhiteSpace(character) || character == '.' || character == ':' || character == '-' || character == '_' || character == ',' || character == ';' || character == '/' || character == '\\';
        }

        private void ResetTokenState()
        {
            _activeToken = string.Empty;
            _tokenStartPointer = null;
            _tokenEndPointer = null;
        }

        private string UpdateTokenState()
        {
            ResetTokenState();

            if (searchTextBox == null)
            {
                return _activeToken;
            }

            var caret = searchTextBox.CaretPosition;
            if (caret == null)
            {
                return _activeToken;
            }

            caret = caret.GetInsertionPosition(LogicalDirection.Backward) ?? caret;

            var backwardText = caret.GetTextInRun(LogicalDirection.Backward);
            var backwardCount = 0;
            if (!string.IsNullOrEmpty(backwardText))
            {
                for (var i = backwardText.Length - 1; i >= 0; i--)
                {
                    if (IsTokenSeparator(backwardText[i]))
                    {
                        break;
                    }
                    backwardCount++;
                }
            }

            _tokenStartPointer = caret.GetPositionAtOffset(-backwardCount, LogicalDirection.Backward) ?? caret;

            var forwardText = caret.GetTextInRun(LogicalDirection.Forward);
            var forwardCount = 0;
            if (!string.IsNullOrEmpty(forwardText))
            {
                for (var i = 0; i < forwardText.Length; i++)
                {
                    if (IsTokenSeparator(forwardText[i]))
                    {
                        break;
                    }
                    forwardCount++;
                }
            }

            _tokenEndPointer = caret.GetPositionAtOffset(forwardCount, LogicalDirection.Forward) ?? caret;

            if (_tokenStartPointer == null || _tokenEndPointer == null)
            {
                ResetTokenState();
                return _activeToken;
            }

            _activeToken = new TextRange(_tokenStartPointer, caret).Text ?? string.Empty;
            _activeToken = _activeToken.Trim();

            return _activeToken;
        }

        private void partTypeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selectedPartType = (e.AddedItems[0] as ComboBoxItem)?.Content?.ToString();
                _currentPartTypeUrn = string.Compare(selectedPartType, "3D Model", StringComparison.OrdinalIgnoreCase) == 0 ? _folder3dUrn : _folder2dUrn;
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
            IsSuggestionsOpen = false;
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
                var searchTerm = GetSearchText();
                await SearchDataAsync(elementGroupId, searchTerm);
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
                string previousCursor = string.Empty;
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

                    if (string.IsNullOrEmpty(cursor) || string.Compare(cursor, previousCursor, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        break;
                    }

                    previousCursor = cursor;

                    response = await GQLRequest.Instance.QueryAsync<GraphQLResponse<SearchData>>(QueryCommands.Query_SearchElements,
                        new { groupId = elementGroupId, filter = new { query = searchTerm }, pagination = new { cursor = cursor } });

                }

                resultsDataGrid.ItemsSource = null;
                resultsDataGrid.AutoGenerateColumns = true;
                resultsDataGrid.Columns.Clear();
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

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressSuggestionRefresh)
            {
                return;
            }

            RefreshSuggestionView();
        }

        private void searchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (suggestionsListBox == null)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Down:
                    RefreshSuggestionView();

                    if (IsSuggestionsOpen && suggestionsListBox.Items.Count > 0)
                    {
                        if (suggestionsListBox.SelectedIndex < 0)
                        {
                            suggestionsListBox.SelectedIndex = 0;
                        }

                        suggestionsListBox.Focus();
                        e.Handled = true;
                    }
                    break;

                case Key.Enter:
                    if (IsSuggestionsOpen && suggestionsListBox.SelectedItem is string selectedSuggestion)
                    {
                        ApplySuggestion(selectedSuggestion);
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    if (IsSuggestionsOpen)
                    {
                        IsSuggestionsOpen = false;
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void searchTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus is ListBoxItem or ListBox)
            {
                return;
            }

            IsSuggestionsOpen = false;
        }

        private void suggestionsListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (suggestionsListBox.SelectedItem is string selected)
            {
                ApplySuggestion(selected);
                e.Handled = true;
            }
        }

        private void suggestionsListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (suggestionsListBox.SelectedItem is string selected)
                    {
                        ApplySuggestion(selected);
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    IsSuggestionsOpen = false;
                    searchTextBox?.Focus();
                    e.Handled = true;
                    break;

                case Key.Up:
                    if (suggestionsListBox.SelectedIndex <= 0)
                    {
                        searchTextBox?.Focus();
                        e.Handled = true;
                    }
                    break;
            }
        }

        private bool FilterSuggestion(object suggestion)
        {
            if (suggestion is not string text)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_activeToken))
            {
                return false;
            }

            return text.IndexOf(_activeToken, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RefreshSuggestionView()
        {
            if (suggestionsListBox == null)
            {
                return;
            }

            var token = UpdateTokenState();
            if (string.IsNullOrWhiteSpace(token))
            {
                IsSuggestionsOpen = false;
                suggestionsListBox.SelectedIndex = -1;
                _filteredSuggestions.Refresh();
                return;
            }

            _filteredSuggestions.Refresh();
            var hasSuggestions = !_filteredSuggestions.IsEmpty;
            if (!hasSuggestions)
            {
                suggestionsListBox.SelectedIndex = -1;
            }

            IsSuggestionsOpen = hasSuggestions;
        }

        private void ApplySuggestion(string suggestion)
        {
            if (string.IsNullOrWhiteSpace(suggestion) || searchTextBox == null)
            {
                return;
            }

            if (_tokenStartPointer == null || _tokenEndPointer == null)
            {
                UpdateTokenState();
            }

            _suppressSuggestionRefresh = true;
            try
            {
                if (_tokenStartPointer != null && _tokenEndPointer != null)
                {
                    var range = new TextRange(_tokenStartPointer, _tokenEndPointer);
                    range.Text = suggestion;
                    searchTextBox.CaretPosition = _tokenStartPointer.GetPositionAtOffset(suggestion.Length, LogicalDirection.Forward) ?? searchTextBox.Document.ContentEnd;
                    searchTextBox.Selection.Select(searchTextBox.CaretPosition, searchTextBox.CaretPosition);
                }
                else
                {
                    SetSearchText(suggestion);
                }
            }
            finally
            {
                _suppressSuggestionRefresh = false;
            }

            IsSuggestionsOpen = false;
            ResetTokenState();
            searchTextBox.Focus();
        }

        private void ChooseProject_Click(object sender, RoutedEventArgs e)
        {
            if (ParentView != null)
            {
                _appView.SetView(ParentView);
            }
        }

        private void referenceCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (schemaList == null || referenceCombobox == null)
            {
                return;
            }

            schemaList.ItemsSource = null;

            var selectedValue = referenceCombobox.SelectedItem switch
            {
                ComboBoxItem cbi => cbi.Content?.ToString() ?? string.Empty,
                string str => str,
                _ => string.Empty
            };
            if (string.Compare(selectedValue, "All", StringComparison.OrdinalIgnoreCase) == 0)
            {
                var data = _schemaObject.Data.SelectMany(c => c.DataWithTag(_schemaObject.Prefix, _schemaObject.Version));
                if (data != null)
                {
                    data = data.OrderBy(c => c.Key, StringComparer.OrdinalIgnoreCase);
                    schemaList.ItemsSource = data;
                }
            }
            else if (string.Compare(selectedValue, "Examples", StringComparison.OrdinalIgnoreCase) == 0)
            {
                var data = _examplesObject.Examples.ToDictionary(k => k.Title, v => v.Script).ToList();
                if (data != null && data.Count > 0)
                {
                    schemaList.ItemsSource = data;
                }
            }
            else
            {
                var singleData = _schemaObject.Data.FirstOrDefault(c => string.Compare(c.GroupName, selectedValue, StringComparison.OrdinalIgnoreCase) == 0);
                if (singleData != null && singleData.SchemaList.Count > 0)
                {
                    var data = singleData.DataWithTag(_schemaObject.Prefix, _schemaObject.Version);
                    schemaList.ItemsSource = data;
                }
            }
        }

        private void SchemaList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((ListBoxItem)sender).Tag.ToString();
            var lastBlock = searchTextBox.Document.Blocks.LastBlock as Paragraph;
            if (lastBlock == null)
            {
                lastBlock = new Paragraph();
                searchTextBox.Document.Blocks.Add(lastBlock);
            }

            lastBlock.Inlines.Add(new Run(item));
        }
    }
}
