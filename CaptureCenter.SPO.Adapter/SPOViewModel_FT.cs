using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using ExportExtensionCommon;

namespace CaptureCenter.SPO
{
    public class SPOViewModel_FT : ModelBase
    {
        #region Construction
        private SPOViewModel vm;
        private SPOSettings settings;

        public SPOViewModel_FT(SPOViewModel vm)
        {
            this.vm = vm;
            settings = vm.SPOSettings;
        }

        public void Initialize(UserControl control) { }

        public bool ActivateTab() { return true; }
        #endregion

        #region Properties
        public SPOSettings.FolderHandlingType FolderHandling
        {
            get { return settings.FolderHandling; }
            set
            {
                settings.FolderHandling = value;
                FolderName = FolderName;    // ...clearing error if necessary
                FieldName = FieldName;      // ...clearing error if necessary
                RaisePropertyChanged(ShowCapacitySettings_name);
                RaisePropertyChanged(ShowDaySettings_name);
                SendPropertyChanged();
            }
        }

        public string FolderName
        {
            get { return settings.FolderName; }
            set
            {
                if (FolderHandling == SPOSettings.FolderHandlingType.Folder && String.IsNullOrEmpty(value))
                    throw new Exception("Foldername can not be empty.");
                settings.FolderName = value;
                SendPropertyChanged();
            }
        }

        public string FieldName
        {
            get { return settings.FieldName; }
            set
            {
                if (FolderHandling == SPOSettings.FolderHandlingType.Field && String.IsNullOrEmpty(value))
                    throw new Exception("Fieldname can not be empty.");
                settings.FieldName = value;
                SendPropertyChanged();
            }
        }

        public SPOSettings.AutoFolderType SelectedAutoFolderType
        {
            get { return settings.SelectedAutoFolderType; }
            set {
                settings.SelectedAutoFolderType = value;
                SendPropertyChanged();
                RaisePropertyChanged(ShowCapacitySettings_name);
                RaisePropertyChanged(ShowDaySettings_name);
            }
        }

        public class AutoFolderDescription
        {
            public SPOSettings.AutoFolderType Mode { get; set; }
            public string Description { get; set; }
        }

        public List<AutoFolderDescription> AutoFolderDescriptions { get; set; } = new List<AutoFolderDescription>()
        {
            new AutoFolderDescription()
                { Mode = SPOSettings.AutoFolderType.ByCapacity, Description = "Hierachy by capacity" },
            new AutoFolderDescription()
                { Mode = SPOSettings.AutoFolderType.ByDay, Description = "Hierachy by day" },
            new AutoFolderDescription()
                { Mode = SPOSettings.AutoFolderType.ByDayMonthYear, Description = "Hierachy by year/month/day" },
        };

        private string ShowCapacitySettings_name = "ShowCapacitySettings";
        public bool ShowCapacitySettings
        {
            get { return !(
                    FolderHandling == SPOSettings.FolderHandlingType.Auto &&
                    SelectedAutoFolderType == SPOSettings.AutoFolderType.ByCapacity 
            ); }
        }

        public string ShowDaySettings_name = "ShowDaySettings";
        public bool ShowDaySettings
        {
            get { return !(
                    FolderHandling == SPOSettings.FolderHandlingType.Auto &&
                    (SelectedAutoFolderType == SPOSettings.AutoFolderType.ByDay ||
                    SelectedAutoFolderType == SPOSettings.AutoFolderType.ByDayMonthYear)
             ); }
        }

        public bool ControlLoad
        {
            get { return settings.ControlLoad; }
            set { settings.ControlLoad = value; SendPropertyChanged(); }
        }

        public int MaxCapacity
        {
            get { return settings.MaxCapacity; }
            set { settings.MaxCapacity = value; SendPropertyChanged(); }
        }

        public string BasefolderName
        {
            get { return settings.BasefolderName; }
            set { settings.BasefolderName = value; SendPropertyChanged(); }
        }

        public int MaxDay
        {
            get { return settings.MaxDay; }
            set { settings.MaxDay = value; SendPropertyChanged(); }
        }
        #endregion
    }

    public class EnumMatchToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string checkValue = value.ToString();
            string targetValue = parameter.ToString();
            return checkValue.Equals(targetValue,
                     StringComparison.InvariantCultureIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Binding.DoNothing;

            bool useValue = (bool)value;
            string targetValue = parameter.ToString();
            if (useValue)
                return Enum.Parse(targetType, targetValue);

            return Binding.DoNothing;
        }
    }
}
