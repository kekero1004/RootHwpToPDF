using System;
using System.Globalization;
using System.Windows.Data;

namespace HwpToPdf.Utils
{
    /// <summary>
    /// 문자열이 지정된 텍스트로 시작하는지 확인하는 컨버터
    /// </summary>
    public class StringStartsWithConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return false;
            }

            string strValue = value.ToString();
            string strParameter = parameter.ToString();

            return strValue.StartsWith(strParameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 