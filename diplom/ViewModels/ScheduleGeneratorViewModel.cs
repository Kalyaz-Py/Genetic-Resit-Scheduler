using diplom.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace diplom.ViewModels
{
    public class ScheduleGeneratorViewModel : INotifyPropertyChanged
    {
        public class SelectableStudent : INotifyPropertyChanged
        {
            public int StudentID { get; set; }
            public string StudentName { get; set; }
            private bool _isSelected;
            public bool IsSelected
            {
                get { return _isSelected; }
                set
                {
                    if (_isSelected == value) return;
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        // single-date option removed; only StartDate/EndDate used
        private DateTime _startDate;
        private DateTime _endDate;
        private ObservableCollection<string> _debtStatuses;
        private string _selectedStatus;
        private ObservableCollection<SelectableStudent> _studentsList;
        private string _searchQuery;

        // removed: UseSingleDate, UseDateRange, SingleDate

        public DateTime StartDate
        {
            get { return _startDate; }
            set { _startDate = value; OnPropertyChanged(nameof(StartDate)); }
        }

        public DateTime EndDate
        {
            get { return _endDate; }
            set { _endDate = value; OnPropertyChanged(nameof(EndDate)); }
        }

        public ObservableCollection<string> DebtStatuses
        {
            get { return _debtStatuses; }
            set { _debtStatuses = value; OnPropertyChanged(nameof(DebtStatuses)); }
        }

        public ObservableCollection<SelectableStudent> Students
        {
            get { return _studentsList; }
            set { _studentsList = value; OnPropertyChanged(nameof(Students)); }
        }

        private ObservableCollection<SelectableStudent> _filteredStudents;
        public ObservableCollection<SelectableStudent> FilteredStudents
        {
            get { return _filteredStudents; }
            set { _filteredStudents = value; OnPropertyChanged(nameof(FilteredStudents)); }
        }

        public string SearchQuery
        {
            get { return _searchQuery; }
            set { _searchQuery = value; OnPropertyChanged(nameof(SearchQuery)); ApplyFilter(); }
        }

        public ICommand SelectAllCommand { get; private set; }
        public ICommand ClearAllCommand { get; private set; }

        public string SelectedStatus
        {
            get { return _selectedStatus; }
            set { _selectedStatus = value; OnPropertyChanged(nameof(SelectedStatus)); }
        }

        public ICommand GenerateScheduleCommand { get; }

        public ScheduleGeneratorViewModel()
        {
            var today = DateTime.Today;
            StartDate = today;
            EndDate = today.AddDays(14);

            DebtStatuses = new ObservableCollection<string>
            {
                "Активна",
                "Перенесена",
                "Выполнена"
            };
            SelectedStatus = "Активна";

            // Загружаем студентов, у которых есть активные задолженности
            try
            {
                var students = (from d in OpenContext.db.Debts
                                join s in OpenContext.db.Students on d.StudentID equals s.StudentID
                                where d.DebtStatus == "Активна"
                                select new { s.StudentID, Name = s.LastName + " " + s.FirstName + " " + s.MiddleName })
                               .Distinct()
                               .OrderBy(x => x.Name)
                               .ToList();

                Students = new ObservableCollection<SelectableStudent>(students.Select(s => new SelectableStudent
                {
                    StudentID = s.StudentID,
                    StudentName = s.Name,
                    IsSelected = false
                }));
                FilteredStudents = new ObservableCollection<SelectableStudent>(Students);
            }
            catch
            {
                Students = new ObservableCollection<SelectableStudent>();
                FilteredStudents = new ObservableCollection<SelectableStudent>();
            }

            // commands for selection and paging
            SelectAllCommand = new DelegateCommand(_ =>
            {
                foreach (var s in FilteredStudents)
                    s.IsSelected = true;
            });

            ClearAllCommand = new DelegateCommand(_ =>
            {
                foreach (var s in Students)
                    s.IsSelected = false;
            });

            GenerateScheduleCommand = new DelegateCommand(_ => OnGenerateSchedule(), _ => CanGenerateSchedule());
        }

        private void ApplyFilter()
        {
            var filtered = new System.Collections.Generic.List<SelectableStudent>();
            if (Students != null)
            {
                if (string.IsNullOrWhiteSpace(SearchQuery))
                {
                    filtered = Students.ToList();
                }
                else
                {
                    var q = SearchQuery.ToLower();
                    filtered = Students.Where(s => s.StudentName != null && s.StudentName.ToLower().Contains(q)).ToList();
                }
            }

            // update filtered list
            FilteredStudents = new ObservableCollection<SelectableStudent>(filtered);
        }

        // pagination removed; filtering uses FilteredStudents directly

        public DateTime GetScheduleStartDate()
        {
            return StartDate;
        }

        public DateTime GetScheduleEndDate()
        {
            return EndDate;
        }

        public bool IsDateRangeValid()
        {
            return StartDate <= EndDate && StartDate >= DateTime.Today;
        }

        public System.Collections.Generic.List<int> GetSelectedStudentIds()
        {
            if (Students == null) return new System.Collections.Generic.List<int>();
            return Students.Where(s => s.IsSelected).Select(s => s.StudentID).ToList();
        }

        private bool CanGenerateSchedule()
        {
            return IsDateRangeValid();
        }

        private void OnGenerateSchedule()
        {
            if (!IsDateRangeValid())
            {
                System.Windows.MessageBox.Show("Пожалуйста, проверьте корректность дат.", "Ошибка");
                return;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
