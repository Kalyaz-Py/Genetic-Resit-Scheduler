using diplom.ViewModels;
using System.Windows;

namespace diplom.Views
{
    public partial class AddDebtWindow : Window
    {
        private AddDebtViewModel _viewModel;

        public AddDebtWindow()
        {
            InitializeComponent();
            _viewModel = new AddDebtViewModel();
            this.DataContext = _viewModel;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ValidateAndAddDebt())
            {
                // Очищаем форму для возможности добавления еще одной задолженности
                _viewModel.SelectedStudent = null;
                _viewModel.SelectedDiscipline = null;
                _viewModel.SelectedTeacher = null;
                _viewModel.DebtStatus = "Активна";

                MessageBox.Show("Вы можете добавить еще одну задолженность или закрыть окно.", 
                                "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
