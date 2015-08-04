using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace SyncBox_Server
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        db db_handle = new db();

        public MainWindow()
        {
            InitializeComponent();
            //MessageBox.Show("Begin");
        }

        private void b_start_Click(object sender, RoutedEventArgs e)
        {
 
            MessageBox.Show("Ciao!" + db_handle.start());
            

        }
    }
}
