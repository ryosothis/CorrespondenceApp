using System;
using System.Windows;

namespace Otchet
{
    public partial class DocumentEditDialog : Window
    {
        public string DocumentType { get; set; }
        public DateTime ReceivedDate { get; set; } = DateTime.Today;
        public DateTime ExecutionDate { get; set; } = DateTime.Today;

        public DocumentEditDialog()
        {
            InitializeComponent();
            DataContext = this;
            dpReceivedDate.SelectedDate = ReceivedDate;
            dpExecutionDate.SelectedDate = ExecutionDate;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDocumentType.Text))
            {
                MessageBox.Show("Введите тип документа", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (dpReceivedDate.SelectedDate == null || dpExecutionDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите обе даты", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (dpExecutionDate.SelectedDate < dpReceivedDate.SelectedDate)
            {
                MessageBox.Show("Дата исполнения не может быть раньше даты получения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DocumentType = txtDocumentType.Text;
            ReceivedDate = dpReceivedDate.SelectedDate.Value;
            ExecutionDate = dpExecutionDate.SelectedDate.Value;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}