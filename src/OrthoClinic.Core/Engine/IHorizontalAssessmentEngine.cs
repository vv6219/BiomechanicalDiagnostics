namespace OrthoClinic.Core.Engine;

using OrthoClinic.Core.Domain;

/// <summary>
/// Интерфейс алгоритмического движка оценки горизонтальных биомеханических тестов.
/// </summary>
public interface IHorizontalAssessmentEngine
{
    HorizontalDiagnosticReport Evaluate(HorizontalInputData input);
}
