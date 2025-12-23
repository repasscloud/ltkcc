using CommunityToolkit.Mvvm.ComponentModel;

namespace LTKCC.ViewModels;

public sealed partial class TemplateFileItem : ObservableObject
{
    [ObservableProperty] private string fileName = "";
    [ObservableProperty] private bool isSelected;
}
