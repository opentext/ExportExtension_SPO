using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ExportExtensionCommon;

namespace CaptureCenter.SPO
{
    public partial class SPOControlWPF : SIEEUserControl
    {
        public SPOControlWPF()
        {
            InitializeComponent();
            this.Loaded += loaded;
        }

        #region Commands
        private void loaded(object sender, RoutedEventArgs ee)
        {
            CommandBindings.Add(new CommandBinding(
                SPOExportConnectorCommands.TestConnection,
                (s, e) => { ((SPOViewModel)DataContext).CT.TestConnection(); }));
            CommandBindings.Add(new CommandBinding(
                SPOExportConnectorCommands.Version,
                (s, e) => { ((SPOViewModel)DataContext).CT.ShowVersion(); }));
            CommandBindings.Add(new CommandBinding(
                SPOExportConnectorCommands.Login,
                (s, e) => { ((SPOViewModel)DataContext).LoginButtonHandler(); }));
            CommandBindings.Add(new CommandBinding(
                SPOExportConnectorCommands.Filter,
                (s, e) => { ((SPOViewModel)DataContext).FilterButtonHandler(); }));

            //CommandBindings.Add(new CommandBinding(
            //    ProcessSuiteExportConnectorCommands.Select,
            //    (s, e) => { ((VmProcessSuiteSettings)this.DataContext).SelectEntityButtonHandler(); },
            //    (s, e) => { e.CanExecute = ((VmProcessSuiteSettings)DataContext).EntitiesVM.CanSelectEntityButtonHandler(); }));
        }
        #endregion

        #region Connection
        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ((SPOViewModel)DataContext).CT.PasswordChangedHandler();
        }

        private void Button_Test_Click(object sender, RoutedEventArgs e)
        {
            ((SPOViewModel)DataContext).CT.TestConnection();
        }

        private void Button_Login_Click(object sender, RoutedEventArgs e)
        {
            ((SPOViewModel)DataContext).LoginButtonHandler();
        }
        #endregion

        #region Lists
        private void Button_LoadFields_Click(object sender, RoutedEventArgs e)
        {
            ((SPOViewModel)DataContext).LoadFieldsButtonHandler();
        }

        private void Button_Filter_Click(object sender, RoutedEventArgs e)
        {
            ((SPOViewModel)DataContext).FilterButtonHandler();
        }
        #endregion

        #region Tab handling
        private Dictionary<string, bool> tabActivation = null;

        private void initializeTabActivation()
        {
            tabActivation = new Dictionary<string, bool>();
            foreach (string name in ((SPOViewModel)DataContext).Tabnames.Keys) tabActivation[name] = false;
        }

        void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((SPOViewModel)DataContext).Tabnames == null) return;
            if (tabActivation == null) initializeTabActivation();
            foreach (string tabName in tabActivation.Keys)
            {
                TabItem pt = (TabItem)LogicalTreeHelper.FindLogicalNode((DependencyObject)sender, tabName);
                if (pt.IsSelected)
                {
                    if (tabActivation[tabName]) return;
                    tabActivation[tabName] = true;
                    try { ((SPOViewModel)DataContext).ActivateTab(tabName); }
                    finally { tabActivation[tabName] = false; }
                    return;
                }
            }
        }

        private void Button_AddTokenToFile(object sender, RoutedEventArgs e)
        {
            SPOViewModel vm = ((SPOViewModel)DataContext);
            vm.DT.AddTokenToFileHandler((string)((Button)sender).Tag);
        }
        #endregion
    }
}
