using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Reflection.PortableExecutable;

namespace ConsoleApp5
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string connectionString = @"Data Source=LINK\SQLEXPRESS;Initial Catalog=StudentGradesDB;Integrated Security=True;Connect Timeout=30;Encrypt=True;TrustServerCertificate=True;";
            
            List<SchoolStudent> schoolStudents = null;
            List<SchoolStudent> schoolStudentsNew = null;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM StudentGrades";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
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

                if (schoolStudents is not null)
                {
                    
                    while(true)
                    {
                        Console.Clear();
                        Console.WriteLine("Меню:");
                        Console.WriteLine("1 - Створити нового студента");
                        Console.WriteLine("2 - Показати всіх студентів");
                        Console.WriteLine("3 - Записати у базу данних");

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

                                
                                    query = "INSERT INTO StudentGrades (FullName, GroupName, AverageGrade, MinSubject, MaxSubject) VALUES (@fullName, @groupName, @averageGrade, @minSubject, @maxSubject)";
                                    using (SqlCommand command = new SqlCommand(query, connection))
                                    {
                                        foreach (var student in schoolStudentsNew)
                                        {
                                            command.Parameters.Clear();

                                            command.Parameters.AddWithValue("@fullname", student.Name);
                                            command.Parameters.AddWithValue("@groupName", student.GroupName);
                                            command.Parameters.AddWithValue("@averageGrade", student.AvgGrade);
                                            command.Parameters.AddWithValue("@minSubject", student.MinSubject);
                                            command.Parameters.AddWithValue("@maxSubject", student.MaxSubject);

                                            int result = command.ExecuteNonQuery();

                                            if (result < 0)
                                                Console.WriteLine("Error inserting student into database");
                                        }
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
