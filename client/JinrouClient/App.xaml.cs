using Prism;
using Prism.Ioc;
using JinrouClient.ViewModels;
using JinrouClient.Views;
using Xamarin.Essentials.Interfaces;
using Xamarin.Essentials.Implementation;
using Xamarin.Forms;
using Prism.DryIoc;
using System.Linq;
using JinrouClient.Usecase;
using JinrouClient.Domain.Repository;
using JinrouClient.Data.Repository;
using JinrouClient.Data;
using Acr.UserDialogs;
using Prism.Plugin.Popups;
using System.Threading.Tasks;

namespace JinrouClient
{
    public partial class App : PrismApplication
    {
        public App(IPlatformInitializer initializer)
            : base(initializer)
        {
        }

        protected override async void OnInitialized()
        {
            InitializeComponent();

            await NavigationService.NavigateAsync("/InitialPage");
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            var viewTypes = GetType().Assembly.DefinedTypes.Where(t => t.IsSubclassOf(typeof(Page)));
            foreach (var viewType in viewTypes)
            {
                containerRegistry.RegisterForNavigation(viewType, viewType.Name);
            }

            containerRegistry.RegisterForNavigation<NavigationPage>();

            containerRegistry.RegisterInstance(UserDialogs.Instance);

            containerRegistry.RegisterPopupNavigationService();

            containerRegistry.RegisterSingleton<IGameUsecase, GameUsecase>();
            containerRegistry.RegisterSingleton<IUserUsecase, UserUsecase>();
            containerRegistry.RegisterSingleton<IAuthRepository, AuthRepository>();
            containerRegistry.RegisterSingleton<IGameRepository, GameRepository>();
            containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();
            containerRegistry.RegisterInstance<IJinrouClientProvider>(new JinrouClientProvider(Config.ServerAddress));
        }
    }
}
