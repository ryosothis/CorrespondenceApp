using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using System.Text.RegularExpressions;

namespace Otchet
{
    public partial class AuthorizationWindow : Window
    {
        private readonly string _connectionString = "Host=172.20.7.53;Port=5432;Database=db2991_08;Username=st2991;Password=pwd2991";

        public AuthorizationWindow()
        {
            InitializeComponent();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow registrationWindow = new RegistrationWindow();
            registrationWindow.Show();
            this.Close();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT correspondence.auth_user(@username, @password)", conn))
                    {
                        cmd.Parameters.AddWithValue("username", txtUsername.Text.Trim());
                        cmd.Parameters.AddWithValue("password", txtPassword.Password);

                        var result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            int userId = Convert.ToInt32(result);

                            if (userId > 0)
                            {
                                OpenMainWindow(userId);
                                return;
                            }
                        }

                        MessageBox.Show("Неверные учетные данные", "Ошибка авторизации",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (PostgresException ex)
            {
                HandleDatabaseError(ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Системная ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Введите имя пользователя", "Ошибка ввода",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsername.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Введите пароль", "Ошибка ввода",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return false;
            }

            if (!Regex.IsMatch(txtUsername.Text, @"^[a-zA-Z0-9_\.-]{3,50}$"))
            {
                MessageBox.Show("Логин должен содержать от 3 до 50 символов\nи состоять из букв, цифр, '_', '-' или '.'",
                                "Некорректный логин", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsername.Focus();
                return false;
            }

            if (txtPassword.Password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать не менее 6 символов",
                                "Слабый пароль", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return false;
            }

            return true;
        }

        private void HandleDatabaseError(PostgresException ex) 
        {
            string errorMessage = "Ошибка базы данных: ";

            switch (ex.SqlState)
            {
                case "28P01": 
                    errorMessage += "Ошибка аутентификации";
                    break;
                case "3D000":
                    errorMessage += "База данных не существует";
                    break;
                case "08006":
                    errorMessage += "Ошибка подключения к серверу";
                    break;
                case "42501":
                    errorMessage += "Недостаточно прав доступа";
                    break;
                default:
                    errorMessage += ex.Message;
                    break;
            }

            MessageBox.Show(errorMessage, "Ошибка базы данных",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OpenMainWindow(int userId)
        {
            User currentUser = GetUserInfo(userId);

            if (currentUser != null)
            {
                MainWindow mainWindow = new MainWindow(currentUser);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Ошибка загрузки данных пользователя",
                                "Системная ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private User GetUserInfo(int userId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                SELECT u.user_id, u.username, u.last_name, u.first_middle_name, 
                       r.role_id, r.role_name, o.name AS organization_name
                FROM correspondence.users u
                JOIN correspondence.roles r ON u.role_id = r.role_id
                JOIN correspondence.organizations o ON u.organization_id = o.organization_id
                WHERE u.user_id = @userId";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("userId", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new User
                                {
                                    UserId = reader.GetInt32(0),
                                    Username = reader.GetString(1),
                                    LastName = reader.GetString(2),
                                    FirstMiddleName = reader.GetString(3),
                                    RoleId = reader.GetInt32(4),
                                    Role = reader.GetString(5),
                                    Organization = reader.GetString(6)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения данных пользователя: {ex.Message}",
                                "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }
    }

    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string LastName { get; set; }
        public string FirstMiddleName { get; set; }
        public string Role { get; set; }
        public int RoleId { get; set; }
        public string Organization { get; set; }

        public bool IsAdmin => RoleId == 1;
        public string FullName => $"{LastName} {FirstMiddleName}";
    }
}