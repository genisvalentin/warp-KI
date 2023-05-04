using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Warp
{
    public class TiltSeriesDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate
            SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null && item is TiltSeries)
            {
                TiltSeries ts = item as TiltSeries;


                if (ts.Aretomo2PngStatus == jobStatus.Finished)
                    return
                        element.FindResource("TiltSeriesDataTemplate3") as DataTemplate;
                else if (ts.AretomoStatus == jobStatus.Failed || ts.NewstackStatus == jobStatus.Failed)
                    return
                        element.FindResource("TiltSeriesDataTemplate2") as DataTemplate;
                else
                    return
                        element.FindResource("TiltSeriesDataTemplate1") as DataTemplate;
            }

            return null;
        }
    }
}