using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace diplom.Views
{
    /// <summary>
    /// Interaction logic for ConflictReportWindow.xaml
    /// </summary>
    public partial class ConflictReportWindow : Window
    {
        private string _reportPath;
        public ConflictReportWindow(System.Collections.Generic.List<string> conflictDetails, string reportPath)
        {
            InitializeComponent();
            _reportPath = reportPath;

            // Подсчёт по типам
            int teacher = conflictDetails.Count(c => c.StartsWith("Teacher conflict:"));
            int room = conflictDetails.Count(c => c.StartsWith("Classroom conflict:"));
            int student = conflictDetails.Count(c => c.StartsWith("Student conflict:"));
            int soft = conflictDetails.Count(c => c.StartsWith("Soft same-day student:"));

            TbTeacherCount.Text = teacher.ToString();
            TbRoomCount.Text = room.ToString();
            TbStudentCount.Text = student.ToString();
            TbSoftCount.Text = soft.ToString();

            // Показать примеры
            foreach (var line in conflictDetails.Take(200))
            {
                LbExamples.Items.Add(line);
            }
        }

        private void BtnOpenReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_reportPath) && File.Exists(_reportPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() { FileName = _reportPath, UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Файл отчета не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть файл: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}