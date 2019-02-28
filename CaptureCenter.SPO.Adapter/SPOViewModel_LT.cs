using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ExportExtensionCommon;
using DOKuStar.Diagnostics.Tracing;
using System.Globalization;

namespace CaptureCenter.SPO
{
    public class SPOViewModel_LT : ModelBase
    {
        #region Construction
        private SPOViewModel vm;
        private SPOSettings settings;

        public SPOViewModel_LT(SPOViewModel vm)
        {
            this.vm = vm;
            settings = vm.SPOSettings;

            Cultures = CultureInfo.GetCultures(CultureTypes.AllCultures).OrderBy(n => n.DisplayName).ToList();
        }

        public void Initialize(UserControl control) { }
 
        public bool ActivateTab()
        {
            if (! vm.DataLoaded) GetLists();
            vm.DataLoaded = true;
            return true;
        }
        #endregion

        #region Properties
        private List<SPOList> lists;
        public List<SPOList> Lists
        {
            get { return lists; }
            set { SetField(ref lists, value); }
        }

        public SPOList SelectedList
        {
            get { return settings.SelectedList; }
            set { settings.SelectedList = value; SendPropertyChanged(); }
        }

        public List<SPOField> Fields
        {
            get { return settings.Fields; }
            set { settings.Fields = value; SendPropertyChanged(); }
        }

        private List<CultureInfo> cultures;
        public List<CultureInfo> Cultures
        {
            get { return cultures; }
            set { SetField(ref cultures, value); }
        }
        public CultureInfo SelectedCulture
        {
            get { return new CultureInfo(settings.SelectedCultureInfoName, UseUserOverride); }
            set
            {
                settings.SelectedCultureInfoName = value.Name;
                SendPropertyChanged();
            }
        }
        public bool UseUserOverride
        {
            get { return settings.UseUserOverride; }
            set { settings.UseUserOverride = value; SendPropertyChanged(); }
        }

        #endregion

        #region Functions
        public void GetLists()
        {
            var save = SelectedList;    // unclear. Setting Lists will set SelectedList to null
            lists = vm.SPOClient.GetLists();
            Lists = vm.SPOSettings.ListFilter.Filter(Lists);
            SelectedList = save;

            if (Lists.Count() == 0) return;

            if (SelectedList == null)
            {
                SelectedList = SelectedList = Lists[0];
                return;
            }
            List<SPOList> ll = Lists.Where(n => n.Id == SelectedList.Id).ToList();
            SelectedList = ll.Count() > 0 ? ll.First() : SelectedList = Lists[0];
        }

        public void LoadFields()
        {
            Fields = vm.SPOClient.GetFields(SelectedList, 
                vm.SPOSettings.ListFilter.ForcedFields.Select(n => n.FieldTitle).ToList());
            foreach (SPOField f in Fields)
                if ((vm.SPOClient.IsTypeSupported(f.TypeName) && f.CustomField) || f.Title == "Title")
                    f.Use = true;
        }

        public void LaunchFilterDialog()
        {
            var filter = new Filter();
            var filterVm = new FilterViewModel((SPOListFilter)SIEESerializer.Clone(
                                vm.SPOSettings.ListFilter));

            if (SIEEOKCancelDialogViewModel.LaunchOkCancelDialog("Define list and field filters", filter, filterVm))
            {
                vm.SPOSettings.ListFilter = (SPOListFilter)SIEESerializer.Clone(
                                            ((FilterViewModel)filter.DataContext).Filter);
                GetLists();
            }
        }
        #endregion
    }

}
