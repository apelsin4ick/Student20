using DatabaseCourseExam.Data;
using Dapper;
using DatabaseCourseExam.DapperTasks.DTO;

//задание 6
namespace DatabaseCourseExam.DapperTasks;
public static class DapperStudentTasks
{
    public static async Task ShowStudentsAsync(string? groupName = null)
    {
        await using var connection = DatabaseConnection.Create();

        const string sql = """
            SELECT
                s.id AS Id,
                s.first_name AS FirstName,
                s.last_name AS LastName,
                s.age AS Age,
                g.name AS GroupName
            FROM students AS s
            JOIN student_groups AS g ON g.id = s.group_id
            WHERE (@GroupName::text IS NULL OR g.name = @GroupName)
            ORDER BY s.id;
            """;

        var students = (await connection.QueryAsync<StudentDto>(
            sql,
            new { GroupName = groupName })).ToList();

        if (students.Count == 0)
        {
            Console.WriteLine("Студенты не найдены.");
            return;
        }

        foreach (StudentDto student in students)
        {
            Console.WriteLine(
                $"{student.Id}. {student.FirstName} {student.LastName}, " +
                $"возраст: {student.Age}, группа: {student.GroupName}");
        }
    }

    //11
    public static async Task UpdateStudentAgeAsync(
        int studentId,
        int newAge)
    {
        if (newAge is < 14 or > 100)
        {
            Console.WriteLine("Возраст должен быть от 14 до 100.");
            return;
        }

        await using var connection = DatabaseConnection.Create();

        const string sql = """
            UPDATE students
            SET age = @NewAge
            WHERE id = @StudentId;
            """;

        int affectedRows = await connection.ExecuteAsync(
            sql,
            new
            {
                NewAge = newAge,
                StudentId = studentId
            });

        Console.WriteLine(
            affectedRows == 0
                ? "Студент не найден."
                : $"Изменено записей: {affectedRows}");
    }

    //12
    public static async Task ShowCoursesAsync()
    {
        await using var connection = DatabaseConnection.Create();

        const string sql = """
            SELECT
                id AS Id,
                name AS Name,
                price AS Price,
                available_places AS AvailablePlaces
            FROM courses
            ORDER BY name;
            """;

        var courses = await connection.QueryAsync<CourseDto>(sql);

        foreach (CourseDto course in courses)
        {
            Console.WriteLine(
                $"{course.Name} | Цена: {course.Price:N2} | " +
                $"Свободных мест: {course.AvailablePlaces}");
        }

        await ShowEnrollmentReportAsync(connection);
    }

    private static async Task ShowEnrollmentReportAsync(
        System.Data.Common.DbConnection connection)
    {
        const string sql = """
            SELECT
                c.id AS Id,
                c.name AS Name,
                COUNT(sc.id) AS EnrolledCount
            FROM courses AS c
            LEFT JOIN student_courses AS sc ON sc.course_id = c.id
            GROUP BY c.id, c.name
            ORDER BY c.name;
            """;

        var report = await connection.QueryAsync<CourseEnrollmentReport>(sql);

        Console.WriteLine();
        Console.WriteLine("Отчёт: записано студентов по курсам");

        foreach (CourseEnrollmentReport row in report)
        {
            Console.WriteLine($"{row.Name}: {row.EnrolledCount}");
        }
    }

    private sealed class CourseEnrollmentReport
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long EnrolledCount { get; set; }
    }

    //задние 2
    public static async Task EnrollStudentAsync(
        int studentId,
        int courseId)
    {
        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            bool studentExists = await connection.QuerySingleAsync<bool>(
                "SELECT EXISTS (SELECT 1 FROM students WHERE id = @StudentId);",
                new { StudentId = studentId },
                transaction);

            if (!studentExists)
            {
                throw new InvalidOperationException("Студент не найден.");
            }

            bool alreadyEnrolled = await connection.QuerySingleAsync<bool>(
                """
                SELECT EXISTS (
                    SELECT 1 FROM student_courses
                    WHERE student_id = @StudentId AND course_id = @CourseId);
                """,
                new { StudentId = studentId, CourseId = courseId },
                transaction);

            if (alreadyEnrolled)
            {
                throw new InvalidOperationException(
                    "Студент уже записан на этот курс.");
            }

            const string updateSql = """
                UPDATE courses
                SET available_places = available_places - 1
                WHERE id = @CourseId
                  AND available_places > 0
                RETURNING available_places;
                """;

            int? remainingPlaces =
                await connection.QuerySingleOrDefaultAsync<int?>(
                    updateSql,
                    new { CourseId = courseId },
                    transaction);

            if (remainingPlaces is null)
            {
                throw new InvalidOperationException(
                    "Курс не найден или свободных мест нет.");
            }

            const string insertSql = """
                INSERT INTO student_courses (student_id, course_id)
                VALUES (@StudentId, @CourseId);
                """;

            await connection.ExecuteAsync(
                insertSql,
                new
                {
                    StudentId = studentId,
                    CourseId = courseId
                },
                transaction);

            await transaction.CommitAsync();

            Console.WriteLine(
                $"Студент записан на курс. Осталось мест: {remainingPlaces}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Операция отменена: {ex.Message}");
        }
    }
}