using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace diplom.Models
{
    /// <summary>
    /// Вспомогательный класс для представления гена (одной ячейки расписания пересдачи)
    /// </summary>
    public class AppointmentGene
    {
        public int DebtID { get; set; }
        public int StudentID { get; set; }
        public int TeacherID { get; set; }

        // Изменяемые параметры (хромосомный набор)
        public DateTime ExamDate { get; set; }
        // Номер пары (для совместимости) и FK на справочник TimeSlots
        public int TimeSlot { get; set; }
        public int TimeSlotID { get; set; }
        public int ClassroomID { get; set; }
    }

    /// <summary>
    /// Вспомогательный класс для представления хромосомы (полного варианта расписания)
    /// </summary>
    public class ScheduleChromosome
    {
        public List<AppointmentGene> Genes { get; set; } = new List<AppointmentGene>();
        public int FitnessScore { get; set; } // Штрафной балл (0 - идеальное расписание)
        // Список детализированных сообщений о найденных конфликтах (для отладки/отчёта)
        public List<string> ConflictDetails { get; set; } = new List<string>();
    }

    /// <summary>
    /// Класс ядра интеллектуального генетического алгоритма
    /// </summary>
    public class GeneticScheduler
    {
        private List<Debt> _activeDebts;
        private List<Classroom> _classrooms;
        private List<TimeSlot> _timeSlots;
        private List<DateTime> _availableDates;
        private Random _rand = new Random();

        public GeneticScheduler()
        {
            // 1. Загружаем из контекста только активные задолженности
            _activeDebts = OpenContext.db.Debts
                .Where(d => d.DebtStatus == "Активна")
                .ToList();

            // 2. Загружаем доступные аудитории
            _classrooms = OpenContext.db.Classrooms.ToList();

            // 2.1 Загружаем справочник пар
            _timeSlots = OpenContext.db.TimeSlots.OrderBy(ts => ts.SlotNumber).ToList();

            // 3. Формируем пул доступных дат (ближайшие 14 дней, исключая воскресенья)
            _availableDates = new List<DateTime>();
            for (int i = 1; i <= 14; i++)
            {
                DateTime date = DateTime.Today.AddDays(i);
                if (date.DayOfWeek != DayOfWeek.Sunday)
                {
                    _availableDates.Add(date);
                }
            }
        }

        /// <summary>
        /// Конструктор с указанием периода и опциональным списком студентов (оставляет только их долги)
        /// </summary>
        public GeneticScheduler(DateTime startDate, DateTime endDate, System.Collections.Generic.List<int> studentIds = null)
        {
            // 1. Загружаем из контекста только активные задолженности
            _activeDebts = OpenContext.db.Debts
                .Where(d => d.DebtStatus == "Активна")
                .ToList();

            if (studentIds != null && studentIds.Count > 0)
            {
                _activeDebts = _activeDebts.Where(d => studentIds.Contains(d.StudentID)).ToList();
            }

            // 2. Загружаем доступные аудитории
            _classrooms = OpenContext.db.Classrooms.ToList();

            // 2.1 Загружаем справочник пар
            _timeSlots = OpenContext.db.TimeSlots.OrderBy(ts => ts.SlotNumber).ToList();

            // 3. Формируем пул доступных дат по переданному диапазону, исключая воскресенья
            _availableDates = new List<DateTime>();
            DateTime cur = startDate.Date;
            while (cur <= endDate.Date)
            {
                if (cur.DayOfWeek != DayOfWeek.Sunday)
                    _availableDates.Add(cur);
                cur = cur.AddDays(1);
            }

            // Если в результате не осталось дат (например, диапазон был только воскресенье), добавим ближайший валидный
            if (_availableDates.Count == 0)
            {
                DateTime fallback = startDate.Date;
                if (fallback.DayOfWeek == DayOfWeek.Sunday)
                    fallback = fallback.AddDays(1);
                _availableDates.Add(fallback);
            }
        }

        /// <summary>
        /// Главный метод запуска эволюционного поиска оптимального расписания
        /// </summary>
        public ScheduleChromosome RunEvolution()
        {
            if (_activeDebts.Count == 0 || _classrooms.Count == 0 || _availableDates.Count == 0)
                return null;

            int populationSize = 60;   // Размер популяции вариантов
            int maxGenerations = 300;  // Максимальное количество шагов эволюции
            double mutationRate = 0.2; // Вероятность мутации гена (20%)

            List<ScheduleChromosome> population = new List<ScheduleChromosome>();

            // Шаг 1: Инициализация начального поколения (случайное распределение)
            for (int i = 0; i < populationSize; i++)
            {
                population.Add(GenerateRandomChromosome());
            }

            // Шаг 2: Эволюционный цикл
            for (int generation = 0; generation < maxGenerations; generation++)
            {
                // Оценка приспособленности каждой хромосомы
                foreach (var chromosome in population)
                {
                    EvaluateFitness(chromosome);
                }

                // Сортировка популяции по возрастанию штрафов (лучшие — вверху)
                population = population.OrderBy(c => c.FitnessScore).ToList();

                // Если найдено идеальное бесконфликтное решение — досрочно завершаем
                if (population[0].FitnessScore == 0)
                    break;

                // Селекция: отбираем 50% лучших решений (выживание сильнейших)
                int eliteCount = populationSize / 2;
                List<ScheduleChromosome> survivors = population.Take(eliteCount).ToList();
                List<ScheduleChromosome> nextGeneration = new List<ScheduleChromosome>(survivors);

                // Репродукция: скрещивание и мутация для заполнения популяции
                while (nextGeneration.Count < populationSize)
                {
                    // Выбор двух случайных родителей из числа выживших
                    ScheduleChromosome parentA = survivors[_rand.Next(eliteCount)];
                    ScheduleChromosome parentB = survivors[_rand.Next(eliteCount)];

                    // Кроссинговер (скрещивание)
                    ScheduleChromosome child = Crossover(parentA, parentB);

                    // Мутация
                    Mutate(child, mutationRate);

                    nextGeneration.Add(child);
                }

                population = nextGeneration;
            }

            // Финальная оценка и возврат абсолютного победителя эволюции
            foreach (var chromosome in population)
            {
                EvaluateFitness(chromosome);
            }
            return population.OrderBy(c => c.FitnessScore).First();
        }

        /// <summary>
        /// Генерация случайной хромосомы для стартовой популяции
        /// </summary>
        private ScheduleChromosome GenerateRandomChromosome()
        {
            var chromosome = new ScheduleChromosome();
            // Пытаемся присвоить каждому долгу незанятый слот (жадно), чтобы минимизировать накладки
            foreach (var debt in _activeDebts)
            {
                // Выбираем единый таймслот и аудиторию, чтобы TimeSlot и TimeSlotID были согласованы
                var tsInitial = _timeSlots.Count > 0 ? _timeSlots[_rand.Next(_timeSlots.Count)] : null;
                var roomInitial = _classrooms[_rand.Next(_classrooms.Count)];

                var gene = new AppointmentGene
                {
                    DebtID = debt.DebtID,
                    StudentID = debt.StudentID,
                    TeacherID = debt.TeacherID,
                    // временно случайные значения
                    ExamDate = _availableDates[_rand.Next(_availableDates.Count)],
                    TimeSlot = tsInitial != null ? tsInitial.SlotNumber : _rand.Next(1, 5),
                    TimeSlotID = tsInitial != null ? tsInitial.TimeSlotID : 0,
                    ClassroomID = roomInitial.ClassroomID
                };

                // Попытка найти безконфликтный слот относительно уже назначенных генов в этой хромосоме
                if (!TryFindNonConflictingSlot(gene, chromosome.Genes, out DateTime date, out int timeSlot, out int classroomId, out int timeSlotId))
                {
                    // если не нашли, используем первоначальные случайные значения
                }
                else
                {
                    gene.ExamDate = date;
                    gene.TimeSlot = timeSlot;
                    gene.TimeSlotID = timeSlotId;
                    gene.ClassroomID = classroomId;
                }

                chromosome.Genes.Add(gene);
            }

            // Ремонтируем возможные конфликты, которые могли возникнуть (на случай полного пула)
            RepairChromosome(chromosome);
            return chromosome;
        }

        /// <summary>
        /// Расчет функции приспособленности (Fitness Function) с системой штрафов
        /// </summary>
        private void EvaluateFitness(ScheduleChromosome chromosome)
        {
            int penalty = 0;
            int geneCount = chromosome.Genes.Count;
            chromosome.ConflictDetails.Clear();

            for (int i = 0; i < geneCount; i++)
            {
                var g1 = chromosome.Genes[i];

                for (int j = i + 1; j < geneCount; j++)
                {
                    var g2 = chromosome.Genes[j];

                    // Проверка на пересечение по времени (один и тот же день и та же пара)
                    bool sameDay = g1.ExamDate.Date == g2.ExamDate.Date;
                    bool sameSlotExact = (g1.TimeSlotID != 0 && g2.TimeSlotID != 0 && g1.TimeSlotID == g2.TimeSlotID);
                    bool timeOverlap = false;
                    if (sameDay && g1.TimeSlotID != 0 && g2.TimeSlotID != 0)
                    {
                        var ts1 = _timeSlots.FirstOrDefault(x => x.TimeSlotID == g1.TimeSlotID);
                        var ts2 = _timeSlots.FirstOrDefault(x => x.TimeSlotID == g2.TimeSlotID);
                        if (ts1 != null && ts2 != null)
                        {
                            TimeSpan s1 = ts1.StartTime; TimeSpan e1 = ts1.EndTime;
                            TimeSpan s2 = ts2.StartTime; TimeSpan e2 = ts2.EndTime;
                            timeOverlap = !(e1 <= s2 || e2 <= s1);
                        }
                    }

                    if (sameDay && (sameSlotExact || timeOverlap))
                    {
                        // 1. Критический конфликт: Преподаватель назначен в две разные аудитории
                        if (g1.TeacherID == g2.TeacherID && g1.ClassroomID != g2.ClassroomID)
                        {
                            // Абсолютный запрет — огромный штраф
                            penalty += 100000;
                            chromosome.ConflictDetails.Add($"Teacher conflict: TeacherID={g1.TeacherID} debts={g1.DebtID},{g2.DebtID} date={g1.ExamDate:yyyy-MM-dd} slots={g1.TimeSlot}/{g2.TimeSlot} rooms={g1.ClassroomID}/{g2.ClassroomID}");
                        }

                        // 2. Критический конфликт: Аудитория занята разными преподавателями/предметами
                        if (g1.ClassroomID == g2.ClassroomID && g1.TeacherID != g2.TeacherID)
                        {
                            penalty += 100000;
                            chromosome.ConflictDetails.Add($"Classroom conflict: ClassroomID={g1.ClassroomID} debts={g1.DebtID},{g2.DebtID} date={g1.ExamDate:yyyy-MM-dd} slots={g1.TimeSlot}/{g2.TimeSlot} teachers={g1.TeacherID}/{g2.TeacherID}");
                        }

                        // 3. Конфликт: Студент должен сдавать два экзамена одновременно
                        if (g1.StudentID == g2.StudentID)
                        {
                            penalty += 100000;
                            chromosome.ConflictDetails.Add($"Student conflict: StudentID={g1.StudentID} debts={g1.DebtID},{g2.DebtID} date={g1.ExamDate:yyyy-MM-dd} slots={g1.TimeSlot}/{g2.TimeSlot}");
                        }
                    }

                    // 4. Мягкое ограничение: У студента не должно быть больше одной пересдачи в один день
                    if (g1.ExamDate.Date == g2.ExamDate.Date && g1.StudentID == g2.StudentID)
                    {
                        penalty += 20; // повышаем вес мягкого штрафа
                        chromosome.ConflictDetails.Add($"Soft same-day student: StudentID={g1.StudentID} debts={g1.DebtID},{g2.DebtID} date={g1.ExamDate:yyyy-MM-dd}");
                    }
                }
            }

            chromosome.FitnessScore = penalty;
        }

        /// <summary>
        /// Оператор одноточечного скрещивания (Crossover)
        /// </summary>
        private ScheduleChromosome Crossover(ScheduleChromosome parentA, ScheduleChromosome parentB)
        {
            var child = new ScheduleChromosome();
            int crossoverPoint = _rand.Next(1, parentA.Genes.Count);

            for (int i = 0; i < parentA.Genes.Count; i++)
            {
                if (i < crossoverPoint)
                {
                    child.Genes.Add(new AppointmentGene
                    {
                        DebtID = parentA.Genes[i].DebtID,
                        StudentID = parentA.Genes[i].StudentID,
                        TeacherID = parentA.Genes[i].TeacherID,
                        ExamDate = parentA.Genes[i].ExamDate,
                        TimeSlot = parentA.Genes[i].TimeSlot,
                        TimeSlotID = parentA.Genes[i].TimeSlotID,
                        ClassroomID = parentA.Genes[i].ClassroomID
                    });
                }
                else
                {
                    child.Genes.Add(new AppointmentGene
                    {
                        DebtID = parentB.Genes[i].DebtID,
                        StudentID = parentB.Genes[i].StudentID,
                        TeacherID = parentB.Genes[i].TeacherID,
                        ExamDate = parentB.Genes[i].ExamDate,
                        TimeSlot = parentB.Genes[i].TimeSlot,
                        TimeSlotID = parentB.Genes[i].TimeSlotID,
                        ClassroomID = parentB.Genes[i].ClassroomID
                    });
                }
            }
            return child;
        }

        /// <summary>
        /// Оператор мутации генов (случайное изменение параметров)
        /// </summary>
        private void Mutate(ScheduleChromosome chromosome, double mutationRate)
        {
            foreach (var gene in chromosome.Genes)
            {
                if (_rand.NextDouble() < mutationRate)
                {
                    // Случайным образом меняем дату, пару и аудиторию для гена
                    var d = _availableDates[_rand.Next(_availableDates.Count)];
                    var tsRec = _timeSlots[_rand.Next(_timeSlots.Count)];
                    var room = _classrooms[_rand.Next(_classrooms.Count)];
                    gene.ExamDate = d;
                    gene.TimeSlot = tsRec.SlotNumber;
                    gene.TimeSlotID = tsRec.TimeSlotID;
                    gene.ClassroomID = room.ClassroomID;
                }
            }

            // После мутаций пробуем устранить новые конфликты
            RepairChromosome(chromosome);
        }

        /// <summary>
        /// Пытается найти слот (дата, пара, аудитория) для гена, не конфликтующий с уже назначенными генами.
        /// </summary>
        private bool TryFindNonConflictingSlot(AppointmentGene gene, List<AppointmentGene> existingGenes, out DateTime date, out int timeSlot, out int classroomId, out int timeSlotId)
        {
            foreach (var d in _availableDates)
            {
                // перебираем таймслоты из справочника
                foreach (var tsRec in _timeSlots)
                {
                    foreach (var room in _classrooms)
                    {
                        bool conflict = false;
                        foreach (var g in existingGenes)
                        {
                            if (g.ExamDate.Date != d.Date)
                                continue;

                            // Для каждого уже назначенного гена пытаемся получить его временной интервал
                            TimeSpan startA = TimeSpan.Zero, endA = TimeSpan.Zero;
                            if (g.TimeSlotID != 0)
                            {
                                var tsOther = _timeSlots.FirstOrDefault(x => x.TimeSlotID == g.TimeSlotID);
                                if (tsOther != null)
                                {
                                    startA = tsOther.StartTime;
                                    endA = tsOther.EndTime;
                                }
                            }
                            else
                            {
                                // fallback: ищем по номеру пары
                                var tsOther = _timeSlots.FirstOrDefault(x => x.SlotNumber == g.TimeSlot);
                                if (tsOther != null)
                                {
                                    startA = tsOther.StartTime;
                                    endA = tsOther.EndTime;
                                }
                            }

                            // Информация о кандидате
                            TimeSpan startB = tsRec.StartTime;
                            TimeSpan endB = tsRec.EndTime;

                            bool timeOverlap = true;
                            // Если мы не смогли получить интервалы для существующего гена, используем грубую проверку по номеру пары
                            if (startA == TimeSpan.Zero && endA == TimeSpan.Zero)
                            {
                                // сравниваем номер пары (если он задан)
                                if (g.TimeSlot != 0 && tsRec.SlotNumber != 0)
                                    timeOverlap = g.TimeSlot == tsRec.SlotNumber;
                                else
                                    timeOverlap = true; // pessimistic: считаем, что есть перекрытие
                            }
                            else
                            {
                                timeOverlap = !(endA <= startB || endB <= startA);
                            }

                            if (timeOverlap && ((g.TeacherID == gene.TeacherID && g.ClassroomID != room.ClassroomID)
                                || (g.ClassroomID == room.ClassroomID && g.TeacherID != gene.TeacherID)
                                || (g.StudentID == gene.StudentID)))
                            {
                                conflict = true;
                                break;
                            }
                        }

                        if (!conflict)
                        {
                            date = d;
                            timeSlot = tsRec.SlotNumber;
                            classroomId = room.ClassroomID;
                            timeSlotId = tsRec.TimeSlotID;
                            return true;
                        }
                    }
                }
            }

            date = default(DateTime);
            timeSlot = 0;
            classroomId = 0;
            timeSlotId = 0;
            return false;
        }

        /// <summary>
        /// Пробегаем по генам и пытаемся устранить конфликты, переназначая проблемные гены в свободные слоты.
        /// </summary>
        private void RepairChromosome(ScheduleChromosome chromosome)
        {
            for (int i = 0; i < chromosome.Genes.Count; i++)
            {
                var gene = chromosome.Genes[i];

                bool hasConflict = false;
                for (int j = 0; j < chromosome.Genes.Count; j++)
                {
                    if (i == j) continue;
                    var other = chromosome.Genes[j];
                    if (other.ExamDate.Date == gene.ExamDate.Date && other.TimeSlot == gene.TimeSlot)
                    {
                        if ((other.TeacherID == gene.TeacherID && other.ClassroomID != gene.ClassroomID)
                            || (other.ClassroomID == gene.ClassroomID && other.TeacherID != gene.TeacherID)
                            || (other.StudentID == gene.StudentID))
                        {
                            hasConflict = true;
                            break;
                        }
                    }
                }

                if (hasConflict)
                {
                    var otherGenes = chromosome.Genes.Where(g => g.DebtID != gene.DebtID).ToList();
                    if (TryFindNonConflictingSlot(gene, otherGenes, out DateTime date, out int ts, out int roomId, out int tsId))
                    {
                        gene.ExamDate = date;
                        gene.TimeSlot = ts;
                        gene.TimeSlotID = tsId;
                        gene.ClassroomID = roomId;
                    }
                }
            }
        }
    }
}
