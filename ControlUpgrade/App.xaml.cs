
using System.Globalization;
using System.Windows;
using System.Threading;


namespace ControlUpgrade
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var culture = CultureInfo.CurrentUICulture;

            if (culture.TwoLetterISOLanguageName == "ja")
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("ja");
            }
            else
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            }
        }
    }

}
