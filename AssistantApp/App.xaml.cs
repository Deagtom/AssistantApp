using System.Configuration;
using System.Windows;
using AssistantApp.Data;
using AssistantApp.ViewModels;
using AssistantApp.Views;

namespace AssistantApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string connString = ConfigurationManager.ConnectionStrings["PostgresConnection"].ConnectionString;
            var dbService = new DatabaseService(connString);
            var viewModel = new SymptomSelectionViewModel(dbService);
            var mainWindow = new SymptomSelectionView { DataContext = viewModel };
            mainWindow.Show();
            viewModel.LoadDataCommand.Execute(null);
        }
    }
}