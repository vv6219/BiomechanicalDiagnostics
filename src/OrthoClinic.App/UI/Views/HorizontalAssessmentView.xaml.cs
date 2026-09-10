namespace OrthoClinic.UI.Views;

using OrthoClinic.UI.ViewModels;

public partial class HorizontalAssessmentView : ContentPage
{
    public HorizontalAssessmentView(HorizontalTestsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
