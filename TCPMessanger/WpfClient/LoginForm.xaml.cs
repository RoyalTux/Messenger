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
using System.Net;
namespace WpfClient
{
    /// <summary>
    /// Логика взаимодействия для LoginForm.xaml
    /// </summary>
    public partial class LoginForm : Window
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void Loginbutton_Click(object sender, RoutedEventArgs e)
        {
            string Username = txtUserName.Text;
            string IP = txtIP.Text;
            string Port = txtPort.Text;
            if(!CorrectData(IP, Port))
            {
                MessageBox.Show("Неверные данные");
                return;
            }

            ClientForm form = new ClientForm(Username, IP, Port);
            form.Show();

            this.Close();
        }

        private bool CorrectData(string ip, string port)
        {
            bool result = true;
            int p = -1;
            result = result && (int.TryParse(port, out p));
            result = result && ((0 <= p) && (p <= 65535));
            IPAddress a;
            result = result && IPAddress.TryParse(ip, out a);
            return result;
        }
    }
}
