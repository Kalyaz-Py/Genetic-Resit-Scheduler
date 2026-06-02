using diplom.ViewModels;
using System.Collections.Generic;
using System.Windows;

namespace diplom.Views
{
    public partial class UpdatedSchedulesWindow : Window
    {
        public UpdatedSchedulesWindow(List<UpdatedScheduleInfo> items)
        {
            InitializeComponent();
            DgUpdated.ItemsSource = items;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
