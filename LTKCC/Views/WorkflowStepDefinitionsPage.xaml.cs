// File: Views/WorkflowStepDefinitionsPage.xaml.cs
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

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Keep OnAppearing non-async.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Vm.LoadAsync();
        });
    }
}
