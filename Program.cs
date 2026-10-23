using DatabaseCourseExam.AdoNet;
using System.Data;
using DatabaseCourseExam.EntityFramework;
using DatabaseCourseExam.DapperTasks;

DataTable? studentsTable = null;

//здание 1
while (true)
{
    Console.WriteLine();
    Console.WriteLine("Зачет: ");
    Console.WriteLine("1. Показать студентов");
    Console.WriteLine("2. Изменить возраст студента");
    Console.WriteLine("3. Записать студента на курс");
    Console.WriteLine("4. Загрузить студентов в DataTable");
    Console.WriteLine("5. Изменить возраст в DataTable");
    Console.WriteLine("6. Показать DataTable");
    Console.WriteLine("7. Сохранить изменения DataTable");
    Console.WriteLine("8. EF Core: показать студентов");
    Console.WriteLine("9. EF Core: изменить возраст студента");
    Console.WriteLine("10. EF Core: показать курсы");
    Console.WriteLine("11. Dapper: показать студентов");
    Console.WriteLine("12. Dapper: изменить возраст");
    Console.WriteLine("13. Dapper: показать курсы");
    Console.WriteLine("14. Dapper: записать студента на курс");
    Console.WriteLine("15. Сравнение: студенты через ADO.NET, EF Core и Dapper");
    Console.WriteLine("16. Dapper: студенты выбранной группы");
    Console.WriteLine("17. EF Core: студенты от минимального возраста");
    Console.WriteLine("0. Выход");
    Console.Write("Выберите действие: ");

    string? choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "1":
                await AdoNetTasks.ShowStudentsAsync();
                break;

            case "2":
                Console.Write("ID студента: ");
                int studentId = int.Parse(Console.ReadLine()!);

                Console.Write("Новый возраст: ");
                int age = int.Parse(Console.ReadLine()!);

                await AdoNetTasks.UpdateStudentAgeAsync(studentId, age);
                break;

            case "3":
                Console.Write("ID студента: ");
                int enrollmentStudentId = int.Parse(Console.ReadLine()!);

                Console.Write("ID курса: ");
                int courseId = int.Parse(Console.ReadLine()!);

                await AdoNetTasks.EnrollStudentAsync(
                    enrollmentStudentId,
                    courseId);
                break;

            case "4":
                studentsTable = await DisconnectedTasks.LoadStudentsAsync();
                Console.WriteLine("Данные загружены в память.");
                DisconnectedTasks.PrintTable(studentsTable);
                break;

            case "5":
                if (studentsTable is null)
                {
                    Console.WriteLine("Сначала загрузите данные (пункт 4).");
                    break;
                }

                Console.Write("ID студента: ");
                int disconnectedStudentId = int.Parse(Console.ReadLine()!);

                Console.Write("Новый возраст: ");
                int disconnectedAge = int.Parse(Console.ReadLine()!);

                if (disconnectedAge is < 14 or > 100)
                {
                    Console.WriteLine("Возраст должен быть от 14 до 100.");
                    break;
                }

                DisconnectedTasks.ChangeAgeInMemory(
                    studentsTable,
                    disconnectedStudentId,
                    disconnectedAge);
                break;

            case "6":
                if (studentsTable is null)
                {
                    Console.WriteLine("Сначала загрузите данные (пункт 4).");
                    break;
                }

                DisconnectedTasks.PrintTable(studentsTable);
                break;

            case "7":
                if (studentsTable is null)
                {
                    Console.WriteLine("Сначала загрузите данные (пункт 4).");
                    break;
                }

                DisconnectedTasks.SaveChanges(studentsTable);
                break;

            case "8":
                await EntityFrameworkTasks.ShowStudentsAsync();
                break;

            case "9":
                {
                    Console.Write("ID студента: ");

                    if (!int.TryParse(Console.ReadLine(), out int stId))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("Новый возраст: ");

                    if (!int.TryParse(Console.ReadLine(), out int newAge)
                        || newAge is < 14 or > 100)
                    {
                        Console.WriteLine("Возраст должен быть от 14 до 100.");
                        break;
                    }

                    await EntityFrameworkTasks.UpdateStudentAgeAsync(
                        stId,
                        newAge);

                    break;
                }

            case "10":
                await EntityFrameworkTasks.ShowCoursesAsync();
                break;

            case "11":
                await DapperStudentTasks.ShowStudentsAsync();
                break;

            case "12":
                {
                    Console.Write("ID студента: ");

                    if (!int.TryParse(Console.ReadLine(), out int dStudentId))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("Новый возраст: ");

                    if (!int.TryParse(Console.ReadLine(), out int newAge)
                        || newAge is < 14 or > 100)
                    {
                        Console.WriteLine("Возраст должен быть от 14 до 100.");
                        break;
                    }

                    await DapperStudentTasks.UpdateStudentAgeAsync(
                        dStudentId,
                        newAge);

                    break;
                }

            case "13":
                await DapperStudentTasks.ShowCoursesAsync();
                break;

            case "14":
                {
                    Console.Write("ID студента: ");

                    if (!int.TryParse(Console.ReadLine(), out int dStudentId))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    Console.Write("ID курса: ");

                    if (!int.TryParse(Console.ReadLine(), out int dCourseId))
                    {
                        Console.WriteLine("Некорректный ID.");
                        break;
                    }

                    await DapperStudentTasks.EnrollStudentAsync(
                        dStudentId,
                        dCourseId);

                    break;
                }
            case "15":
                Console.WriteLine("ADO.NET");
                await AdoNetTasks.ShowStudentsAsync();

                Console.WriteLine("EF Core");
                await EntityFrameworkTasks.ShowStudentsAsync();

                Console.WriteLine("Dapper");
                await DapperStudentTasks.ShowStudentsAsync();
                break;

            case "16":
                Console.Write("Название группы (пусто - все): ");
                string? groupName = Console.ReadLine();

                await DapperStudentTasks.ShowStudentsAsync(
                    string.IsNullOrWhiteSpace(groupName)
                        ? null
                        : groupName.Trim());
                break;

            case "17":
                {
                    Console.Write("Минимальный возраст: ");

                    if (!int.TryParse(Console.ReadLine(), out int minAge))
                    {
                        Console.WriteLine("Некорректный возраст.");
                        break;
                    }

                    await EntityFrameworkTasks.ShowStudentsAsync(minAge);
                    break;
                }

            case "0":
                return;

            default:
                Console.WriteLine("Неизвестная команда.");
                break;
        }
    }
    catch (FormatException)
    {
        Console.WriteLine("Введите целое число.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}