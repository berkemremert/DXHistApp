using DevExpress.Xpf.Charts;
using DevExpress.Charts.Designer;

namespace DXHistApp.Services
{
    public interface IChartService
    {
        void ShowDesigner(ChartControl chartControl);
        void SaveToFile(ChartControl chartControl, string fileName);
        void LoadFromFile(ChartControl chartControl, string fileName);
        void UpdateChart(ChartControl chartControl);
    }
}