using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ExportExtensionCommon;
using DOKuStar.Diagnostics.Tracing;
using System.IO;

namespace CaptureCenter.SPO
{
    public class SPOViewModel : SIEEViewModel
    {
        #region Construction
        public SPOSettings SPOSettings;
        public ISPOClient SPOClient;

        public SPOViewModel_CT CT { get; set; }
        public SPOViewModel_LT LT { get; set; }
        public SPOViewModel_FT FT { get; set; }
        public SPOViewModel_DT DT { get; set; }

        public SPOViewModel(SIEESettings settings, ISPOClient spoClient)
        {
            SPOSettings = settings as SPOSettings;
            SPOClient = spoClient;

            CT = new SPOViewModel_CT(this);
            LT = new SPOViewModel_LT(this);
            FT = new SPOViewModel_FT(this);
            DT = new SPOViewModel_DT(this);

            SelectedTab = 0;
            IsRunning = false;
            DataLoaded = false;

            if (SPOSettings.LoginPossible) LoginButtonHandler();

            CT.PropertyChanged += (s, e) =>
            {
                if (CT.IsConnectionRelevant(e.PropertyName))
                {
                    SPOSettings.LoginPossible = false;
                    DataLoaded = false;
                    TabNamesReset();
                }
            };
        }

        public override void Initialize(UserControl control)
        {
            CT.Initialize(control);
            LT.Initialize(control);
            DT.Initialize(control);
            initializeTabnames(control);
        }

        public override SIEESettings Settings
        {
            get { return SPOSettings; }
        }
        #endregion

        #region Properties (general)
        // The settings in this view model just control the visibility and accessibility of the various tabs
        private int selectedTab;
        public int SelectedTab
        {
            get { return selectedTab; }
            set { selectedTab = value; SendPropertyChanged(); }
        }
        private bool dataLoaded;
        public bool DataLoaded
        {
            get { return dataLoaded; }
            set { dataLoaded = value; SendPropertyChanged(); }
        }
        #endregion

        #region Event handler
        public void LoginButtonHandler()
        {
            IsRunning = true;
            SPOSettings.LoginPossible = true;
            try { CT.Login();}
            catch (Exception e)
            {
                DataLoaded = false;
                SPOSettings.LoginPossible = false;
                SIEEMessageBox.Show(e.Message, "Login error", MessageBoxImage.Error);
            }
            finally { IsRunning = false; }

            if (SPOSettings.LoginPossible) SelectedTab = 1;
        }

        public void LoadFieldsButtonHandler()
        {
            IsRunning = true;
            try { LT.LoadFields(); }
            catch (Exception e)
            { SIEEMessageBox.Show(e.Message, "Can't load fields. Reson:\n", MessageBoxImage.Error); }
            finally { IsRunning = false; }
        }

        public void FilterButtonHandler()
        {
            IsRunning = true;
            try { LT.LaunchFilterDialog(); }
            catch (Exception e)
            { SIEEMessageBox.Show(e.Message, "Unexpected error. Reson:\n", MessageBoxImage.Error); }
            finally { IsRunning = false; }
        }

        public void TestCommandHandler()
        {
            MessageBox.Show("World!");
        }
        #endregion

        #region Tab activation
        public Dictionary<string, bool> Tabnames;
        // Retrieve tabitem names from user control
        private void initializeTabnames(UserControl control)
        {
            Tabnames = new Dictionary<string, bool>();
            TabControl tc = (TabControl)LogicalTreeHelper.FindLogicalNode(control, "mainTabControl");
            foreach (TabItem tabItem in LogicalTreeHelper.GetChildren(tc)) Tabnames[tabItem.Name] = false;
        }

        public void ActivateTab(string tabName)
        {
            if (Tabnames[tabName]) return;
            IsRunning = true;
            try
            {
                switch (tabName)
                {
                    case "connectionTabItem":   { Tabnames[tabName] = CT.ActivateTab(); break; }
                    case "listTabItem":         { Tabnames[tabName] = LT.ActivateTab(); break; }
                    case "folderTabItem":       { Tabnames[tabName] = FT.ActivateTab(); break; }
                    case "documentTabItem":     { Tabnames[tabName] = DT.ActivateTab(Settings.CreateSchema()); break; }
                }
            }
            catch (Exception e)
            {
                SIEEMessageBox.Show(e.Message, "Error in " + tabName, MessageBoxImage.Error);
                DataLoaded = false;
                SelectedTab = 0;
                TabNamesReset();
            }
            finally { IsRunning = false; }
        }

        private void TabNamesReset()
        {
            foreach (string tn in Tabnames.Keys.ToList()) Tabnames[tn] = false;
        }
        #endregion

    }
}
