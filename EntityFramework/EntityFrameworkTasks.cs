using DatabaseCourseExam.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DatabaseCourseExam.EntityFramework;

//задание 4
public static class EntityFrameworkTasks
{
    public static async Task ShowStudentsAsync(int minAge = 0)
    {
        await using var context = SchoolDbContextFactory.Create();

        List<Student> students = await context.Students
            .AsNoTracking()
            .Include(student => student.Group)
            .Where(student => student.Age >= minAge)
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ToListAsync();

        foreach (Student student in students)
        {
            Console.WriteLine(
                $"{student.Id}. {student.FirstName} {student.LastName}, " +
                $"возраст: {student.Age}, группа: {student.Group?.Name ?? "—"}");
        }
    }

    //задние7
    public static async Task UpdateStudentAgeAsync(
        int studentId,
        int newAge)
    {
        if (newAge is < 14 or > 100)
        {
            Console.WriteLine("Возраст должен быть от 14 до 100.");
            return;
        }

        await using var context = SchoolDbContextFactory.Create();

        Student? student = await context.Students
            .FirstOrDefaultAsync(x => x.Id == studentId);

        if (student is null)
        {
            Console.WriteLine("Студент не найден.");
            return;
        }

        student.Age = newAge;

        try
        {
            int affectedRows = await context.SaveChangesAsync();

            Console.WriteLine(
                $"Возраст изменён. Сохранено изменений: {affectedRows}");
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            Console.WriteLine(
                "Нарушено ограничение уникальности: такое значение уже есть.");
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine(
                $"Не удалось сохранить изменения: " +
                $"{ex.InnerException?.Message ?? ex.Message}");
        }
    }

    //задние 9
    public static async Task ShowCoursesAsync()
    {
        await using var context = SchoolDbContextFactory.Create();

        var courses = await context.Courses
            .OrderBy(course => course.Name)
            .Select(course => new
            {
                course.Name,
                course.Price,
                course.AvailablePlaces,
                NoPlaces = course.AvailablePlaces <= 0
            })
            .ToListAsync();

        foreach (var course in courses)
        {
            Console.WriteLine(
                $"{course.Name} | Цена: {course.Price:N2} | " +
                $"Свободных мест: {course.AvailablePlaces}" +
                (course.NoPlaces ? " (мест нет)" : string.Empty));
        }
    }
}