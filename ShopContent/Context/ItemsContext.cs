using MySql.Data.MySqlClient;
using ShopContent.Classes;
using ShopContent.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ShopContent.Context
{
    public class ItemsContext : Items
    {
        // Конструктор
        public ItemsContext(bool isNew = false)
        {
            if (isNew)
            {
                this.Category = new Categorys(); // Пустая категория по умолчанию
            }
        }

        // Получение всех товаров
        public static ObservableCollection<ItemsContext> AllItems()
        {
            ObservableCollection<ItemsContext> allItems = new ObservableCollection<ItemsContext>();
            ObservableCollection<CategorysContext> allCategorys = CategorysContext.AllCategorys();

            MySqlConnection connection;
            MySqlDataReader dataItems = Connection.Query(
                "SELECT * FROM `ShopContent`.`Items`", out connection);

            while (dataItems.Read())
            {
                int catId = dataItems.GetInt32("IdCategory"); // Используем имя столбца для надёжности
                var category = allCategorys.FirstOrDefault(x => x.Id == catId);

                allItems.Add(new ItemsContext()
                {
                    Id = dataItems.GetInt32("Id"),
                    Name = dataItems.GetString("Name"),
                    Price = dataItems.GetDouble("Price"),
                    // ← Исправление: используем индекс столбца (3) вместо имени
                    Description = dataItems.IsDBNull(3) ? "" : dataItems.GetString("Description"),
                    Category = category
                });
            }
            Connection.CloseConnection(connection);
            return allItems;
        }

        // Сохранение (INSERT или UPDATE)
        public void Save(bool New = false)
        {
            // Проверка на наличие категории
            if (this.Category == null || this.Category.Id == 0)
            {
                System.Windows.MessageBox.Show("Ошибка: Не выбрана категория!");
                return;
            }

            MySqlConnection connection = null;

            try
            {
                if (New)
                {
                    // INSERT + получение нового ID
                    string sql = "INSERT INTO `ShopContent`.`Items` " +
                                 "(`Name`, `Price`, `Description`, `IdCategory`) " +
                                 "VALUES " +
                                 $"('{this.Name}', {this.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
                                 $"'{this.Description.Replace("'", "''")}', {this.Category.Id}); " +
                                 "SELECT LAST_INSERT_ID();";

                    MySqlDataReader reader = Connection.Query(sql, out connection);
                    if (reader.Read())
                    {
                        this.Id = reader.GetInt32(0);
                    }
                }
                else
                {
                    // UPDATE
                    string sql = "UPDATE `ShopContent`.`Items` " +
                                 "SET " +
                                 $"`Name` = '{this.Name.Replace("'", "''")}', " +
                                 $"`Price` = {this.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
                                 $"`Description` = '{this.Description.Replace("'", "''")}', " +
                                 $"`IdCategory` = {this.Category.Id} " +
                                 "WHERE `Id` = " + this.Id;

                    Connection.Query(sql, out connection);
                }
            }
            finally
            {
                Connection.CloseConnection(connection);
            }
        }

        // Удаление
        public void Delete()
        {
            MySqlConnection connection;
            Connection.Query($"DELETE FROM `ShopContent`.`Items` WHERE `Id` = {this.Id}", out connection);
            Connection.CloseConnection(connection);
        }

        // Команда редактирования (переход на страницу Add)
        public RelayCommand OnEdit
        {
            get
            {
                return new RelayCommand(obj =>
                {
                    MainWindow.init.frame.Navigate(new View.Add(this));
                });
            }
        }

        // Команда сохранения (вызывается на странице Add)
        public RelayCommand OnSave
        {
            get
            {
                return new RelayCommand(obj =>
                {
                    bool isNew = (this.Id == 0);
                    Save(isNew);

                    // Обновляем список на главной странице
                    MainWindow.init.Main.DataContext = new ViewModel.VMItems();
                    MainWindow.init.frame.Navigate(MainWindow.init.Main);
                });
            }
        }

        // Команда удаления
        public RelayCommand OnDelete
        {
            get
            {
                return new RelayCommand(obj =>
                {
                    if (System.Windows.MessageBox.Show("Удалить товар?", "Подтверждение",
                        System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
                    {
                        Delete();

                        // Удаляем из коллекции на главной странице
                        var vm = MainWindow.init.Main.DataContext as ViewModel.VMItems;
                        vm?.Items.Remove(this);
                    }
                });
            }
        }
    }
}