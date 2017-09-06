using System;
using System.Globalization;
using System.Windows.Data;
using Amoeba.Messages;
using Amoeba.Service;

namespace Amoeba.Interface
{
    [ValueConversion(typeof(DownloadState), typeof(string))]
    class DownloadStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DownloadState type)
            {
                if (type == DownloadState.Downloading)
                {
                    return LanguagesManager.Instance.DownloadState_Downloading;
                }
                if (type == DownloadState.ParityDecoding)
                {
                    return LanguagesManager.Instance.DownloadState_ParityDecoding;
                }
                if (type == DownloadState.Decoding)
                {
                    return LanguagesManager.Instance.DownloadState_Decoding;
                }
                if (type == DownloadState.Completed)
                {
                    return LanguagesManager.Instance.DownloadState_Completed;
                }
                if (type == DownloadState.Error)
                {
                    return LanguagesManager.Instance.DownloadState_Error;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
