using diplom.Models;
using diplom.ViewModels;
using System;
using System.Windows;

namespace diplom.Views
{
    public partial class ScheduleGeneratorWindow : Window
    {
        private ScheduleGeneratorViewModel _viewModel;

        public DateTime? ScheduleStartDate { get; private set; }
        public DateTime? ScheduleEndDate { get; private set; }
        public string SelectedDebtStatus { get; private set; }
        public System.Collections.Generic.List<int> SelectedStudentIds { get; private set; }

        public ScheduleGeneratorWindow()
        {
            InitializeComponent();
            _viewModel = new ScheduleGeneratorViewModel();
            this.DataContext = _viewModel;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.IsDateRangeValid())
            {
                MessageBox.Show("Пожалуйста, проверьте корректность выбранных дат.\n" +
                                "Дата не может быть ранее сегодняшнего дня.",
                                "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Вы уверены? Старое расписание будет полностью заменено.\n" +
                               "Эта операция может занять несколько минут.",
                               "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ScheduleStartDate = _viewModel.GetScheduleStartDate();
                ScheduleEndDate = _viewModel.GetScheduleEndDate();
                SelectedDebtStatus = _viewModel.SelectedStatus;
                SelectedStudentIds = _viewModel.GetSelectedStudentIds();

                this.DialogResult = true;
                this.Close();
            }
        }
    }
}
