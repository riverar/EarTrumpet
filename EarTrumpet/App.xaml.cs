using System.Threading;
using System.Windows;

namespace EarTrumpet
{
    public partial class App
    {
        private Mutex _mutex;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!CreateMutex())
            {
                _mutex = null;
                Current.Shutdown();
            }

            new MainWindow();
        }

        private bool CreateMutex()
        {
            bool mutexCreated;

            #if DEBUG
            _mutex = new Mutex(true, @"Local\EarTrumpet_Debug", out mutexCreated);
            #else
            _mutex = new Mutex(true, @"Local\EarTrumpet", out mutexCreated);
            #endif

            return mutexCreated;
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Close();
            }
        }
    }
}
