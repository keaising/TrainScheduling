using System;
using System.Collections.Generic;
using System.Data.OleDb;
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

namespace TrainScheduling
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        //public readonly static string _strdata = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=C:\Users\xxming\Desktop\170213_TrainScheduling\TrainScheduling\password.mdb";
        string name;
        public readonly static string _strdata = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=..\..\Data\password.mdb";
        int password;
        OleDbConnection _oleDbConn = new OleDbConnection(_strdata);

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (textBox.Text == "" || passwordBox.Password == "")
            {
                MessageBox.Show("请输入账号密码！");
            }
            else
            {
                name = textBox.Text;
                password = Convert.ToInt32(passwordBox.Password);
                _oleDbConn.Open();
                OleDbCommand _oleDbCom = new OleDbCommand(@"SELECT * FROM [User]  WHERE UserName='" + name + "'AND Password=" + password + "", _oleDbConn);

                OleDbDataReader reader = _oleDbCom.ExecuteReader();
                if (reader.Read())
                {
                    MessageBox.Show("登陆成功！");
                    this.Hide();
                    SchedulingWindow mainwindow = new SchedulingWindow();
                    mainwindow.Show();
                }
                else
                {
                    MessageBox.Show("用户不存在！");
                }


                _oleDbConn.Close();
            }

        }

        private void RegButton_Click(object sender, RoutedEventArgs e)
        {
            if (textBox.Text == "" || passwordBox.Password == "")
            {
                MessageBox.Show("请输入注册账号密码！");
            }
            else
            {
                name = textBox.Text;
                password = Convert.ToInt32(passwordBox.Password);
                _oleDbConn.Open();
                OleDbCommand _oleDbCom = new OleDbCommand("INSERT INTO [User] ([UserName],[Password])VALUES ('" + name + "','" + password + "')", _oleDbConn);
                int num = _oleDbCom.ExecuteNonQuery();
                string message = num > 0 ? "添加成功" : "添加失败";
                MessageBox.Show(message);
                _oleDbConn.Close();
            }
        }

        private void ForgetButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (textBox.Text == "" || passwordBox.Password == "")
            {
                MessageBox.Show("请输入注销账号密码！");
            }
            else
            {
                name = textBox.Text;
                password = Convert.ToInt32(passwordBox.Password);
                _oleDbConn.Open();
                OleDbCommand _oleDbCom = new OleDbCommand("delete from [User] where [UserName]='" + name + "'", _oleDbConn);
                int num = _oleDbCom.ExecuteNonQuery();
                string message = num > 0 ? "注销成功" : "注销失败";
                MessageBox.Show(message);
                _oleDbConn.Close();
            }
        }
    }
}
