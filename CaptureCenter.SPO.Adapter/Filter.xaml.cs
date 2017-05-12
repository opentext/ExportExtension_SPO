using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CaptureCenter.SPO
{
    public partial class Filter : UserControl
    {
        public Filter()
        {
            InitializeComponent();
        }

        private void Button_BaseType_Click(object sender, RoutedEventArgs e)
        {
            ((FilterViewModel)DataContext).BaseTypeDeleteHandler();
        }

        private void Button_TemplateTypeRangeDelete_Click(object sender, RoutedEventArgs e)
        {
            ((FilterViewModel)DataContext).TemplateTypeRangeDeleteButtonHandler();
        }

        private void Button_TitleFilterDelete_Click(object sender, RoutedEventArgs e)
        {
            ((FilterViewModel)DataContext).TitleFilterDeletHandler();
        }

        private void Button_ForcedField_Click(object sender, RoutedEventArgs e)
        {
            ((FilterViewModel)DataContext).ForcedFieldDeleteHandler();
        }

        private void Button_SaveAsDefaultFilter_Click(object sender, RoutedEventArgs e)
        {
            ((FilterViewModel)DataContext).SaveAsDefaulFiltertHandler();
        }
        private void Button_SaveFilterAs_Click(object sender, RoutedEventArgs e)
        {
            ((FilterViewModel)DataContext).SaveFilterAsHandler();
        }
        private void Button_LoadFilter_Click(object sender, RoutedEventArgs e)
        {
            ((FilterViewModel)DataContext).LoadFilterHandler();
        }
    }
}
