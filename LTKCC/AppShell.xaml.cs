namespace LTKCC;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute("workflow-step-definition-details", typeof(Views.WorkflowStepDefinitionDetailsPage));
	}
}
