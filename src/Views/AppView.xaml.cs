using GraphQLClient.Commands;
using GraphQLClient.Data;
using GraphQLClient.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks; 
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
    }
}


