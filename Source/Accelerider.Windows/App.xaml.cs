﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Accelerider.Windows.Properties;
using System.Windows;
using Accelerider.Windows.Constants;
using Accelerider.Windows.I18nResources;
using Accelerider.Windows.Infrastructure;
using Accelerider.Windows.Infrastructure.Mvvm;
using Accelerider.Windows.Infrastructure.Upgrade;
using Accelerider.Windows.ServerInteraction;
using Accelerider.Windows.TransferService.WpfInteractions;
using Accelerider.Windows.Views.Authentication;
using MaterialDesignThemes.Wpf;
using Prism.Events;
using Prism.Ioc;
using Prism.Logging;
using Prism.Mvvm;
using Prism.Unity;
using Refit;
using Unity;
using Unity.Injection;
using WpfExtensions.Xaml;
#if DEBUG
using Prism.Modularity;
#endif

namespace Accelerider.Windows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private const string ProcessName = @"Global\Accelerider.Windows.Wpf";
        private static readonly ILogger Logger = DefaultLogger.Get(typeof(App));

        protected override void OnStartup(StartupEventArgs e)
        {
            LogBasicInfo();

            ProcessController.CheckSingleton(ProcessName, (IntPtr)Settings.Default.WindowHandle);
            ConfigureApplicationEventHandlers();
            base.OnStartup(e);
        }

        protected override void ConfigureViewModelLocator()
        {
            ViewModelLocationProvider.SetDefaultViewModelFactory(new ViewModelResolver(() => Container)
                .UseDefaultConfigure()
                .ResolveViewModelForView);
        }

        public override void Initialize()
        {
            base.Initialize();
            InitializeCultureInfo();
            Settings.Default.PropertyChanged += (sender, eventArgs) => Settings.Default.Save();
        }

        //protected override void RegisterRequiredTypes(IContainerRegistry containerRegistry)
        //{
        //    base.RegisterRequiredTypes(containerRegistry);
        //    containerRegistry.RegisterSingleton<ILoggerFacade, Log4NetLogger>();
        //}

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            var snackbarMessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(2));
            var nonAuthenticationApi = RestService.For<INonAuthenticationApi>(AcceleriderUrls.ApiBaseAddress);
            var dataRepository = new DataRepository(() => "test@demo.com" /*Container.Resolve<IAcceleriderUser>().Email*/);
            var injectionConstructor = new InjectionConstructor(
                new Func<Task<List<UpgradeInfo>>>(() => nonAuthenticationApi.GetAppInfoList()));

            containerRegistry.RegisterInstance<ISnackbarMessageQueue>(snackbarMessageQueue);
            containerRegistry.RegisterInstance(nonAuthenticationApi);
            containerRegistry.RegisterInstance<IDataRepository>(dataRepository);
            containerRegistry.GetContainer().RegisterSingleton<IUpgradeService, UpgradeService>(injectionConstructor);
        }

        protected override Window CreateShell() => null;

        protected override void OnInitialized()
        {
            ApiExceptionResolverExtension.SetUnityContainer(Container);
            WindowHelper.Container = Container.GetContainer();

            WindowHelper.SwitchTo<AuthenticationWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (Container != null)
            {
                Container.Resolve<IUpgradeService>().Stop();
                Container.Resolve<IEventAggregator>().GetEvent<ApplicationExiting>().Publish();
                (Container.Resolve<ILoggerFacade>() as IDisposable)?.Dispose();
            }

            ProcessController.Clear();

            base.OnExit(e);
        }

#if DEBUG
        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new DirectoryModuleCatalog
            {
                ModulePath = "../Modules/netcoreapp3.1"
            };
        }
#endif

        // ---------------------------------------------------------------------------------------------------------------

        private static void ConfigureApplicationEventHandlers()
        {
            var handler = new ExceptionHandler();
            AppDomain.CurrentDomain.UnhandledException += handler.UnhandledExceptionHandler;
            Current.DispatcherUnhandledException += handler.DispatcherUnhandledExceptionHandler;
        }

        private void InitializeCultureInfo()
        {
            var settings = Container.Resolve<IDataRepository>().Get<ShellSettings>(isGlobal: true);

            I18nManager.Instance.CurrentUICultureChanged += (sender, args) => settings.Language = args.NewUICulture;

            if (settings.Language == null)
            {
                settings.Language = CultureInfo.InstalledUICulture;
            }

            I18nManager.Instance.CurrentUICulture = settings.Language;
            I18nManager.Instance.Add(UiResources.ResourceManager);
        }

        private static void LogBasicInfo()
        {
            Logger.Info(
                $"Accelerider for Windows: {AcceleriderConsts.Version};{Environment.NewLine}" +
                $"OS: {SystemInfo.Caption} ({SystemInfo.Version}) {SystemInfo.OSArchitecture};{Environment.NewLine}" +
                $".NET: {SystemInfo.DotNetFrameworkVersion};{Environment.NewLine}" +
                $"CLR: {Environment.Version};{Environment.NewLine}" +
                $"Processor: {SystemInfo.CPUName};{Environment.NewLine}" +
                $"RAM: {(DisplayDataSize) (SystemInfo.TotalVisibleMemorySize * 1024)};");
        }
    }
}