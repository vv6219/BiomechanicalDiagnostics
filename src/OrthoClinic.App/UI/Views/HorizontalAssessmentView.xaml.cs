namespace OrthoClinic.UI.Views;

using OrthoClinic.UI.ViewModels;
using OrthoClinic.UI.Drawables;

public partial class HorizontalAssessmentView : ContentPage
{
    private readonly LegLengthDiscrepancyDrawable _legDrawable = new();
    private readonly TibialTorsionPalimpsestDrawable _tibialDrawable = new();
    private readonly AdaptiveWedgeDrawable _wedgeDrawable = new();
    private readonly PlantarInsoleMapDrawable _plantarDrawable = new();

    public HorizontalAssessmentView() 
        : this(IPlatformApplication.Current?.Services.GetRequiredService<HorizontalTestsViewModel>()!)
    {
    }

    public HorizontalAssessmentView(HorizontalTestsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        LegsGraphicsView.Drawable = _legDrawable;
        TibialGraphicsView.Drawable = _tibialDrawable;
        AdaptiveWedgeGraphicsView.Drawable = _wedgeDrawable;
        PlantarMapGraphicsView.Drawable = _plantarDrawable;

        // Initialize state
        _legDrawable.LeftOffset = viewModel.LeftMalleolus;
        _legDrawable.RightOffset = viewModel.RightMalleolus;
        _tibialDrawable.State = viewModel.TibialState;
        _wedgeDrawable.HasConflict = viewModel.EvaluationResult?.IsComplexTorsionConflict ?? false;
        _wedgeDrawable.Phase = viewModel.WavePhase;
        _plantarDrawable.Prescriptions = viewModel.EvaluationResult?.Prescriptions;

        viewModel.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(HorizontalTestsViewModel.LeftMalleolus):
                case nameof(HorizontalTestsViewModel.RightMalleolus):
                    _legDrawable.LeftOffset = viewModel.LeftMalleolus;
                    _legDrawable.RightOffset = viewModel.RightMalleolus;
                    LegsGraphicsView.Invalidate();
                    break;

                case nameof(HorizontalTestsViewModel.TibialState):
                    _tibialDrawable.State = viewModel.TibialState;
                    TibialGraphicsView.Invalidate();
                    break;

                case nameof(HorizontalTestsViewModel.WavePhase):
                    _wedgeDrawable.Phase = viewModel.WavePhase;
                    AdaptiveWedgeGraphicsView.Invalidate();
                    break;

                case nameof(HorizontalTestsViewModel.EvaluationResult):
                    _wedgeDrawable.HasConflict = viewModel.EvaluationResult?.IsComplexTorsionConflict ?? false;
                    _plantarDrawable.Prescriptions = viewModel.EvaluationResult?.Prescriptions;
                    AdaptiveWedgeGraphicsView.Invalidate();
                    PlantarMapGraphicsView.Invalidate();
                    break;
            }
        };
    }
}
