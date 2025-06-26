using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using System.Text.RegularExpressions;

namespace Otchet
{
    public partial class RegistrationWindow : Window
    {
        private readonly string _connectionString = "Host=172.20.7.53;Port=5432;Database=db2991_08;Username=st2991;Password=pwd2991";
        private int _defaultRoleId = 2;
        private int _defaultOrganizationId = 1;
        public RegistrationWindow()
        {
            InitializeComponent();
            Loaded += RegistrationWindow_Loaded;
        }

        private void RegistrationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDefaultRole();
        }

        private void LoadDefaultRole()
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new NpgsqlCommand("SELECT role_id FROM correspondence.roles WHERE role_name = 'user'", conn);
                    var result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        _defaultRoleId = Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки роли по умолчанию: {ex.Message}",
                                "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TogglePasswordVisibility(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                int index = int.Parse(tag);

                if (index == 1)
                {
                    if (txtPassword.Visibility == Visibility.Visible)
                    {
                        txtVisiblePassword1.Text = txtPassword.Password;
                        txtPassword.Visibility = Visibility.Collapsed;
                        txtVisiblePassword1.Visibility = Visibility.Visible;
                        btnTogglePassword1.Content = "🔒";
                    }
                    else
                    {
                        txtPassword.Password = txtVisiblePassword1.Text;
                        txtVisiblePassword1.Visibility = Visibility.Collapsed;
                        txtPassword.Visibility = Visibility.Visible;
                        btnTogglePassword1.Content = "👁️";
                    }
                }
                else if (index == 2)
                {
                    if (txtConfirmPassword.Visibility == Visibility.Visible)
                    {
                        txtVisiblePassword2.Text = txtConfirmPassword.Password;
                        txtConfirmPassword.Visibility = Visibility.Collapsed;
                        txtVisiblePassword2.Visibility = Visibility.Visible;
                        btnTogglePassword2.Content = "🔒";
                    }
                    else
                    {
                        txtConfirmPassword.Password = txtVisiblePassword2.Text;
                        txtVisiblePassword2.Visibility = Visibility.Collapsed;
                        txtConfirmPassword.Visibility = Visibility.Visible;
                        btnTogglePassword2.Content = "👁️";
                    }
                }
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT correspondence.register_user(@username, @password, @last_name, @first_name, @email, @role_id, @org_id)", conn))
                    {
                        cmd.Parameters.AddWithValue("username", txtUsername.Text.Trim());
                        cmd.Parameters.AddWithValue("password", txtPassword.Password);
                        cmd.Parameters.AddWithValue("last_name", txtLastName.Text.Trim());
                        cmd.Parameters.AddWithValue("first_name", txtFirstName.Text.Trim());
                        cmd.Parameters.AddWithValue("email", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("role_id", _defaultRoleId);
                        cmd.Parameters.AddWithValue("org_id", _defaultOrganizationId);

                        var result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            int newUserId = Convert.ToInt32(result);

                            if (newUserId > 0)
                            {
                                MessageBox.Show("Регистрация успешно завершена!", "Успех",
                                                MessageBoxButton.OK, MessageBoxImage.Information);

                                AuthorizationWindow authWindow = new AuthorizationWindow();
                                authWindow.Show();
                                this.Close();
                            }
                        }
                        else
                        {
                            MessageBox.Show("Пользователь с таким логином или email уже существует",
                                            "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}", "Системная ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                MessageBox.Show("Введите фамилию", "Ошибка ввода",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtLastName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                MessageBox.Show("Введите имя и отчество", "Ошибка ввода",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtFirstName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Введите логин", "Ошибка ввода",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsername.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Введите email", "Ошибка ввода",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return false;
            }

            if (!Regex.IsMatch(txtEmail.Text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Неверный формат email", "Ошибка ввода",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Введите пароль", "Ошибка ввода",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return false;
            }

            if (txtPassword.Password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать не менее 6 символов",
                                "Слабый пароль", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return false;
            }

            if (txtPassword.Password != txtConfirmPassword.Password)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка ввода",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtConfirmPassword.Focus();
                return false;
            }

            return true;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            AuthorizationWindow authWindow = new AuthorizationWindow();
            authWindow.Show();
            this.Close();
        }
    }
}