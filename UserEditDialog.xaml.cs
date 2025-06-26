using System;
using System.Collections.Generic;
using System.Windows;
using Npgsql;

namespace Otchet
{
    public partial class UserEditDialog : Window
    {
        private readonly string _connectionString = "Host=172.20.7.53;Port=5432;Database=db2991_08;Username=st2991;Password=pwd2991";

        public string Username { get; set; }
        public string Password { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public int OrganizationId { get; set; }

        public bool IsEditMode { get; set; }

        public string Title => IsEditMode ? "Редактирование пользователя" : "Новый пользователь";

        public UserEditDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRoles();
            LoadOrganizations();
            AdjustPasswordFieldsVisibility();
        }

        private void AdjustPasswordFieldsVisibility()
        {
            if (IsEditMode)
            {
                txtPasswordLabel.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Collapsed;
                txtNewPasswordLabel.Visibility = Visibility.Visible;
                txtNewPassword.Visibility = Visibility.Visible;
            }
            else
            {
                txtPasswordLabel.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Visible;
                txtNewPasswordLabel.Visibility = Visibility.Collapsed;
                txtNewPassword.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadRoles()
        {
            try
            {
                var roles = new List<Role>();
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT role_id, role_name FROM correspondence.roles", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            roles.Add(new Role
                            {
                                RoleId = reader.GetInt32(0),
                                RoleName = reader.GetString(1)
                            });
                        }
                    }
                }
                cmbRoles.ItemsSource = roles;
                if (roles.Count > 0)
                    cmbRoles.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadOrganizations()
        {
            try
            {
                var organizations = new List<Organization>();
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT organization_id, name FROM correspondence.organizations", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            organizations.Add(new Organization
                            {
                                OrganizationId = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
                cmbOrganizations.ItemsSource = organizations;
                if (organizations.Count > 0)
                    cmbOrganizations.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки организаций: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Введите логин", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!IsEditMode && string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Введите пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (IsEditMode && !string.IsNullOrWhiteSpace(txtNewPassword.Password) && txtNewPassword.Password.Length < 6)
            {
                MessageBox.Show("Пароль должен быть не менее 6 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                MessageBox.Show("Введите фамилию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                MessageBox.Show("Введите имя и отчество", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (cmbRoles.SelectedItem == null)
            {
                MessageBox.Show("Выберите роль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (cmbOrganizations.SelectedItem == null)
            {
                MessageBox.Show("Выберите организацию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Username = txtUsername.Text;
            Password = IsEditMode ? txtNewPassword.Password : txtPassword.Password;
            LastName = txtLastName.Text;
            FirstName = txtFirstName.Text;
            Email = txtEmail.Text;
            RoleId = ((Role)cmbRoles.SelectedItem).RoleId;
            OrganizationId = ((Organization)cmbOrganizations.SelectedItem).OrganizationId;

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }

    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }

    public class Organization
    {
        public int OrganizationId { get; set; }
        public string Name { get; set; }
    }
}