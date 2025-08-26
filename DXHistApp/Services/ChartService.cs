using System;
using System.Windows;
using DevExpress.Xpf.Charts;
using DevExpress.Charts.Designer;
using Application = System.Windows.Application;

namespace DXHistApp.Services
{
    public class ChartService : IChartService
    {
        public void ShowDesigner(ChartControl chartControl)
        {
            var designer = new ChartDesigner(chartControl);
            designer.Show(Application.Current.MainWindow);
        }
   
        public void SaveToFile(ChartControl chartControl, string fileName)
        {
            chartControl.SaveToFile(fileName);
        }

        public void LoadFromFile(ChartControl chartControl, string fileName)
        {
            chartControl.LoadFromFile(fileName);
        }

        public void UpdateChart(ChartControl chartControl)
        {
            chartControl.UpdateData();
        }
    }
}
