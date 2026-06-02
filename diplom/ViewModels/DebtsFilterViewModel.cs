using diplom.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace diplom.ViewModels
{
    public class DelegateCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }


        public DelegateCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }


        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

    public class DebtsFilterViewModel : INotifyPropertyChanged
    {
        private List<DebtInfo> _allDebts;
        private ObservableCollection<DebtInfo> _filteredDebts;
        private List<string> _students;
        private List<string> _disciplines;
        private List<string> _groups;

        private string _selectedStudent;
        private string _selectedDiscipline;
        private string _selectedGroup;
        private string _selectedTeacher;
        private string _searchText;

        private List<string> _teachers;
        private List<string> _clearedFilters;

        private string _selectedClearedFilter;

        public ObservableCollection<DebtInfo> FilteredDebts
        {
            get { return _filteredDebts; }
            set { _filteredDebts = value; OnPropertyChanged(nameof(FilteredDebts)); }
        }

        public List<string> Students
        {
            get { return _students; }
            set { _students = value; OnPropertyChanged(nameof(Students)); }
        }

        public List<string> Disciplines
        {
            get { return _disciplines; }
            set { _disciplines = value; OnPropertyChanged(nameof(Disciplines)); }
        }

        public List<string> Groups
        {
            get { return _groups; }
            set { _groups = value; OnPropertyChanged(nameof(Groups)); }
        }

        public List<string> Teachers
        {
            get { return _teachers; }
            set { _teachers = value; OnPropertyChanged(nameof(Teachers)); }
        }

        public List<string> ClearedFilters
        {
            get { return _clearedFilters; }
            set { _clearedFilters = value; OnPropertyChanged(nameof(ClearedFilters)); }
        }

        public string SelectedStudent
        {
            get { return _selectedStudent; }
            set { _selectedStudent = value; OnPropertyChanged(nameof(SelectedStudent)); ApplyFilters(); }
        }

        public string SelectedDiscipline
        {
            get { return _selectedDiscipline; }
            set { _selectedDiscipline = value; OnPropertyChanged(nameof(SelectedDiscipline)); ApplyFilters(); }
        }

        public string SelectedGroup
        {
            get { return _selectedGroup; }
            set { _selectedGroup = value; OnPropertyChanged(nameof(SelectedGroup)); ApplyFilters(); }
        }

        public string SelectedTeacher
        {
            get { return _selectedTeacher; }
            set { _selectedTeacher = value; OnPropertyChanged(nameof(SelectedTeacher)); ApplyFilters(); }
        }

        public string SelectedClearedFilter
        {
            get { return _selectedClearedFilter; }
            set { _selectedClearedFilter = value; OnPropertyChanged(nameof(SelectedClearedFilter)); ApplyFilters(); }
        }

        public string SearchText
        {
            get { return _searchText; }
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); ApplyFilters(); }
        }

        public ICommand ResetFiltersCommand { get; }

        public DebtsFilterViewModel()
        {
            _filteredDebts = new ObservableCollection<DebtInfo>();

            // Загрузка базовых списков фильтров один раз при инициализации.
            try
            {
                var students = OpenContext.db.Students
                                  .Select(s => s.LastName + " " + s.FirstName + " " + s.MiddleName)
                                  .Distinct()
                                  .OrderBy(x => x)
                                  .ToList();
                Students = new List<string> { "Все студенты" };
                Students.AddRange(students);

                var disciplines = OpenContext.db.Disciplines
                                    .Select(d => d.DisciplineName)
                                    .Distinct()
                                    .OrderBy(x => x)
                                    .ToList();
                Disciplines = new List<string> { "Все предметы" };
                Disciplines.AddRange(disciplines);

                var groups = OpenContext.db.Groups
                               .Select(g => g.GroupName)
                               .Distinct()
                               .OrderBy(x => x)
                               .ToList();
                Groups = new List<string> { "Все группы" };
                Groups.AddRange(groups);

                var teachers = OpenContext.db.Teachers
                                 .Select(t => t.LastName + " " + t.FirstName.Substring(0,1) + ".")
                                 .Distinct()
                                 .OrderBy(x => x)
                                 .ToList();
                Teachers = new List<string> { "Все преподаватели" };
                Teachers.AddRange(teachers);
                // По умолчанию выбираем "Все" для каждого ComboBox, чтобы при открытии окна не было пустого значения
                SelectedStudent = "Все студенты";
                SelectedDiscipline = "Все предметы";
                SelectedGroup = "Все группы";
                SelectedTeacher = "Все преподаватели";
            }
            catch
            {
                Students = new List<string> { "Все студенты" };
                Disciplines = new List<string> { "Все предметы" };
                Groups = new List<string> { "Все группы" };
                Teachers = new List<string> { "Все преподаватели" };
            }

            ResetFiltersCommand = new DelegateCommand(_ => ResetFilters());

            // Первичная загрузка долгов и заполнение таблицы
            LoadData();

            // Инициализация фильтра по флагу сданности
            ClearedFilters = new List<string> { "Все", "Сданные", "Не сданные" };
            SelectedClearedFilter = "Все";
        }

        private void LoadData()
        {
            // Сохраним текущие списки и выбранные значения на случай, если новая загрузка вернёт неполные данные
            var previousStudents = Students != null ? new List<string>(Students) : null;
            var previousDisciplines = Disciplines != null ? new List<string>(Disciplines) : null;
            var previousGroups = Groups != null ? new List<string>(Groups) : null;
            var previousTeachers = Teachers != null ? new List<string>(Teachers) : null;

            var prevSelectedStudent = SelectedStudent;
            var prevSelectedDiscipline = SelectedDiscipline;
            var prevSelectedGroup = SelectedGroup;
            var prevSelectedTeacher = SelectedTeacher;

            try
            {
                // Загружаем все долги с информацией о кабинетах из расписания
                var list = (from d in OpenContext.db.Debts
                             join s in OpenContext.db.Students on d.StudentID equals s.StudentID
                             join g in OpenContext.db.Groups on s.GroupID equals g.GroupID
                             join disc in OpenContext.db.Disciplines on d.DisciplineID equals disc.DisciplineID
                             join t in OpenContext.db.Teachers on d.TeacherID equals t.TeacherID
                             join sched in OpenContext.db.Schedules on d.DebtID equals sched.DebtID into schedules
                             from sched in schedules.DefaultIfEmpty()
                             join c in OpenContext.db.Classrooms on (sched != null ? sched.ClassroomID : (int?)null) equals c.ClassroomID into classrooms
                             from c in classrooms.DefaultIfEmpty()
                             join ts in OpenContext.db.TimeSlots on (sched != null ? (int?)sched.TimeSlotID : (int?)null) equals ts.TimeSlotID into times
                             from ts in times.DefaultIfEmpty()
                             select new
                             {
                                 StudentCardNumber = s.StudentCardNumber,
                                 StudentName = s.LastName + " " + s.FirstName + " " + s.MiddleName,
                                 GroupName = g.GroupName,
                                 DisciplineName = disc.DisciplineName,
                                 TeacherName = t.LastName + " " + t.FirstName.Substring(0, 1) + "." + (t.MiddleName != null ? t.MiddleName.Substring(0, 1) + "." : ""),
                                 Status = d.DebtStatus,
                                 DebtID = d.DebtID,
                                 DateRecorded = d.DateRecorded,
                                 ClassroomNumber = c != null ? c.ClassroomNumber : "Не назначен",
                                 ExamDate = sched != null ? sched.ExamDate : (DateTime?)null,
                                 TimeSlotNumber = ts != null ? (int?)ts.SlotNumber : (sched != null ? (int?)sched.TimeSlot : (int?)null),
                                 TimeSlotID = sched != null ? (int?)sched.TimeSlotID : (int?)null,
                                 IsCleared = d.IsCleared
                             }).ToList();

                

                _allDebts = list.Select(x => new DebtInfo
                {
                    StudentCardNumber = x.StudentCardNumber,
                    StudentName = x.StudentName,
                    GroupName = x.GroupName,
                    DisciplineName = x.DisciplineName,
                    TeacherName = x.TeacherName,
                    Status = x.Status,
                    DebtID = x.DebtID,
                    DateRecorded = x.DateRecorded,
                    ClassroomNumber = x.ClassroomNumber,
                    ExamDate = x.ExamDate,
                    TimeSlotNumber = x.TimeSlotNumber,
                    TimeSlotID = x.TimeSlotID,
                    IsCleared = x.IsCleared,
                    TimeSlotDescription = x.TimeSlotNumber.HasValue ? ("Пара " + x.TimeSlotNumber.Value.ToString()) : "Не назначена"
                }).ToList();

                // Не обновляем списки фильтров здесь — они загружаются один раз в конструкторе.
                // Просто применяем фильтры к загруженным долгам.
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка");
            }
        }

        // Публичный метод для перезагрузки данных извне
        public void Refresh()
        {
            LoadData();
        }

        private void ApplyFilters()
        {
            if (_allDebts == null || _allDebts.Count == 0)
                return;

            var filtered = _allDebts.AsEnumerable();

            // Применяем фильтр по студенту
            if (!string.IsNullOrEmpty(SelectedStudent) && SelectedStudent != "Все студенты")
            {
                filtered = filtered.Where(d => d.StudentName == SelectedStudent);
            }

            // Применяем фильтр по предмету
            if (!string.IsNullOrEmpty(SelectedDiscipline) && SelectedDiscipline != "Все предметы")
            {
                filtered = filtered.Where(d => d.DisciplineName == SelectedDiscipline);
            }

            // Применяем фильтр по группе
            if (!string.IsNullOrEmpty(SelectedGroup) && SelectedGroup != "Все группы")
            {
                filtered = filtered.Where(d => d.GroupName == SelectedGroup);
            }

            // Применяем фильтр по преподавателю
            if (!string.IsNullOrEmpty(SelectedTeacher) && SelectedTeacher != "Все преподаватели")
            {
                filtered = filtered.Where(d => d.TeacherName == SelectedTeacher);
            }

            // Применяем фильтр по поисковому тексту
            if (!string.IsNullOrEmpty(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(d =>
                    d.StudentName.ToLower().Contains(search) ||
                    d.StudentCardNumber.ToLower().Contains(search) ||
                    d.DisciplineName.ToLower().Contains(search) ||
                    d.GroupName.ToLower().Contains(search));
            }

            // Применяем фильтр по флагу сданности
            if (!string.IsNullOrEmpty(SelectedClearedFilter) && SelectedClearedFilter != "Все")
            {
                if (SelectedClearedFilter == "Сданные")
                {
                    filtered = filtered.Where(d => d.IsCleared == true || d.Status == "Сдан");
                }
                else if (SelectedClearedFilter == "Не сданные")
                {
                    filtered = filtered.Where(d => !(d.IsCleared == true || d.Status == "Сдан"));
                }
            }

            FilteredDebts = new ObservableCollection<DebtInfo>(filtered.ToList());
        }

        private void ResetFilters()
        {
            SelectedStudent = "Все студенты";
            SelectedDiscipline = "Все предметы";
            SelectedGroup = "Все группы";
            SelectedTeacher = "Все преподаватели";
            SearchText = string.Empty;
            ApplyFilters();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DebtInfo
    {
        public int DebtID { get; set; }
        public string StudentCardNumber { get; set; }
        public string StudentName { get; set; }
        public string GroupName { get; set; }
        public string DisciplineName { get; set; }
        public string TeacherName { get; set; }
        public string Status { get; set; }
        public DateTime DateRecorded { get; set; }
        public string ClassroomNumber { get; set; }
        public DateTime? ExamDate { get; set; }
        public bool? IsCleared { get; set; }
            public int? TimeSlotNumber { get; set; }
            public string TimeSlotDescription { get; set; }
        public int? TimeSlotID { get; set; }
    }
}
