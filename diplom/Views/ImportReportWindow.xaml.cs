using diplom.ViewModels;
using System.Collections.Generic;
using System.Windows;

namespace diplom.Views
{
    public partial class ImportReportWindow : Window
    {
        public ImportReportWindow(List<ImportReportItem> items)
        {
            InitializeComponent();
            DgReport.ItemsSource = items;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
