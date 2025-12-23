using LTKCC.Data;
using LTKCC.Services;
using LTKCC.ViewModels;
using LTKCC.Views;
using Microsoft.Extensions.Logging;
using SQLitePCL;

namespace LTKCC;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		Batteries_V2.Init();

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<Data.AppDb>();
		builder.Services.AddSingleton<Data.DistributionListRepository>();
		builder.Services.AddSingleton<IAlertService, AlertService>();

		builder.Services.AddSingleton<IClientService, ClientService>();
		builder.Services.AddSingleton<ISupportedApplicationService, SupportedApplicationService>();

		builder.Services.AddTransient<ClientsViewModel>();
		builder.Services.AddTransient<ClientsPage>();
		builder.Services.AddTransient<SupportedApplicationsViewModel>();
		builder.Services.AddTransient<SupportedApplicationsPage>();

		builder.Services.AddTransient<SendGridSettingsViewModel>();
		builder.Services.AddTransient<SendGridSettingsPage>();

		builder.Services.AddTransient<DistributionListsViewModel>();
		builder.Services.AddTransient<DistributionListsPage>();

		builder.Services.AddSingleton<ITemplateFileService, TemplateFileService>();
		builder.Services.AddSingleton<TemplatesViewModel>();
		builder.Services.AddSingleton<TemplatesPage>();

		builder.Services.AddSingleton<IHtmlTemplateStore, HtmlTemplateStore>();
		builder.Services.AddSingleton<WorkflowStepDefinitionRepository>();

		builder.Services.AddTransient<WorkflowStepDefinitionsViewModel>();
		builder.Services.AddTransient<WorkflowStepDefinitionsPage>();

		builder.Services.AddTransient<LTKCC.ViewModels.WorkflowStepDefinitionDetailsViewModel>();
		builder.Services.AddTransient<LTKCC.Views.WorkflowStepDefinitionDetailsPage>();

		// builder.Services.AddTransient<WorkflowRunnerViewModel>();
		// builder.Services.AddTransient<WorkflowRunnerPage>();
#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
