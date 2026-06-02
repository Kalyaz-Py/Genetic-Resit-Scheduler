using diplom.Views;
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

namespace diplom
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Вход для студентов (без пароля)
        private void BtnStudentMode_Click(object sender, RoutedEventArgs e)
        {
            // Создаем экземпляр гостевого окна студента
            StudentGuestWindow studentWindow = new StudentGuestWindow();
            studentWindow.Show();

            // Закрываем текущее стартовое окно
            this.Close();
        }

        // Вход для Администратора / Заведующего отделением
        private void BtnAdminLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = TxtLogin.Text.Trim();
            string password = TxtPassword.Password.Trim();

            // Проверка на заполненность полей
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, заполните все поля для авторизации.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка учетных данных (демо-режим авторизации)
            if (login == "admin" && password == "admin")
            {
                // Если данные верны, открываем панель администратора
                AdminWindow adminWindow = new AdminWindow();
                adminWindow.Show();

                // Закрываем окно входа
                this.Close();
            }
            else
            {
                // Если пароль не подошел — выводим предупреждение
                MessageBox.Show("Неверный логин или пароль! Доступ заблокирован.", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);

                // Очищаем поле пароля для повторного ввода
                TxtPassword.Clear();
            }
        }

        // Просмотр долгов студентов (без авторизации)
        private void BtnShowDebts_Click(object sender, RoutedEventArgs e)
        {
            DebtsFilterWindow debtsWindow = new DebtsFilterWindow();
            debtsWindow.ShowDialog();
        }
    }
}
