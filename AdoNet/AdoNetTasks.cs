
using DatabaseCourseExam.Data;
using Npgsql;
using NpgsqlTypes;

namespace DatabaseCourseExam.AdoNet;

public static class AdoNetTasks
{
    public static async Task ShowStudentsAsync()
    {
        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        const string sql = """
            SELECT s.id, s.first_name, s.last_name, s.age, g.name
            FROM students AS s
            JOIN student_groups AS g ON g.id = s.group_id
            ORDER BY s.id;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            Console.WriteLine(
                $"{reader.GetInt32(0)}. " +
                $"{reader.GetString(1)} {reader.GetString(2)}, " +
                $"возраст: {reader.GetInt32(3)}, " +
                $"группа: {reader.GetString(4)}");
        }
    }

    //задание 3
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
        await connection.OpenAsync();

        const string sql = """
            UPDATE students
            SET age = @Age
            WHERE id = @StudentId;
            """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add("@Age", NpgsqlDbType.Integer).Value = newAge;
        command.Parameters.Add("@StudentId", NpgsqlDbType.Integer).Value =
            studentId;

        int affectedRows = await command.ExecuteNonQueryAsync();

        Console.WriteLine(
            affectedRows == 0
                ? "Студент не найден."
                : $"Изменено записей: {affectedRows}");
    }

    public static async Task EnrollStudentAsync(
        int studentId,
        int courseId)
    {
        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            const string updateSql = """
                UPDATE courses
                SET available_places = available_places - 1
                WHERE id = @CourseId
                  AND available_places > 0
                RETURNING available_places;
                """;

            await using var updateCommand =
                new NpgsqlCommand(updateSql, connection, transaction);

            updateCommand.Parameters.Add(
                "@CourseId", NpgsqlDbType.Integer).Value = courseId;

            object? result = await updateCommand.ExecuteScalarAsync();

            if (result is null)
            {
                throw new InvalidOperationException(
                    "Курс не найден или свободных мест нет.");
            }

            const string insertSql = """
                INSERT INTO student_courses (student_id, course_id)
                VALUES (@StudentId, @CourseId);
                """;

            await using var insertCommand =
                new NpgsqlCommand(insertSql, connection, transaction);

            insertCommand.Parameters.Add(
                "@StudentId", NpgsqlDbType.Integer).Value = studentId;

            insertCommand.Parameters.Add(
                "@CourseId", NpgsqlDbType.Integer).Value = courseId;

            await insertCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            Console.WriteLine("Студент записан на курс.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Операция отменена: {ex.Message}");
        }
    }
}