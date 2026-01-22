using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace GraphQLClient.Views
{
    /// <summary>
    /// Interaction logic for BaseView.xaml
    /// </summary>
    public partial class BaseView : UserControl, INotifyPropertyChanged, IFresh
    {
        private bool _isLoaded = false;
        protected AppView _appView;

        public virtual ViewTypes ViewType => ViewTypes.None;
        public virtual Task LoadData() => Task.CompletedTask;
        public virtual Task FreshView() => Task.CompletedTask;
        public virtual string ViewTitle => string.Empty;
        public virtual string BackButtonTitle => string.Empty;
        public virtual string NextButtonTitle => "Next";
        public virtual Action NextButtonAcion => () => { };


        public BaseView? ParentView { get; private set; } = null;

        public BaseView(AppView appView, BaseView? parentView = null)
        {
            Loaded += BaseView_Loaded;
            _appView = appView;
            ParentView = parentView;
        }

        private async void BaseView_Loaded(object sender, RoutedEventArgs e)
        {
            // Lazy load - only load once when the view is first shown
            if (!_isLoaded)
            {
                _isLoaded = true;
                await LoadData();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
