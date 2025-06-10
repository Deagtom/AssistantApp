using AssistantApp.Data;
using AssistantApp.ViewModels;
using Microsoft.Win32;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace AssistantApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var conn = ConfigurationManager.ConnectionStrings["PostgresConnection"].ConnectionString;
            DataContext = new SymptomSelectionViewModel(new DatabaseService(conn));
            ((SymptomSelectionViewModel)DataContext).LoadDataCommand.Execute(null);
        }

        private async void LoadFromFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv"
            };
            if (dlg.ShowDialog() == true)
            {
                var vm = DataContext as SymptomSelectionViewModel;
                await vm.ImportDataFromFileAsync(dlg.FileName);
            }
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int idx))
                MainTabControl.SelectedIndex = idx;
        }
    }
}