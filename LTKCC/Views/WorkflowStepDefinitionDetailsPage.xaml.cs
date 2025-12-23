using LTKCC.ViewModels;

namespace LTKCC.Views;

[QueryProperty(nameof(DefinitionId), "id")]
public partial class WorkflowStepDefinitionDetailsPage : ContentPage
{
    private WorkflowStepDefinitionDetailsViewModel Vm => (WorkflowStepDefinitionDetailsViewModel)BindingContext;

    public WorkflowStepDefinitionDetailsPage(WorkflowStepDefinitionDetailsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    public string? DefinitionId
    {
        get => Vm.DefinitionId;
        set => Vm.DefinitionId = value;
    }
}
