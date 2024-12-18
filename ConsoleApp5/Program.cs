﻿using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;



namespace ConsoleApp5
{
    internal class Program
    {
        private static DbProviderFactory factory;
        private static string connectionString;
        private static Logger logger = new Logger();

        public static async Task Main(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json")
                .Build();

            string providerName = configuration["ProviderName"];
            string connectionString = configuration.GetConnectionString("FirstConnection");

            await SelectDatabaseAsync(configuration);

            DbProviderFactories.RegisterFactory(providerName, SqlClientFactory.Instance);

            DbProviderFactory factory = DbProviderFactories.GetFactory(providerName);

            List<SchoolStudent> schoolStudents = null;
            List<SchoolStudent> schoolStudentsNew = null;

            using (DbConnection connection = factory.CreateConnection())
            {
                if (connection == null)
                {
                    Console.WriteLine("Неможливо відкрити датабазу.");
                    return;
                }
                connection.ConnectionString = connectionString;
                await connection.OpenAsync();
                Console.WriteLine("Зьеднання встановлено.");

                string query = "SELECT * FROM StudentGrades";
                var stopwatch = Stopwatch.StartNew();
                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandType = CommandType.Text;

                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            schoolStudents = schoolStudents ?? new List<SchoolStudent>();
                            schoolStudents.Add(
                               new SchoolStudent()
                               {
                                   Id = reader.GetInt32(0),
                                   Name = reader.GetString(1) ?? default,
                                   GroupName = reader.GetString(2) ?? default,
                                   AvgGrade = reader.GetDouble(3),
                                   MinSubject = reader.GetString(4) ?? default,
                                   MaxSubject = reader.GetString(5) ?? default

                               });
                        }
                    }
                }
                stopwatch.Stop();
                logger.LogQuery(query, stopwatch.ElapsedMilliseconds);
                Console.WriteLine($"Час виконання запиту: {stopwatch.Elapsed.TotalSeconds} секунд.");
                Console.ReadLine();
                if (schoolStudents is not null)
                {

                    while (true)
                    {
                        Console.Clear();
                        Console.WriteLine("Меню:");
                        Console.WriteLine("1 - Створити нового студента");
                        Console.WriteLine("2 - Показати всіх студентів");
                        Console.WriteLine("3 - Записати у базу данних");
                        Console.WriteLine("4 - Оновити данні");
                        Console.WriteLine("5 - Видалити з бази");
                        Console.WriteLine("6 - Змінити датабазу");
                        Console.WriteLine("7 - Експортувати дані в CSV");
                        Console.WriteLine("8 - Імпортувати дані в CSV");

                        var input = Console.ReadLine();

                        switch (input.ToLower())
                        {
                            case "1":

                                Console.WriteLine("Ім'я студента: ");
                                string NewName = Console.ReadLine();
                                Console.WriteLine("Група студента:");
                                string NewGroup = Console.ReadLine();
                                Console.WriteLine("Середня оцінка студента:");
                                string NewAvgGrade = Console.ReadLine();
                                Console.WriteLine("Найгірший предмет студента:");
                                string NewMinSubject = Console.ReadLine();
                                Console.WriteLine("Найкращий предмет студента:");
                                string NewMaxSubject = Console.ReadLine();

                                var studentNew = new SchoolStudent()
                                {
                                    Id = schoolStudents.Count + 1,
                                    Name = NewName,
                                    GroupName = NewGroup,
                                    AvgGrade = double.Parse(NewAvgGrade),
                                    MinSubject = NewMinSubject,
                                    MaxSubject = NewMaxSubject

                                };

                                schoolStudentsNew = schoolStudentsNew ?? new List<SchoolStudent>();
                                schoolStudents.Add(studentNew);
                                schoolStudentsNew.Add(studentNew);

                                break;

                            case "2":
                                foreach (var student in schoolStudents)
                                {
                                    Console.WriteLine($"Id - {student.Id}, Name - {student.Name}, Group Name - {student.GroupName}, Average Grade - {student.AvgGrade}");
                                }
                                Console.ReadLine();
                                break;

                            case "3":
                                try
                                {

                                    stopwatch = Stopwatch.StartNew();
                                    query = "INSERT INTO StudentGrades (FullName, GroupName, AverageGrade, MinSubject, MaxSubject) VALUES (@fullName, @groupName, @averageGrade, @minSubject, @maxSubject)";
                                    using (DbCommand insertCommand = connection.CreateCommand())
                                    {
                                        foreach (var student in schoolStudentsNew)
                                        {
                                            insertCommand.CommandText = query;
                                            insertCommand.CommandType = CommandType.Text;

                                            var nameParameter = insertCommand.CreateParameter();
                                            nameParameter.ParameterName = "@fullname";
                                            nameParameter.Value = student.Name;

                                            var groupParameter = insertCommand.CreateParameter();
                                            groupParameter.ParameterName = "@groupName";
                                            groupParameter.Value = student.GroupName;

                                            var avgGradeParameter = insertCommand.CreateParameter();
                                            avgGradeParameter.ParameterName = "@averageGrade";
                                            groupParameter.Value = student.AvgGrade;

                                            var minSubjectParameter = insertCommand.CreateParameter();
                                            minSubjectParameter.ParameterName = "@minSubject";
                                            groupParameter.Value = student.MinSubject;

                                            var maxSubjectParameter = insertCommand.CreateParameter();
                                            maxSubjectParameter.ParameterName = "@maxSubject";
                                            groupParameter.Value = student.MaxSubject;

                                            insertCommand.Parameters.Add(nameParameter);
                                            insertCommand.Parameters.Add(groupParameter);
                                            insertCommand.Parameters.Add(avgGradeParameter);
                                            insertCommand.Parameters.Add(minSubjectParameter);
                                            insertCommand.Parameters.Add(maxSubjectParameter);

                                            int rowsAffected = await insertCommand.ExecuteNonQueryAsync();

                                            if (rowsAffected < 0)
                                                Console.WriteLine("Error inserting student into database");
                                        }
                                        stopwatch.Stop();
                                        logger.LogQuery(query, stopwatch.ElapsedMilliseconds);
                                        schoolStudentsNew.Clear();
                                        Console.WriteLine("Database updated");
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("List is empty");

                                }
                                Console.ReadLine();
                                break;
                            case "4":
                                Console.WriteLine("ID студента для оновлення:");
                                int updateId = int.Parse(Console.ReadLine());

                                Console.WriteLine("Нове ім'я:");
                                string newName = Console.ReadLine();

                                query = "UPDATE StudentGrades SET FullName = @fullName WHERE Id = @id";
                                stopwatch = Stopwatch.StartNew();
                                using (DbCommand updateCommand = connection.CreateCommand())
                                {
                                    updateCommand.CommandText = query;
                                    updateCommand.CommandType = CommandType.Text;

                                    var idParameter = updateCommand.CreateParameter();
                                    idParameter.ParameterName = "@id";
                                    idParameter.Value = updateId;

                                    var nameParameter = updateCommand.CreateParameter();
                                    nameParameter.ParameterName = "@fullName";
                                    nameParameter.Value = newName;

                                    updateCommand.Parameters.Add(idParameter);
                                    updateCommand.Parameters.Add(nameParameter);

                                    int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                                    if (rowsAffected < 0)
                                        Console.WriteLine("Error updating");
                                }
                                stopwatch.Stop();
                                logger.LogQuery(query, stopwatch.ElapsedMilliseconds);
                                break;

                            case "5":
                                Console.WriteLine("ID студента для видалення:");
                                int deleteId = int.Parse(Console.ReadLine());

                                query = "DELETE FROM StudentGrades WHERE Id = @id";
                                stopwatch = Stopwatch.StartNew();
                                using (DbCommand deleteCommand = connection.CreateCommand())
                                {
                                    deleteCommand.CommandText = query;
                                    deleteCommand.CommandType = CommandType.Text;

                                    var idParameter = deleteCommand.CreateParameter();
                                    idParameter.ParameterName = "@id";
                                    idParameter.Value = deleteId;

                                    deleteCommand.Parameters.Add(idParameter);

                                    int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
                                    if (rowsAffected < 0)
                                        Console.WriteLine("Error deleting");
                                }
                                stopwatch.Stop();
                                logger.LogQuery(query, stopwatch.ElapsedMilliseconds);
                                break;
                            case "6":
                                await SelectDatabaseAsync(configuration);
                                break;
                            case "7":
                                await ExportToCsvAsync(schoolStudents);
                                break;
                            case "8":
                                Console.WriteLine("Введіть шлях до CSV файлу:");
                                string filePath = Console.ReadLine();
                                await ImportFromCsvAsync(connection, filePath);
                                break;
                            default:
                                break;
                        }
                    }
                    
                }
                else
                {
                    Console.WriteLine("schoolStudents is null");
                }
            }
        }
        private static async Task ImportFromCsvAsync(DbConnection connection, string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Файл {filePath} не знайдено.");
                return;
            }

            List<SchoolStudent> studentsToInsert = new List<SchoolStudent>();
            List<SchoolStudent> failedToInsert = new List<SchoolStudent>();
            int successfulImports = 0;
            int failedImports = 0;

            using (var reader = new StreamReader(filePath))
            {
                await reader.ReadLineAsync();

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var values = line.Split(',');

                    if (values.Length < 6)
                    {
                        failedImports++;
                        continue;
                    }
                    if (!double.TryParse(values[3], out double avgGrade))
                    {
                        failedImports++;
                        continue;
                    }

                    var student = new SchoolStudent
                    {
                        Id = studentsToInsert.Count + 1,
                        Name = values[1],
                        GroupName = values[2],
                        AvgGrade = avgGrade,
                        MinSubject = values[4],
                        MaxSubject = values[5]
                    };

                    studentsToInsert.Add(student);
                }
            }

            foreach (var student in studentsToInsert)
            {
                try
                {
                    string query = "INSERT INTO StudentGrades (FullName, GroupName, AverageGrade, MinSubject, MaxSubject) " +
                                   "VALUES (@fullName, @groupName, @averageGrade, @minSubject, @maxSubject)";
                    using (DbCommand insertCommand = connection.CreateCommand())
                    {
                        insertCommand.CommandText = query;
                        insertCommand.CommandType = CommandType.Text;

                        var nameParameter = insertCommand.CreateParameter();
                        nameParameter.ParameterName = "@fullName";
                        nameParameter.Value = student.Name;

                        var groupParameter = insertCommand.CreateParameter();
                        groupParameter.ParameterName = "@groupName";
                        groupParameter.Value = student.GroupName;

                        var avgGradeParameter = insertCommand.CreateParameter();
                        avgGradeParameter.ParameterName = "@averageGrade";
                        avgGradeParameter.Value = student.AvgGrade;

                        var minSubjectParameter = insertCommand.CreateParameter();
                        minSubjectParameter.ParameterName = "@minSubject";
                        minSubjectParameter.Value = student.MinSubject;

                        var maxSubjectParameter = insertCommand.CreateParameter();
                        maxSubjectParameter.ParameterName = "@maxSubject";
                        maxSubjectParameter.Value = student.MaxSubject;

                        insertCommand.Parameters.Add(nameParameter);
                        insertCommand.Parameters.Add(groupParameter);
                        insertCommand.Parameters.Add(avgGradeParameter);
                        insertCommand.Parameters.Add(minSubjectParameter);
                        insertCommand.Parameters.Add(maxSubjectParameter);

                        int rowsAffected = await insertCommand.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                            successfulImports++;
                        else
                            failedImports++;
                    }
                }
                catch (Exception)
                {
                    failedImports++;
                }
            }

            Console.WriteLine($"Імпорт завершено. Успішно додано: {successfulImports} записів.");
            Console.WriteLine($"Не вдалося додати: {failedImports} записів.");
        }

        private static async Task ExportToCsvAsync(List<SchoolStudent> schoolStudents)
        {
            if (schoolStudents == null || schoolStudents.Count == 0)
            {
                Console.WriteLine("Немає даних для експорту.");
                return;
            }

            using (var writer = new StreamWriter("students_export.csv", false, Encoding.UTF8))
            {
                await writer.WriteLineAsync("Id,Name,GroupName,AvgGrade,MinSubject,MaxSubject");

                foreach (var student in schoolStudents)
                {
                    string line = $"{student.Id},{student.Name},{student.GroupName},{student.AvgGrade},{student.MinSubject},{student.MaxSubject}";
                    await writer.WriteLineAsync(line);
                }
            }

            Console.WriteLine($"Дані успішно експортовані у файл \"students_export.csv\"");
        }

        private static async Task SelectDatabaseAsync(IConfigurationRoot configuration)
        {
            Console.WriteLine("Оберіть СКБД:");
            Console.WriteLine("1 - 1A Class");
            Console.WriteLine("2 - 10S Class");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    connectionString = configuration.GetConnectionString("FirstConnection");
                    break;

                case "2":
                    connectionString = configuration.GetConnectionString("SecondConnection");
                    break;

                default:
                    connectionString = configuration.GetConnectionString("FirstConnection");
                    break;
            }

            Console.WriteLine($"Обрано: {choice}. З'єднання встановлюється...");
        }
    }

    public class Logger
    {   
        private string LogDirectory => "Logs";
        private string CurrentLogFile => Path.Combine(LogDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");

        public Logger()
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }

        public void LogQuery(string query, long durationMs)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Query: {query} | Duration: {durationMs} ms\n";

            File.AppendAllText(CurrentLogFile, logMessage);
        }
    }
    public class SchoolStudent
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string GroupName { get; set; }
        public double AvgGrade { get; set; }
        public string MinSubject { get; set; }
        public string MaxSubject { get; set; }
    }
}
