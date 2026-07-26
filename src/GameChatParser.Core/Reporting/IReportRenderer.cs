namespace GameChatParser.Core.Reporting;

/// <summary>Writes a report out in some presentation format.</summary>
public interface IReportRenderer
{
    void Render(Report report, TextWriter writer);
}
