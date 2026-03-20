using System.Windows;
using Prism.Ioc;
using Prism.Unity;
using WonderlandServerWpf.Services;
using WonderlandServerWpf.Views;

namespace WonderlandServerWpf
{
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ILogService, LogService>();
            containerRegistry.RegisterSingleton<IServerEngine, ServerEngine>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            var engine = Container.Resolve<IServerEngine>();
            engine.StartAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            var engine = Container.Resolve<IServerEngine>();
            engine.Shutdown();
            base.OnExit(e);
        }
    }
}
