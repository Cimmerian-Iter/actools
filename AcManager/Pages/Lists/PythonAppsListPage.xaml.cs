﻿using System;
using System.Windows;
using AcManager.Controls.ViewModels;
using AcManager.Tools.Filters;
using AcManager.Tools.Managers;
using AcManager.Tools.Objects;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Converters;
using StringBasedFilter;

namespace AcManager.Pages.Lists {
    public partial class PythonAppsListPage : IParametrizedUriContent {
        public void OnUri(Uri uri) {
            var filter = uri.GetQueryParam("Filter");
            DataContext = new PythonAppsListPageViewModel(string.IsNullOrEmpty(filter) ? null : Filter.Create(AcCommonObjectTester.Instance, filter)); // TODO: proper filter
            InitializeComponent();
        }

        private void PythonAppsListPage_OnUnloaded(object sender, RoutedEventArgs e) {
            ((PythonAppsListPageViewModel)DataContext).Unload();
        }

        private class PythonAppsListPageViewModel : AcListPageViewModel<PythonAppObject> {
            public PythonAppsListPageViewModel(IFilter<PythonAppObject> listFilter)
                : base(PythonAppsManager.Instance, listFilter) {
            }

            protected override string GetStatus() => PluralizingConverter.PluralizeExt(MainList.Count, "{0} app");
        }
    }
}