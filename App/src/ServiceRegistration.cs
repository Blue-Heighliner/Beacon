namespace BlueHeighliner.Beacon;

/// <summary>Registers the app's services.</summary>
internal static class ServiceRegistration
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers every service, repository, and view model, storing data under <paramref name="dataDirectory" />.</summary>
        /// <param name="dataDirectory">The folder for the database and key file.</param>
        /// <returns>The same collection.</returns>
        public IServiceCollection AddBeacon(string dataDirectory)
        {
            services.AddSingleton<IAppPaths>(new AppPaths(dataDirectory));
            services.AddSingleton<IClock, Clock>();
            services.AddSingleton<IKeyStore, FileKeyStore>();
            services.AddSingleton<IDodIdCipher>(provider => OperatingSystem.IsWindows() ? new DpapiDodIdCipher() : new AesDodIdCipher(provider.GetRequiredService<IKeyStore>()));
            services.AddSingleton<IDodIdHasher, DodIdHasher>();
            services.AddSingleton<IDatabase, Database>();
            services.AddSingleton<IUserRepository, UserRepository>();
            services.AddSingleton<IItemRepository, ItemRepository>();
            services.AddSingleton<IInputValidator, InputValidator>();
            services.AddSingleton<ICredentialHasher, CredentialHasher>();
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IExcelExportService, ExcelExportService>();
            services.AddSingleton<IFilePicker, FilePicker>();
            services.AddSingleton<IUiTimerFactory, UiTimerFactory>();
            services.AddSingleton<INavigation, Navigation>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<InventoryViewModel>();
            services.AddTransient<AccountViewModel>();
            services.AddTransient<UserManagementViewModel>();
            return services;
        }
    }
}
