using System;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using Npgsql;
using Microsoft.Win32;
using System.IO;
using System.Windows.Threading;
using System.ComponentModel;

namespace Otchet
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly string _connectionString = "Host=172.20.7.53;Port=5432;Database=db2991_08;Username=st2991;Password=pwd2991";
        private User _currentUser;
        private DateTime _currentDateTime;

        public event PropertyChangedEventHandler PropertyChanged;

        public User CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged(nameof(CurrentUser));
            }
        }

        public DateTime CurrentDateTime
        {
            get => _currentDateTime;
            set
            {
                _currentDateTime = value;
                OnPropertyChanged(nameof(CurrentDateTime));
            }
        }

        public MainWindow(User user)
        {
            InitializeComponent();
            CurrentUser = user;
            DataContext = this;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) => CurrentDateTime = DateTime.Now;
            timer.Start();

            LoadDocuments();
            if (CurrentUser.IsAdmin)
            {
                LoadUsers();
                LoadOrganizations();
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadDocuments()
        {
            try
            {
                string query = @"
                    SELECT c.correspondence_id, c.document_type, c.received_date, c.execution_date, 
                           u.last_name || ' ' || u.first_middle_name as user_full_name
                    FROM correspondence.correspondence c
                    JOIN correspondence.users u ON c.user_id = u.user_id
                    ORDER BY c.received_date DESC";

                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    var adapter = new NpgsqlDataAdapter(query, conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgDocuments.ItemsSource = dt.DefaultView;
                }
                txtStatus.Text = $"Загружено {dgDocuments.Items.Count} документов";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки документов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddDocument_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DocumentEditDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        var cmd = new NpgsqlCommand(
                            "INSERT INTO correspondence.correspondence (document_type, received_date, execution_date, user_id) " +
                            "VALUES (@type, @received, @execution, @userId) RETURNING correspondence_id", conn);

                        cmd.Parameters.AddWithValue("@type", dialog.DocumentType);
                        cmd.Parameters.AddWithValue("@received", dialog.ReceivedDate);
                        cmd.Parameters.AddWithValue("@execution", dialog.ExecutionDate);
                        cmd.Parameters.AddWithValue("@userId", CurrentUser.UserId);

                        var id = cmd.ExecuteScalar();
                        txtStatus.Text = $"Добавлен новый документ ID: {id}";
                        LoadDocuments();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditDocument_Click(object sender, RoutedEventArgs e)
        {
            if (dgDocuments.SelectedItem == null)
            {
                MessageBox.Show("Выберите документ для редактирования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = (DataRowView)dgDocuments.SelectedItem;
            var dialog = new DocumentEditDialog
            {
                DocumentType = row["document_type"].ToString(),
                ReceivedDate = (DateTime)row["received_date"],
                ExecutionDate = (DateTime)row["execution_date"]
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        var cmd = new NpgsqlCommand(
                            "UPDATE correspondence.correspondence SET " +
                            "document_type = @type, received_date = @received, execution_date = @execution " +
                            "WHERE correspondence_id = @id", conn);

                        cmd.Parameters.AddWithValue("@type", dialog.DocumentType);
                        cmd.Parameters.AddWithValue("@received", dialog.ReceivedDate);
                        cmd.Parameters.AddWithValue("@execution", dialog.ExecutionDate);
                        cmd.Parameters.AddWithValue("@id", (int)row["correspondence_id"]);

                        int affected = cmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            txtStatus.Text = $"Документ ID: {row["correspondence_id"]} обновлен";
                            LoadDocuments();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка редактирования документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteDocument_Click(object sender, RoutedEventArgs e)
        {
            if (dgDocuments.SelectedItem == null)
            {
                MessageBox.Show("Выберите документ для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите удалить выбранный документ?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var row = (DataRowView)dgDocuments.SelectedItem;
                try
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        var cmd = new NpgsqlCommand(
                            "DELETE FROM correspondence.correspondence WHERE correspondence_id = @id", conn);
                        cmd.Parameters.AddWithValue("@id", (int)row["correspondence_id"]);

                        int affected = cmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            txtStatus.Text = $"Документ ID: {row["correspondence_id"]} удален";
                            LoadDocuments();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SearchDocuments_Click(object sender, RoutedEventArgs e)
        {
            string searchValue = txtSearchValue.Text.Trim();
            if (string.IsNullOrEmpty(searchValue))
            {
                MessageBox.Show("Введите значение для поиска", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string searchField = ((ComboBoxItem)cmbSearchField.SelectedItem).Content.ToString();
            string query = @"
                SELECT c.correspondence_id, c.document_type, c.received_date, c.execution_date, 
                       u.last_name || ' ' || u.first_middle_name as user_full_name
                FROM correspondence.correspondence c
                JOIN correspondence.users u ON c.user_id = u.user_id
                WHERE ";

            switch (searchField)
            {
                case "Тип документа":
                    query += "c.document_type ILIKE @value";
                    searchValue = $"%{searchValue}%";
                    break;
                case "Дата получения":
                    if (!DateTime.TryParse(searchValue, out _))
                    {
                        MessageBox.Show("Введите корректную дату", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    query += "c.received_date = @value::date";
                    break;
                case "Дата исполнения":
                    if (!DateTime.TryParse(searchValue, out _))
                    {
                        MessageBox.Show("Введите корректную дату", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    query += "c.execution_date = @value::date";
                    break;
            }

            query += " ORDER BY c.received_date DESC";

            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new NpgsqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@value", searchValue);

                    var adapter = new NpgsqlDataAdapter(cmd);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgDocuments.ItemsSource = dt.DefaultView;
                }
                txtStatus.Text = $"Найдено {dgDocuments.Items.Count} документов";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearchValue.Text = "";
            LoadDocuments();
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                FileName = $"Документы_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ExportHelper.ExportToExcel(dgDocuments, saveFileDialog.FileName);
                    txtStatus.Text = $"Данные экспортированы в {saveFileDialog.FileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadUsers()
        {
            try
            {
                string query = @"
                    SELECT u.user_id, u.username, u.last_name, u.first_middle_name, 
                           r.role_name as role, o.name as organization, u.email
                    FROM correspondence.users u
                    JOIN correspondence.roles r ON u.role_id = r.role_id
                    JOIN correspondence.organizations o ON u.organization_id = o.organization_id
                    ORDER BY u.last_name, u.first_middle_name";

                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    var adapter = new NpgsqlDataAdapter(query, conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgUsers.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserEditDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        var cmd = new NpgsqlCommand(
                            "INSERT INTO correspondence.users (username, password_hash, last_name, first_middle_name, email, role_id, organization_id) " +
                            "VALUES (@username, crypt(@password, gen_salt('bf')), @lastName, @firstName, @email, @roleId, @orgId) RETURNING user_id", conn);

                        cmd.Parameters.AddWithValue("@username", dialog.Username);
                        cmd.Parameters.AddWithValue("@password", dialog.Password);
                        cmd.Parameters.AddWithValue("@lastName", dialog.LastName);
                        cmd.Parameters.AddWithValue("@firstName", dialog.FirstName);
                        cmd.Parameters.AddWithValue("@email", dialog.Email);
                        cmd.Parameters.AddWithValue("@roleId", dialog.RoleId);
                        cmd.Parameters.AddWithValue("@orgId", dialog.OrganizationId);

                        var id = cmd.ExecuteScalar();
                        txtStatus.Text = $"Добавлен новый пользователь ID: {id}";
                        LoadUsers();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem == null)
            {
                MessageBox.Show("Выберите пользователя для редактирования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = (DataRowView)dgUsers.SelectedItem;
            var dialog = new UserEditDialog
            {
                Username = row["username"].ToString(),
                LastName = row["last_name"].ToString(),
                FirstName = row["first_middle_name"].ToString(),
                Email = row["email"].ToString(),
                IsEditMode = true
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        var cmd = new NpgsqlCommand(
                            "UPDATE correspondence.users SET " +
                            "username = @username, last_name = @lastName, first_middle_name = @firstName, " +
                            "email = @email, role_id = @roleId, organization_id = @orgId " +
                            (string.IsNullOrEmpty(dialog.Password) ? "" : ", password_hash = crypt(@password, gen_salt('bf')) ") +
                            "WHERE user_id = @id", conn);

                        cmd.Parameters.AddWithValue("@username", dialog.Username);
                        cmd.Parameters.AddWithValue("@lastName", dialog.LastName);
                        cmd.Parameters.AddWithValue("@firstName", dialog.FirstName);
                        cmd.Parameters.AddWithValue("@email", dialog.Email);
                        cmd.Parameters.AddWithValue("@roleId", dialog.RoleId);
                        cmd.Parameters.AddWithValue("@orgId", dialog.OrganizationId);
                        cmd.Parameters.AddWithValue("@id", (int)row["user_id"]);

                        if (!string.IsNullOrEmpty(dialog.Password))
                            cmd.Parameters.AddWithValue("@password", dialog.Password);

                        int affected = cmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            txtStatus.Text = $"Пользователь ID: {row["user_id"]} обновлен";
                            LoadUsers();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка редактирования пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem == null)
            {
                MessageBox.Show("Выберите пользователя для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = (DataRowView)dgUsers.SelectedItem;
            if ((int)row["user_id"] == CurrentUser.UserId)
            {
                MessageBox.Show("Вы не можете удалить самого себя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите удалить выбранного пользователя?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        var cmd = new NpgsqlCommand(
                            "DELETE FROM correspondence.users WHERE user_id = @id", conn);
                        cmd.Parameters.AddWithValue("@id", (int)row["user_id"]);

                        int affected = cmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            txtStatus.Text = $"Пользователь ID: {row["user_id"]} удален";
                            LoadUsers();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadOrganizations()
        {
            try
            {
                string query = "SELECT organization_id, name, ownership_type FROM correspondence.organizations ORDER BY name";

                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    var adapter = new NpgsqlDataAdapter(query, conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgOrganizations.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки организаций: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddOrganization_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OrganizationEditDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        var cmd = new NpgsqlCommand(
                            "INSERT INTO correspondence.organizations (name, ownership_type) " +
                            "VALUES (@name, @type) RETURNING organization_id", conn);

                        cmd.Parameters.AddWithValue("@name", dialog.Name);
                        cmd.Parameters.AddWithValue("@type", dialog.OwnershipType);

                        var id = cmd.ExecuteScalar();
                        txtStatus.Text = $"Добавлена новая организация ID: {id}";
                        LoadOrganizations();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления организации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditOrganization_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrganizations.SelectedItem == null)
            {
                MessageBox.Show("Выберите организацию для редактирования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = (DataRowView)dgOrganizations.SelectedItem;
            var dialog = new OrganizationEditDialog
            {
                Name = row["name"].ToString(),
                OwnershipType = row["ownership_type"].ToString()
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        var cmd = new NpgsqlCommand(
                            "UPDATE correspondence.organizations SET " +
                            "name = @name, ownership_type = @type " +
                            "WHERE organization_id = @id", conn);

                        cmd.Parameters.AddWithValue("@name", dialog.Name);
                        cmd.Parameters.AddWithValue("@type", dialog.OwnershipType);
                        cmd.Parameters.AddWithValue("@id", (int)row["organization_id"]);

                        int affected = cmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            txtStatus.Text = $"Организация ID: {row["organization_id"]} обновлена";
                            LoadOrganizations();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка редактирования организации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteOrganization_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrganizations.SelectedItem == null)
            {
                MessageBox.Show("Выберите организацию для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = (DataRowView)dgOrganizations.SelectedItem;
            if (MessageBox.Show("Вы уверены, что хотите удалить выбранную организацию?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        var cmd = new NpgsqlCommand(
                            "DELETE FROM correspondence.organizations WHERE organization_id = @id", conn);
                        cmd.Parameters.AddWithValue("@id", (int)row["organization_id"]);

                        int affected = cmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            txtStatus.Text = $"Организация ID: {row["organization_id"]} удалена";
                            LoadOrganizations();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления организации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            if (dpReportStartDate.SelectedDate == null || dpReportEndDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите диапазон дат", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dpReportStartDate.SelectedDate > dpReportEndDate.SelectedDate)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string reportType = ((ComboBoxItem)cmbReportType.SelectedItem).Content.ToString();
            string query = "";

            switch (reportType)
            {
                case "По типам документов":
                    query = @"
                        SELECT document_type as ""Тип документа"", 
                               COUNT(*) as ""Количество"",
                               MIN(received_date) as ""Первая дата"",
                               MAX(received_date) as ""Последняя дата""
                        FROM correspondence.correspondence
                        WHERE received_date BETWEEN @start AND @end
                        GROUP BY document_type
                        ORDER BY COUNT(*) DESC";
                    break;
                case "По исполнителям":
                    query = @"
                        SELECT u.last_name || ' ' || u.first_middle_name as ""Исполнитель"",
                               COUNT(*) as ""Количество документов"",
                               COUNT(DISTINCT c.document_type) as ""Разнообразие типов""
                        FROM correspondence.correspondence c
                        JOIN correspondence.users u ON c.user_id = u.user_id
                        WHERE c.received_date BETWEEN @start AND @end
                        GROUP BY u.user_id, u.last_name, u.first_middle_name
                        ORDER BY COUNT(*) DESC";
                    break;
                case "По организациям":
                    query = @"
                        SELECT o.name as ""Организация"",
                               COUNT(*) as ""Количество документов"",
                               COUNT(DISTINCT c.document_type) as ""Разнообразие типов""
                        FROM correspondence.correspondence c
                        JOIN correspondence.users u ON c.user_id = u.user_id
                        JOIN correspondence.organizations o ON u.organization_id = o.organization_id
                        WHERE c.received_date BETWEEN @start AND @end
                        GROUP BY o.organization_id, o.name
                        ORDER BY COUNT(*) DESC";
                    break;
            }

            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new NpgsqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@start", dpReportStartDate.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@end", dpReportEndDate.SelectedDate.Value);

                    var adapter = new NpgsqlDataAdapter(cmd);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgReports.ItemsSource = dt.DefaultView;
                }
                txtStatus.Text = $"Сформирован отчет: {reportType}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            if (dgReports.Items.Count == 0)
            {
                MessageBox.Show("Нет данных для печати", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    MessageBox.Show("Печать отчета...", "Печать", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка печати: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var authWindow = new AuthorizationWindow();
            authWindow.Show();
            this.Close();
        }

        private void DocumentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDocuments.SelectedItem != null)
            {
                var row = (DataRowView)dgDocuments.SelectedItem;
                txtStatus.Text = $"Выбран документ: {row["document_type"]} от {row["received_date"]}";
            }
        }

        private void RefreshDocuments_Click(object sender, RoutedEventArgs e) => LoadDocuments();
        private void RefreshUsers_Click(object sender, RoutedEventArgs e) => LoadUsers();
        private void RefreshOrganizations_Click(object sender, RoutedEventArgs e) => LoadOrganizations();

        private void Search_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SearchDocuments_Click(sender, e);
            }
        }
    }
}