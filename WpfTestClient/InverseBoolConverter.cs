using System.Globalization;
using System.Windows.Data;

namespace WpfTestClient;

/// <summary>
/// IsBusy=true 일 때 컨트롤을 비활성화하기 위한 bool 반전 컨버터.
/// </summary>
[ValueConversion(typeof(bool), typeof(bool))]
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}
