
using System.Data;
using DatabaseCourseExam.Data;
using Npgsql;

namespace DatabaseCourseExam.AdoNet;

//здание 5
public static class DisconnectedTasks
{
    public static async Task<DataTable> LoadStudentsAsync()
    {
        var dataSet = new DataSet("School");
        var table = new DataTable("Students");
        var enrollments = new DataTable("StudentCourses");
        dataSet.Tables.Add(table);
        dataSet.Tables.Add(enrollments);

        await using var connection = DatabaseConnection.Create();
        await connection.OpenAsync();

        const string sql = """
            SELECT id, first_name, last_name, age, group_id
            FROM students
            ORDER BY id;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        using var adapter = new NpgsqlDataAdapter(command);

        adapter.Fill(table);
        table.PrimaryKey = [table.Columns["id"]!];

        const string enrollmentsSql = """
            SELECT id, student_id, course_id, enrolled_at
            FROM student_courses
            ORDER BY id;
            """;

        await using var enrollmentsCommand =
            new NpgsqlCommand(enrollmentsSql, connection);
        using var enrollmentsAdapter =
            new NpgsqlDataAdapter(enrollmentsCommand);

        enrollmentsAdapter.Fill(enrollments);

        dataSet.Relations.Add(new DataRelation(
            "Student_Enrollments",
            table.Columns["id"]!,
            enrollments.Columns["student_id"]!,
            createConstraints: false));

        return table;
    }

    public static void PrintTable(DataTable table)
    {
        bool hasRelation =
            table.DataSet?.Relations.Contains("Student_Enrollments") == true;

        foreach (DataRow row in table.Rows)
        {
            if (row.RowState == DataRowState.Deleted)
            {
                continue;
            }

            string enrollmentsInfo = hasRelation
                ? $", записей на курсы: " +
                  $"{row.GetChildRows("Student_Enrollments").Length}"
                : string.Empty;

            Console.WriteLine(
                $"{row["id"]}: {row["first_name"]} " +
                $"{row["last_name"]}, возраст {row["age"]}" +
                enrollmentsInfo);
        }
    }

    public static void ChangeAgeInMemory(
        DataTable table,
        int studentId,
        int newAge)
    {
        DataRow? row = table.Rows.Find(studentId);

        if (row is null)
        {
            Console.WriteLine("Студент не найден в DataTable.");
            return;
        }

        row["age"] = newAge;

        Console.WriteLine(
            $"Возраст изменён в памяти. Состояние строки: {row.RowState}");
    }

    //10
    public static int SaveChanges(DataTable table)
    {
        using var connection = DatabaseConnection.Create();
        connection.Open();

        const string sql = """
        UPDATE students
        SET age = @age
        WHERE id = @id;
        """;

        using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add("@age", NpgsqlTypes.NpgsqlDbType.Integer,
            4, "age");

        command.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Integer,
            4, "id");

        using var adapter = new NpgsqlDataAdapter();
        adapter.UpdateCommand = command;

        int affectedRows = adapter.Update(table);
        Console.WriteLine($"Сохранено изменений: {affectedRows}");

        int stillModified = table.Select(
            null, null, DataViewRowState.ModifiedCurrent).Length;

        Console.WriteLine(
            stillModified == 0
                ? "Строк в состоянии Modified не осталось."
                : $"Внимание: в состоянии Modified осталось строк: {stillModified}");

        return affectedRows;
    }
}