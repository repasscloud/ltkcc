using LTKCC.ViewModels;

namespace LTKCC.Views;

public partial class WorkflowStepDefinitionsPage : ContentPage
{
    private WorkflowStepDefinitionsViewModel Vm => (WorkflowStepDefinitionsViewModel)BindingContext;

    public WorkflowStepDefinitionsPage(WorkflowStepDefinitionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Vm.LoadAsync();
    }
}
