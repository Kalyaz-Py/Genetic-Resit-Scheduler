using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace diplom
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Корректная установка лицензии для EPPlus 8+
            ExcelPackage.License.SetNonCommercialPersonal("Руфат");

            base.OnStartup(e);
        }
    }
}
