using Kwarta.Services;
using Kwarta.ViewModels;
using Kwarta.Views;
using Microsoft.Extensions.Logging;

namespace Kwarta
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("MaterialIconsRound-Regular.otf", Icons.FontFamily);
                });

            builder.Services.AddSingleton<BudgetDatabase>();
            builder.Services.AddSingleton<BackupService>();
            builder.Services.AddSingleton<RecurringService>();
            builder.Services.AddSingleton<AppLock>();

            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<TransactionsViewModel>();
            builder.Services.AddTransient<TransactionsPage>();
            builder.Services.AddTransient<BudgetsViewModel>();
            builder.Services.AddTransient<BudgetsPage>();
            builder.Services.AddTransient<TransactionEditViewModel>();
            builder.Services.AddTransient<TransactionEditPage>();
            builder.Services.AddTransient<CategoryEditViewModel>();
            builder.Services.AddTransient<CategoryEditPage>();
            builder.Services.AddTransient<AccountsViewModel>();
            builder.Services.AddTransient<AccountsPage>();
            builder.Services.AddTransient<AccountEditViewModel>();
            builder.Services.AddTransient<AccountEditPage>();
            builder.Services.AddTransient<MoreViewModel>();
            builder.Services.AddTransient<MorePage>();
            builder.Services.AddTransient<ReportsViewModel>();
            builder.Services.AddTransient<ReportsPage>();
            builder.Services.AddTransient<RecurringListViewModel>();
            builder.Services.AddTransient<RecurringPage>();
            builder.Services.AddTransient<TemplatesViewModel>();
            builder.Services.AddTransient<TemplatesPage>();
            builder.Services.AddTransient<GoalsViewModel>();
            builder.Services.AddTransient<GoalsPage>();
            builder.Services.AddTransient<GoalEditViewModel>();
            builder.Services.AddTransient<GoalEditPage>();
            builder.Services.AddTransient<DebtsViewModel>();
            builder.Services.AddTransient<DebtsPage>();
            builder.Services.AddTransient<DebtEditViewModel>();
            builder.Services.AddTransient<DebtEditPage>();
            builder.Services.AddTransient<BackupViewModel>();
            builder.Services.AddTransient<BackupPage>();
            builder.Services.AddTransient<SecurityViewModel>();
            builder.Services.AddTransient<SecurityPage>();
            builder.Services.AddTransient<LockViewModel>();
            builder.Services.AddTransient<LockPage>();

#if ANDROID
            // Inputs sit inside cards, so drop Android's default underline.
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) =>
                handler.PlatformView.BackgroundTintList =
                    Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent));
            Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) =>
                handler.PlatformView.BackgroundTintList =
                    Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent));
            Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) =>
                handler.PlatformView.FindViewById(Resource.Id.search_plate)?
                    .SetBackgroundColor(Android.Graphics.Color.Transparent));
#endif

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
