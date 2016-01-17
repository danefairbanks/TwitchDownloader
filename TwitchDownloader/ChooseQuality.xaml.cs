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
using System.Windows.Shapes;

namespace TwitchDownloader
{
    /// <summary>
    /// Interaction logic for ChooseQuality.xaml
    /// </summary>
    public partial class ChooseQuality : Window
    {
        public ChooseQuality()
        {
            InitializeComponent();
        }

        public int SelectedIndex
        {
            get
            {
                var rb = Choices.Children.OfType<RadioButton>().FirstOrDefault(x => x.IsChecked == true);
                return Choices.Children.IndexOf(rb);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
