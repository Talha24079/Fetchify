using System.Windows;
using Fetchify.Helpers;
using WPF = System.Windows;

namespace Fetchify
{
    public partial class App : WPF.Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            Aria2ProcessManager.StopAria2();
            base.OnExit(e);
        }
    }
}
