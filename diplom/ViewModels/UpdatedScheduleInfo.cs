using System;

namespace diplom.ViewModels
{
    public class UpdatedScheduleInfo
    {
        public int DebtID { get; set; }
        public string StudentCardNumber { get; set; }
        public string StudentName { get; set; }
        public string GroupName { get; set; }
        public string DisciplineName { get; set; }
        public string TeacherName { get; set; }
        public string Status { get; set; }
        public DateTime? ExamDate { get; set; }
        public int? TimeSlotNumber { get; set; }
        public string TimeSlotDescription { get; set; }
        public string ClassroomNumber { get; set; }
        public bool IsNew { get; set; }
    }
}
