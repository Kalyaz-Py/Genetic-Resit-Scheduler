using diplom.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace diplom.Views
{
    /// <summary>
    /// Логика взаимодействия для StudentGuestWindow.xaml
    /// </summary>
    public partial class StudentGuestWindow : Window
    {
        public StudentGuestWindow()
        {
            InitializeComponent();
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string cardNumber = TxtCardNumber.Text.Trim();

            if (string.IsNullOrEmpty(cardNumber))
            {
                MessageBox.Show("Пожалуйста, введите номер зачетной книжки.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 1. Ищем студента по номеру зачетки через наш единый контекст db
                var student = OpenContext.db.Students
                    .FirstOrDefault(s => s.StudentCardNumber == cardNumber);

                if (student == null)
                {
                    MessageBox.Show("Студент с таким номером зачетной книжки не найден в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    // Скрываем обе панели на случай прошлых поисков
                    SuccessPanel.Visibility = Visibility.Collapsed;
                    DebtPanel.Visibility = Visibility.Collapsed;
                    return;
                }

                // 2. Проверяем наличие активных задолженностей для этого студента
                // Предполагаем, что статус "Активна" означает наличие долга
                var activeDebts = OpenContext.db.Debts
                    .Where(d => d.StudentID == student.StudentID && d.DebtStatus == "Активна")
                    .ToList();

                // 3. Бинарное ветвление логики (Зеленый / Красный коридор)
                if (activeDebts.Count == 0)
                {
                    // ЗЕЛЕНЫЙ КОРИДОР: Долгов нет
                    SuccessPanel.Visibility = Visibility.Visible;
                    DebtPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // КРАСНЫЙ КОРИДОР: Долги есть, вытаскиваем расписание пересдач для этих долгов
                    // Используем LINQ-запрос для объединения таблиц и формирования красивой плоской структуры
                    var scheduleData = (from debt in activeDebts
                                        join sched in OpenContext.db.Schedules on debt.DebtID equals sched.DebtID
                                        join disc in OpenContext.db.Disciplines on debt.DisciplineID equals disc.DisciplineID
                                        join teach in OpenContext.db.Teachers on debt.TeacherID equals teach.TeacherID
                                        join classrm in OpenContext.db.Classrooms on sched.ClassroomID equals classrm.ClassroomID
                                        select new
                                        {
                                            DisciplineName = disc.DisciplineName,
                                            TeacherName = teach.LastName + " " + teach.FirstName.Substring(0, 1) + ".",
                                            ExamDate = sched.ExamDate,
                                            TimeSlot = sched.TimeSlot,
                                            ClassroomNumber = classrm.ClassroomNumber
                                        }).ToList();

                    // Привязываем полученные данные к таблице DataGrid
                    DgSchedule.ItemsSource = scheduleData;

                    // Меняем видимость панелей
                    SuccessPanel.Visibility = Visibility.Collapsed;
                    DebtPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обращении к базе данных: {ex.Message}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = new MainWindow();  
            window.Show();
            this.Close();
        }
    }
}
