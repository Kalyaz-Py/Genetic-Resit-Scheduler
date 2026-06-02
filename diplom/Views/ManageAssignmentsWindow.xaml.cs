using diplom.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace diplom.Views
{
    /// <summary>
    /// Interaction logic for ManageAssignmentsWindow.xaml
    /// </summary>
    public partial class ManageAssignmentsWindow : Window
    {
        private List<Teacher> allTeachers;
        private List<Discipline> allDisciplines;
        private Discipline selectedDiscipline;
        private Teacher selectedTeacher;
        private bool modeByDiscipline = true; // true: выбираем дисциплину -> назначаем преподавателя; false: выбираем преподавателя -> назначаем дисциплину

        public ManageAssignmentsWindow()
        {
            InitializeComponent();
            LoadData();

            LbDisciplines.SelectionChanged += LbDisciplines_SelectionChanged;
            LbTeachers.SelectionChanged += LbTeachers_SelectionChanged;
        }

        private void LoadData()
        {
            try
            {
                allTeachers = OpenContext.db.Teachers.ToList();
                allDisciplines = OpenContext.db.Disciplines.ToList();

                // Подготовим объект FullName для отображения преподавателя
                var teachersForList = allTeachers.Select(t => new { TeacherID = t.TeacherID, FullName = string.Join(" ", new[] { t.LastName, t.FirstName, t.MiddleName }.Where(s => !string.IsNullOrWhiteSpace(s))) }).ToList();
                LbTeachers.ItemsSource = teachersForList;

                LbDisciplines.ItemsSource = allDisciplines;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LbDisciplines_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LbDisciplines.SelectedItem == null)
            {
                LbAssignedTeachers.ItemsSource = null;
                return;
            }

            selectedDiscipline = LbDisciplines.SelectedItem as Discipline;
            if (selectedDiscipline == null)
                return;

            // Обновим список назначенных: загрузим из БД свежую запись Discipline с связанными Teachers
            var disc = OpenContext.db.Disciplines.FirstOrDefault(d => d.DisciplineID == selectedDiscipline.DisciplineID);
            if (disc == null)
                return;

            var assigned = disc.Teachers.Select(t => new { TeacherID = t.TeacherID, FullName = string.Join(" ", new[] { t.LastName, t.FirstName, t.MiddleName }.Where(s => !string.IsNullOrWhiteSpace(s))) }).ToList();
            LbAssignedTeachers.ItemsSource = assigned;
        }

        private void BtnAssign_Click(object sender, RoutedEventArgs e)
        {
            if (modeByDiscipline)
            {
                if (selectedDiscipline == null)
                {
                    MessageBox.Show("Выберите дисциплину", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (LbTeachers.SelectedItem == null)
                {
                    MessageBox.Show("Выберите преподавателя для назначения", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int teacherId = (int)LbTeachers.SelectedValue;
                var teacher = OpenContext.db.Teachers.FirstOrDefault(t => t.TeacherID == teacherId);
                if (teacher == null) return;

                var disc = OpenContext.db.Disciplines.FirstOrDefault(d => d.DisciplineID == selectedDiscipline.DisciplineID);
                if (disc == null) return;

                if (!disc.Teachers.Any(t => t.TeacherID == teacher.TeacherID))
                {
                    disc.Teachers.Add(teacher);
                    try { OpenContext.db.SaveChanges(); } catch (Exception ex) { MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
                }

                // Обновим отображение
                LbDisciplines_SelectionChanged(null, null);
            }
            else
            {
                if (selectedTeacher == null)
                {
                    MessageBox.Show("Выберите преподавателя", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (LbDisciplines.SelectedItem == null)
                {
                    MessageBox.Show("Выберите дисциплину для назначения", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int discId = (int)LbDisciplines.SelectedValue;
                var disc = OpenContext.db.Disciplines.FirstOrDefault(d => d.DisciplineID == discId);
                if (disc == null) return;

                var teacher = OpenContext.db.Teachers.FirstOrDefault(t => t.TeacherID == selectedTeacher.TeacherID);
                if (teacher == null) return;

                if (!teacher.Disciplines.Any(d => d.DisciplineID == disc.DisciplineID))
                {
                    teacher.Disciplines.Add(disc);
                    try { OpenContext.db.SaveChanges(); } catch (Exception ex) { MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
                }

                // Обновим отображение
                LbTeachers_SelectionChanged(null, null);
            }
        }

        private void BtnUnassign_Click(object sender, RoutedEventArgs e)
        {
            if (modeByDiscipline)
            {
                if (selectedDiscipline == null)
                {
                    MessageBox.Show("Выберите дисциплину", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (LbAssignedTeachers.SelectedItem == null)
                {
                    MessageBox.Show("Выберите назначенного преподавателя для отвязки", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int teacherId = (int)LbAssignedTeachers.SelectedValue;
                var teacher = OpenContext.db.Teachers.FirstOrDefault(t => t.TeacherID == teacherId);
                if (teacher == null) return;

                var disc = OpenContext.db.Disciplines.FirstOrDefault(d => d.DisciplineID == selectedDiscipline.DisciplineID);
                if (disc == null) return;

                var rel = disc.Teachers.FirstOrDefault(t => t.TeacherID == teacher.TeacherID);
                if (rel != null)
                {
                    disc.Teachers.Remove(rel);
                    try { OpenContext.db.SaveChanges(); } catch (Exception ex) { MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
                }

                LbDisciplines_SelectionChanged(null, null);
            }
            else
            {
                if (selectedTeacher == null)
                {
                    MessageBox.Show("Выберите преподавателя", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (LbDisciplines.SelectedItem == null)
                {
                    MessageBox.Show("Выберите дисциплину для отвязки", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int discId = (int)LbDisciplines.SelectedValue;
                var disc = OpenContext.db.Disciplines.FirstOrDefault(d => d.DisciplineID == discId);
                if (disc == null) return;

                var teacher = OpenContext.db.Teachers.FirstOrDefault(t => t.TeacherID == selectedTeacher.TeacherID);
                if (teacher == null) return;

                var rel = teacher.Disciplines.FirstOrDefault(d => d.DisciplineID == disc.DisciplineID);
                if (rel != null)
                {
                    teacher.Disciplines.Remove(rel);
                    try { OpenContext.db.SaveChanges(); } catch (Exception ex) { MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
                }

                LbTeachers_SelectionChanged(null, null);
            }
        }

        private void LbTeachers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LbTeachers.SelectedItem == null)
            {
                selectedTeacher = null;
                if (!modeByDiscipline)
                    LbAssignedTeachers.ItemsSource = null;
                return;
            }

            int id = (int)LbTeachers.SelectedValue;
            selectedTeacher = OpenContext.db.Teachers.FirstOrDefault(t => t.TeacherID == id);

            if (!modeByDiscipline)
            {
                // Показать дисциплины, связанные с выбранным преподавателем
                var teacher = OpenContext.db.Teachers.FirstOrDefault(t => t.TeacherID == id);
                if (teacher == null)
                {
                    LbAssignedTeachers.ItemsSource = null;
                    return;
                }

                var assigned = teacher.Disciplines.Select(d => new { DisciplineID = d.DisciplineID, DisciplineName = d.DisciplineName }).ToList();
                LbAssignedTeachers.DisplayMemberPath = "DisciplineName";
                LbAssignedTeachers.SelectedValuePath = "DisciplineID";
                LbAssignedTeachers.ItemsSource = assigned;
                TxtAssignedLabel.Text = "Дисциплины, связанные с выбранным преподавателем:";
            }
        }

        private void BtnToggleMode_Click(object sender, RoutedEventArgs e)
        {
            modeByDiscipline = !modeByDiscipline;
            if (modeByDiscipline)
            {
                BtnToggleMode.Content = "Режим: по дисциплине";
                TxtAssignedLabel.Text = "Преподаватели, связанные с выбранной дисциплиной:";
                LbAssignedTeachers.DisplayMemberPath = "FullName";
                LbAssignedTeachers.SelectedValuePath = "TeacherID";
                // восстановим обработчик и обновим представление
                LbDisciplines_SelectionChanged(null, null);
            }
            else
            {
                BtnToggleMode.Content = "Режим: по преподавателю";
                // поменяем местами заголовки и наполнение
                TxtAssignedLabel.Text = "Дисциплины, связанные с выбранным преподавателем:";
                LbAssignedTeachers.DisplayMemberPath = "DisciplineName";
                LbAssignedTeachers.SelectedValuePath = "DisciplineID";
                // если уже выбран преподаватель, обновим список
                LbTeachers_SelectionChanged(null, null);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenContext.db.SaveChanges();
                MessageBox.Show("Изменения сохранены", "Сохранено", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnRefreshDisc_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
    }
}
