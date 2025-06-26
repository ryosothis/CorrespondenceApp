using System.Windows;

namespace Otchet
{
    public partial class OrganizationEditDialog : Window
    {
        public string Name { get; set; }
        public string OwnershipType { get; set; }

        public OrganizationEditDialog()
        {
            InitializeComponent();
            cmbOwnershipType.SelectedIndex = 0;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите название организации", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (cmbOwnershipType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип собственности", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Name = txtName.Text;
            OwnershipType = ((System.Windows.Controls.ComboBoxItem)cmbOwnershipType.SelectedItem).Content.ToString();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}