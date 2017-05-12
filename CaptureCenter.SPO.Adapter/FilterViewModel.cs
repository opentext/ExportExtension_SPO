using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Globalization;
using ExportExtensionCommon;

namespace CaptureCenter.SPO
{
    public class FilterViewModel : SIEEViewModel
    {
        private SPOListFilter filter;
        public SPOListFilter Filter
        {
            get { return filter; }
            set
            {
                filter = value;
                SendPropertyChanged("ValidBaseTypes");
                SendPropertyChanged("TypeTemplateRanges");
                SendPropertyChanged("TitleFilters");
                SendPropertyChanged("ForcedFields");

                if (ValidBaseTypes.Count > 0) SelectedTBaseType = ValidBaseTypes[0];
                if (TypeTemplateRanges.Count > 0) SelectedTypeTemplateRange = TypeTemplateRanges[0];
                if (TitleFilters.Count > 0) SelectedTitleFilter = TitleFilters[0];
                if (ForcedFields.Count > 0) SelectedForcedField = ForcedFields[0];
            }
        }

        public FilterViewModel(SPOListFilter Filter)
        {
            this.Filter = Filter;
        }

        #region Base types
        public ObservableCollection<SPOListFilter.BaseType> ValidBaseTypes
        {
            get { return Filter.ValidBaseTypes; }
            set { Filter.ValidBaseTypes = value; SendPropertyChanged(); }
        }

        public SPOListFilter.BaseType selectedTBaseType;
        public SPOListFilter.BaseType SelectedTBaseType
        {
            get { return selectedTBaseType; }
            set
            {
                SetField(ref selectedTBaseType, value);
                SendPropertyChanged(CanDeleteBaseType_name);
            }
        }

        private string CanDeleteBaseType_name = "CanDeleteBaseType";
        public bool CanDeleteBaseType
        {
            get { return SelectedTBaseType != null; }
        }

        public void BaseTypeDeleteHandler()
        {
            if (SelectedTBaseType == null) return;
            int idx = removeFromCollection(ValidBaseTypes, SelectedTBaseType);
            if (idx >= 0) SelectedTBaseType = ValidBaseTypes[idx];
        }
        #endregion

        #region Type template ranges
        public ObservableCollection<SPOListFilter.TypeTemplateRange> TypeTemplateRanges
        {
            get { return Filter.TypeTemplateRanges; }
            set { Filter.TypeTemplateRanges = value; SendPropertyChanged(); }
        }

        public SPOListFilter.TypeTemplateRange selectedTypeTemplateRange;
        public SPOListFilter.TypeTemplateRange SelectedTypeTemplateRange
        {
            get { return selectedTypeTemplateRange; }
            set {
                SetField(ref selectedTypeTemplateRange, value);
                SendPropertyChanged(CanDeleteTypeTemplateRange_name);
            }
        }

        private string CanDeleteTypeTemplateRange_name = "CanDeleteTypeTemplateRange";
        public bool CanDeleteTypeTemplateRange
        {
            get { return SelectedTypeTemplateRange != null; }
        }

        public void TemplateTypeRangeDeleteButtonHandler()
        {
            int idx = removeFromCollection(TypeTemplateRanges, SelectedTypeTemplateRange);
            if (idx >= 0) SelectedTypeTemplateRange = TypeTemplateRanges[idx];
        }
        #endregion

        #region Title filters
        public ObservableCollection<SPOListFilter.TitleFilter> TitleFilters
        {
            get { return Filter.TitleFilters; }
            set { Filter.TitleFilters = value; SendPropertyChanged(); }
        }

        public SPOListFilter.TitleFilter selectedTitleFilter;
        public SPOListFilter.TitleFilter SelectedTitleFilter
        {
            get { return selectedTitleFilter; }
            set
            {
                SetField(ref selectedTitleFilter, value);
                SendPropertyChanged(CanDeleteSelectedTitleFilter_name);
            }
        }

        private string CanDeleteSelectedTitleFilter_name = "CanDeleteSelectedTitleFilter";
        public bool CanDeleteSelectedTitleFilter
        {
            get { return SelectedTitleFilter != null; }
        }

        public void TitleFilterDeletHandler()
        {
            int idx = removeFromCollection(TitleFilters, SelectedTitleFilter);
            if (idx>= 0) SelectedTitleFilter = TitleFilters[idx];
        }
        #endregion

        #region Forced fields
        public ObservableCollection<SPOListFilter.ForcedField> ForcedFields
        {
            get { return Filter.ForcedFields; }
            set { Filter.ForcedFields = value; SendPropertyChanged(); }
        }

        public SPOListFilter.ForcedField selectedForcedField;
        public SPOListFilter.ForcedField SelectedForcedField
        {
            get { return selectedForcedField; }
            set
            {
                SetField(ref selectedForcedField, value);
                SendPropertyChanged(CanDeleteForcedField_name);
            }
        }

        private string CanDeleteForcedField_name = "CanDeleteForcedField";
        public bool CanDeleteForcedField
        {
            get { return SelectedForcedField != null; }
        }

        public void ForcedFieldDeleteHandler()
        {
            int idx = removeFromCollection(ForcedFields, SelectedForcedField);
            if (idx >= 0) SelectedForcedField = ForcedFields[idx];
        }

        public void SaveAsDefaulFiltertHandler()
        {
            SPOSettings.SaveAsDefaultFilter(Filter);
        }

        public void SaveFilterAsHandler()
        {
            SPOSettings.SaveFilterAs(Filter);
        }

        public void LoadFilterHandler()
        {
            SPOListFilter newFilter = SPOSettings.LoadFilter();
            if (newFilter != null) Filter = newFilter;
        }
        #endregion

        #region Utilities
        private int removeFromCollection<T>(Collection<T> collection, T selectedElement)
        {
            if (selectedElement == null) return -1;
            int index = collection.IndexOf(selectedElement);
            collection.Remove(selectedElement);
            if (collection.Count == 0) return -1;
            if (index >= collection.Count) index = collection.Count - 1;
            return index;
        }
        #endregion
    }

    #region Converters
    public class BaseTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is SPOListFilter.BaseType ? value : null;
        }
    }

    public class TypeTemplateRangeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is SPOListFilter.TypeTemplateRange ? value : null;
        }
    }

    public class TitleFilterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is SPOListFilter.TitleFilter ? value : null;
        }
    }

    public class ForcedFieldConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is SPOListFilter.ForcedField ? value : null;
        }
    }
    #endregion
}
