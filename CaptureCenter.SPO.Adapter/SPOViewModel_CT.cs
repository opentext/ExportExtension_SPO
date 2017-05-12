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
    public class SPOViewModel_CT :ModelBase
    {
        #region Construction
        private SPOViewModel vm;
        private SPOSettings settings;

        public SPOViewModel_CT(SPOViewModel vm)
        {
            this.vm = vm;
            settings = vm.SPOSettings;
        }

        public void Initialize(UserControl control)
        {
            findPasswordBox(control);
        }

        public bool ActivateTab() { return true; }
        #endregion

        #region Properties ConnectionTab
        string SiteUrl_name = "SiteUrl";
        public string SiteUrl
        {
            get { return settings.SiteUrl; }
            set { settings.SiteUrl = value; SendPropertyChanged(); }
        }

        string Username_name = "Username";
        public string Username
        {
            get { return settings.Username; }
            set { settings.Username = value; SendPropertyChanged(); }
        }

        string Office365_name = "Office365";
        public bool Office365
        {
            get { return settings.Office365; }
            set { settings.Office365 = value; SendPropertyChanged(); }
        }
        #endregion

        #region Password
        string Password_name = "Password";
        public string Password
        {
            get {
                if (settings.Password == null) return string.Empty;
                return PasswordEncryption.Decrypt(settings.Password);
            }
            set {
                settings.Password = PasswordEncryption.Encrypt(value);
                SendPropertyChanged("Password");
            }
        }

        private PasswordBox passwordBox;
        private void findPasswordBox(UserControl control)
        {
            passwordBox = (PasswordBox)LogicalTreeHelper.FindLogicalNode(control, "passwordBox");
        }
        public void PasswordChangedHandler()
        {
            Password = SIEEUtils.GetUsecuredString(passwordBox.SecurePassword);
        }
        #endregion

        #region Functions Connection
        public ISPOClient GetSPOClient()
        {
            return vm.SPOClient;
        }

        public bool IsConnectionRelevant(string property)
        {
            return
                property == SiteUrl_name ||
                property == Username_name ||
                property == Password_name ||
                property == Office365_name;
        }

        public void Login()
        {
            settings.InitializeSPOClient(vm.SPOClient);
            vm.SPOClient.Login();
        }

        public void ShowVersion()
        {
            SIEEMessageBox.Show("SharePoint online connector Version 0.7", "Version", MessageBoxImage.Information );
        }

        private ConnectionTestResultDialog connectionTestResultDialog;
        private ConnectionTestHandler ConnectionTestHandler;

        // Set up objects, start tests (running in the backgroud) and launch the dialog
        public void TestConnection()
        {
            settings.InitializeSPOClient(vm.SPOClient);

            VmTestResultDialog vmConnectionTestResultDialog = new VmTestResultDialog();
            ConnectionTestHandler = new SPOConnectionTestHandler(vmConnectionTestResultDialog);
            ConnectionTestHandler.CallingViewModel = this;

            connectionTestResultDialog = new ConnectionTestResultDialog(ConnectionTestHandler);
            connectionTestResultDialog.DataContext = vmConnectionTestResultDialog;
            connectionTestResultDialog.ShowInTaskbar = false;

            //The test environment is Winforms, we then set the window to topmost.
            //In OCC we we can set the owner property
            if (Application.Current == null)
                connectionTestResultDialog.Topmost = true;
            else
                connectionTestResultDialog.Owner = Application.Current.MainWindow;

            ConnectionTestHandler.LaunchTests();
            connectionTestResultDialog.ShowDialog();
        }

        #endregion

    }
}
