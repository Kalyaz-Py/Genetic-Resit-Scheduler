using diplom.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace diplom.ViewModels
{
    public class AddDebtViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Student> _students;
        private ObservableCollection<Discipline> _disciplines;
        private ObservableCollection<Discipline> _filteredDisciplines;
        private ObservableCollection<Teacher> _teachers;

        private Student _selectedStudent;
        private Discipline _selectedDiscipline;
        private Teacher _selectedTeacher;
        private string _debtStatus;

        public ObservableCollection<Student> Students
        {
            get { return _students; }
            set { _students = value; OnPropertyChanged(nameof(Students)); }
        }

        public ObservableCollection<Discipline> Disciplines
        {
            get { return _disciplines; }
            set { _disciplines = value; OnPropertyChanged(nameof(Disciplines)); }
        }

        public ObservableCollection<Discipline> FilteredDisciplines
        {
            get { return _filteredDisciplines; }
            set { _filteredDisciplines = value; OnPropertyChanged(nameof(FilteredDisciplines)); }
        }

        public ObservableCollection<Teacher> Teachers
        {
            get { return _teachers; }
            set { _teachers = value; OnPropertyChanged(nameof(Teachers)); }
        }

        public Student SelectedStudent
        {
            get { return _selectedStudent; }
            set { _selectedStudent = value; OnPropertyChanged(nameof(SelectedStudent)); }
        }

        public Discipline SelectedDiscipline
        {
            get { return _selectedDiscipline; }
            set { _selectedDiscipline = value; OnPropertyChanged(nameof(SelectedDiscipline)); }
        }

        public Teacher SelectedTeacher
        {
            get { return _selectedTeacher; }
            set { _selectedTeacher = value; OnPropertyChanged(nameof(SelectedTeacher)); UpdateFilteredDisciplines(); }
        }

        public string DebtStatus
        {
            get { return _debtStatus; }
            set { _debtStatus = value; OnPropertyChanged(nameof(DebtStatus)); }
        }

        public ICommand AddDebtCommand { get; }

        public AddDebtViewModel()
        {
            Students = new ObservableCollection<Student>();
            Disciplines = new ObservableCollection<Discipline>();
            Teachers = new ObservableCollection<Teacher>();

            DebtStatus = "Активна";

            AddDebtCommand = new DelegateCommand(_ => OnAddDebt(), _ => CanAddDebt());

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Загружаем студентов
                var students = OpenContext.db.Students.ToList();
                foreach (var student in students)
                {
                    Students.Add(student);
                }

                // Загружаем дисциплины
                var disciplines = OpenContext.db.Disciplines.ToList();
                foreach (var discipline in disciplines)
                {
                    Disciplines.Add(discipline);
                }

                // Загружаем преподавателей
                var teachers = OpenContext.db.Teachers.ToList();
                foreach (var teacher in teachers)
                {
                    Teachers.Add(teacher);
                }

                // Изначально фильтрованные дисциплины = все дисциплины
                FilteredDisciplines = new ObservableCollection<Discipline>(Disciplines);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка");
            }
        }

        private void UpdateFilteredDisciplines()
        {
            try
            {
                if (SelectedTeacher == null)
                {
                    // показать все дисциплины, если преподаватель не выбран
                    FilteredDisciplines = new ObservableCollection<Discipline>(Disciplines);
                    return;
                }

                // Используем навигацию many-to-many: Teacher.Disciplines
                var teacherId = SelectedTeacher.TeacherID;
                var allowed = OpenContext.db.Teachers
                              .Where(t => t.TeacherID == teacherId)
                              .SelectMany(t => t.Disciplines)
                              .ToList();

                // Если таблица маппинга отсутствует, падаем обратно на все дисциплины
                if (allowed == null || allowed.Count == 0)
                {
                    FilteredDisciplines = new ObservableCollection<Discipline>(Disciplines);
                    return;
                }

                FilteredDisciplines = new ObservableCollection<Discipline>(allowed);
            }
            catch
            {
                // безопасный откат на полный список
                FilteredDisciplines = new ObservableCollection<Discipline>(Disciplines);
            }
        }

        private bool CanAddDebt()
        {
            return SelectedStudent != null && SelectedDiscipline != null && SelectedTeacher != null;
        }

        private void OnAddDebt()
        {
            // Логика переделана в окне
        }

        public bool ValidateAndAddDebt()
        {
            if (SelectedStudent == null)
            {
                System.Windows.MessageBox.Show("Пожалуйста, выберите студента.", "Ошибка валидации");
                return false;
            }

            if (SelectedDiscipline == null)
            {
                System.Windows.MessageBox.Show("Пожалуйста, выберите дисциплину.", "Ошибка валидации");
                return false;
            }

            if (SelectedTeacher == null)
            {
                System.Windows.MessageBox.Show("Пожалуйста, выберите преподавателя.", "Ошибка валидации");
                return false;
            }

            try
            {
                // Проверяем, есть ли уже такая задолженность
                bool debtExists = OpenContext.db.Debts.Any(d => 
                    d.StudentID == SelectedStudent.StudentID &&
                    d.DisciplineID == SelectedDiscipline.DisciplineID &&
                    d.DebtStatus == "Активна");

                if (debtExists)
                {
                    System.Windows.MessageBox.Show("Такая задолженность уже существует.", "Предупреждение");
                    return false;
                }

                // Создаем новую задолженность
                Debt newDebt = new Debt
                {
                    StudentID = SelectedStudent.StudentID,
                    DisciplineID = SelectedDiscipline.DisciplineID,
                    TeacherID = SelectedTeacher.TeacherID,
                    DebtStatus = DebtStatus,
                    DateRecorded = DateTime.Now
                };

                OpenContext.db.Debts.Add(newDebt);
                OpenContext.db.SaveChanges();

                System.Windows.MessageBox.Show($"Задолженность успешно добавлена!\n" +
                                              $"Студент: {SelectedStudent.LastName} {SelectedStudent.FirstName}\n" +
                                              $"Дисциплина: {SelectedDiscipline.DisciplineName}\n" +
                                              $"Преподаватель: {SelectedTeacher.LastName}",
                                              "Успех", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при добавлении задолженности: {ex.Message}", "Ошибка");
                return false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Экспорт ведомости задолженностей в Word (.docx)
        // Формирует строки по шаблону: [Код_Группы],[ФИО_Студента],Дифференцированный зачет:,[Предмет1],[Предмет2],Экзамен:,[Предмет3]
        
        
        
        

        
    }
}
