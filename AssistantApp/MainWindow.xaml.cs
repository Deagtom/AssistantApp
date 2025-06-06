using AssistantApp.Data;
using AssistantApp.ViewModels;
using System.Configuration;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AssistantApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var conn = ConfigurationManager.ConnectionStrings["PostgresConnection"].ConnectionString;
            DataContext = new SymptomSelectionViewModel(new DatabaseService(conn));
            ((SymptomSelectionViewModel)DataContext).LoadDataCommand.Execute(null);
        }
    }
}