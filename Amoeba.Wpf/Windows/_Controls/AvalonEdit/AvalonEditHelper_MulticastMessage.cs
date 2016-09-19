using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Library.Net.Amoeba;
using Amoeba.Properties;

namespace Amoeba.Windows
{
    class AvalonEditHelper_MulticastMessage : AvalonEditHelperBase
    {
        private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;

        public void Setup(TextEditor textEditor)
        {
            textEditor.TextArea.TextView.Options.EnableEmailHyperlinks = false;
            textEditor.TextArea.TextView.Options.EnableHyperlinks = false;

            textEditor.TextArea.TextView.LineTransformers.Clear();

            textEditor.FontFamily = new FontFamily(Settings.Instance.Global_Fonts_Message_FontFamily);
            textEditor.FontSize = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_Message_FontSize + "pt");

            textEditor.TextArea.Caret.CaretBrush = Brushes.Transparent;
        }

        public void Clear(TextEditor textEditor)
        {
            _ranges.Clear();

            textEditor.Document.BeginUpdate();

            textEditor.Document.Text = "";
            textEditor.CaretOffset = 0;
            textEditor.SelectionLength = 0;
            textEditor.TextArea.TextView.ElementGenerators.Clear();
            textEditor.ScrollToHome();

            textEditor.Document.EndUpdate();
        }

        private readonly List<CustomElementRange> _ranges = new List<CustomElementRange>();

        public IEnumerable<int> SelectIndexes(TextEditor textEditor)
        {
            int start = 0;
            int end = 0;

            if (textEditor.SelectionLength != 0)
            {
                start = textEditor.SelectionStart;
                end = textEditor.SelectionStart + textEditor.SelectionLength;
            }
            else
            {
                var position = textEditor.GetPositionFromPoint(MouseUtils.GetMousePosition(textEditor));

                if (position != null)
                {
                    start = textEditor.Document.GetOffset(position.Value.Line, position.Value.Column);
                    end = start;
                }
            }

            var list = new List<int>();

            for (int i = 0; i < _ranges.Count; i++)
            {
                var range = _ranges[i];
                if ((range.Start <= start && range.End <= start) || (range.Start >= end && range.End >= end)) continue;

                list.Add(i);
            }

            return list;
        }

        public void Set(TextEditor textEditor, IEnumerable<MulticastMessageViewModel> collection)
        {
            _ranges.Clear();

            var document = new StringBuilder();
            var settings = new List<CustomElementSetting>();

            foreach (var target in collection)
            {
                int startOffset = document.Length;

                {
                    string item1;
                    if (target.Option.State.HasFlag(MulticastMessageState.IsLocked)) item1 = "#";
                    else if (target.Option.State.HasFlag(MulticastMessageState.IsUnread)) item1 = "!";
                    else item1 = "@";

                    var item2 = target.Item.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                    var item3 = target.Item.Signature;

                    {
                        settings.Add(new CustomElementSetting("State", document.Length, item1.Length));
                        document.Append(item1);
                        document.Append(" ");

                        settings.Add(new CustomElementSetting("CreationTime", document.Length, item2.Length));
                        document.Append(item2);
                        document.Append(" - ");

                        settings.Add(new CustomElementSetting("Signature", document.Length, item3.Length));
                        document.Append(item3);

                        if (!Inspect.ContainTrustSignature(target.Item.Signature))
                        {
                            document.Append(" +");
                            document.Append(target.Option.Cost);
                        }
                    }

                    document.AppendLine();
                }

                {
                    document.AppendLine();
                }

                {
                    foreach (var line in target.Item.Comment
                        .Trim('\r', '\n')
                        .Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                        .Take(128)
                        .Select(n => this.Regularization(n)))
                    {
                        foreach (var match in _uriRegexes.Select(n => n.Matches(line)).SelectMany(n => n.OfType<Match>()))
                        {
                            settings.Add(new CustomElementSetting("Uri", document.Length + match.Index, match.Length));
                        }

                        document.AppendLine(line);
                    }
                }

                {
                    document.AppendLine();
                }

                _ranges.Add(new CustomElementRange(startOffset, document.Length));
            }

            if (document.Length >= 2) document.Remove(document.Length - 2, 2);

            textEditor.Document.BeginUpdate();

            textEditor.Document.Text = "";
            textEditor.CaretOffset = 0;
            textEditor.SelectionLength = 0;
            textEditor.TextArea.TextView.ElementGenerators.Clear();
            textEditor.ScrollToHome();

            textEditor.Document.Text = document.ToString();

            var elementGenerator = new CustomElementGenerator(settings);
            elementGenerator.ClickEvent += (string text) => this.OnClickEvent(text);
            elementGenerator.SelectEvent += (CustomElementRange range) => textEditor.Select(range.Start, range.End - range.Start);
            textEditor.TextArea.TextView.ElementGenerators.Add(elementGenerator);

            textEditor.Document.EndUpdate();

            textEditor.CaretOffset = textEditor.Document.Text.Length;
            textEditor.TextArea.Caret.BringCaretToView();
            textEditor.ScrollToEnd();
        }

        public void Set(TextEditor textEditor, DateTime creationTime, string signature, int cost, string comment)
        {
            var document = new StringBuilder();
            var settings = new List<CustomElementSetting>();

            {
                {
                    var item1 = "#";
                    var item2 = creationTime.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                    var item3 = signature;

                    {
                        settings.Add(new CustomElementSetting("State", document.Length, item1.Length));
                        document.Append(item1);
                        document.Append(" ");

                        settings.Add(new CustomElementSetting("CreationTime", document.Length, item2.Length));
                        document.Append(item2);
                        document.Append(" - ");

                        settings.Add(new CustomElementSetting("Signature", document.Length, item3.Length));
                        document.Append(item3);

                        if (!Inspect.ContainTrustSignature(signature))
                        {
                            document.Append(" - ");
                            document.Append(cost);
                        }
                    }

                    document.AppendLine();
                }

                {
                    document.AppendLine();
                }

                {
                    foreach (var line in comment
                        .Trim('\r', '\n')
                        .Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                        .Take(128)
                        .Select(n => this.Regularization(n)))
                    {
                        foreach (var match in _uriRegexes.Select(n => n.Matches(line)).SelectMany(n => n.OfType<Match>()))
                        {
                            settings.Add(new CustomElementSetting("Uri", document.Length + match.Index, match.Length));
                        }

                        document.AppendLine(line);
                    }
                }

                {
                    document.AppendLine();
                }
            }

            if (document.Length >= 2) document.Remove(document.Length - 2, 2);

            textEditor.Document.BeginUpdate();

            textEditor.Document.Text = "";
            textEditor.CaretOffset = 0;
            textEditor.SelectionLength = 0;
            textEditor.TextArea.TextView.ElementGenerators.Clear();

            textEditor.Document.Text = document.ToString();

            var elementGenerator = new CustomElementGenerator(settings);
            elementGenerator.ClickEvent += (string text) => this.OnClickEvent(text);
            elementGenerator.SelectEvent += (CustomElementRange range) => textEditor.Select(range.Start, range.End - range.Start);
            textEditor.TextArea.TextView.ElementGenerators.Add(new CustomElementGenerator(settings));

            textEditor.Document.EndUpdate();
        }

        class CustomElementGenerator : AbstractCustomElementGenerator
        {
            private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;

            public CustomElementGenerator(IEnumerable<CustomElementSetting> settings)
                : base(settings)
            {

            }

            public override VisualLineElement ConstructElement(int offset)
            {
                var result = this.FindMatch(offset);

                if (result != null)
                {
                    if (result.Type == "State")
                    {
                        var size = (double)new FontSizeConverter().ConvertFromString(Settings.Instance.Global_Fonts_Message_FontSize + "pt");

                        var image = new Image() { Height = (size - 3), Width = (size - 3), Margin = new Thickness(1.5, 1.5, 0, 0) };
                        if (result.Value == "#") image.Source = StatesIconManager.Instance.Green;
                        else if (result.Value == "!") image.Source = StatesIconManager.Instance.Red;
                        else if (result.Value == "@") image.Source = StatesIconManager.Instance.Yello;

                        var element = new CustomObjectElement(result.Value, image);

                        element.ClickEvent += (string text) =>
                        {
                            this.OnSelectEvent(new CustomElementRange(offset, offset + result.Value.Length));
                        };

                        return element;
                    }
                    else if (result.Type == "Signature")
                    {
                        Brush brush;

                        if (Inspect.ContainTrustSignature(result.Value)) brush = new SolidColorBrush(_serviceManager.Config.Colors.Message_Trust);
                        else brush = new SolidColorBrush(_serviceManager.Config.Colors.Message_Untrust);

                        var element = new CustomTextElement(result.Value);
                        element.Foreground = brush;

                        element.ClickEvent += (string text) =>
                        {
                            this.OnSelectEvent(new CustomElementRange(offset, offset + result.Value.Length));
                        };

                        return element;
                    }
                    else if (result.Type == "Uri")
                    {
                        var uri = result.Value;

                        CustomObjectElement element = null;

                        if (uri.StartsWith("http:") | uri.StartsWith("https:"))
                        {
                            var textBlock = new TextBlock();
                            textBlock.Text = uri.Substring(0, Math.Min(64, uri.Length)) + ((uri.Length > 64) ? "..." : "");
                            textBlock.ToolTip = HttpUtility.UrlDecode(uri);

                            if (Settings.Instance.Global_UrlHistorys.Contains(uri)) textBlock.Foreground = new SolidColorBrush(_serviceManager.Config.Colors.Link);
                            else textBlock.Foreground = new SolidColorBrush(_serviceManager.Config.Colors.Link_New);

                            element = new CustomObjectElement(uri, textBlock);
                        }
                        else if (uri.StartsWith("Tag:"))
                        {
                            var tag = AmoebaConverter.FromTagString(uri);

                            var textBlock = new TextBlock();
                            textBlock.Text = uri.Substring(0, Math.Min(64, uri.Length)) + ((uri.Length > 64) ? "..." : "");
                            textBlock.ToolTip = MessageConverter.ToInfoMessage(tag);

                            if (Settings.Instance.Global_TagHistorys.Contains(tag)) textBlock.Foreground = new SolidColorBrush(_serviceManager.Config.Colors.Link);
                            else textBlock.Foreground = new SolidColorBrush(_serviceManager.Config.Colors.Link_New);

                            element = new CustomObjectElement(uri, textBlock);
                        }
                        else if (uri.StartsWith("Tag:"))
                        {
                            var seed = AmoebaConverter.FromTagString(uri);

                            var textBlock = new TextBlock();
                            textBlock.Text = uri.Substring(0, Math.Min(64, uri.Length)) + ((uri.Length > 64) ? "..." : "");
                            textBlock.ToolTip = MessageConverter.ToInfoMessage(seed);

                            if (Settings.Instance.Global_TagHistorys.Contains(seed)) textBlock.Foreground = new SolidColorBrush(_serviceManager.Config.Colors.Link);
                            else textBlock.Foreground = new SolidColorBrush(_serviceManager.Config.Colors.Link_New);

                            element = new CustomObjectElement(uri, textBlock);
                        }

                        if (element != null)
                        {
                            element.ClickEvent += (string text) =>
                            {
                                this.OnSelectEvent(new CustomElementRange(offset, offset + result.Value.Length));
                                this.OnClickEvent(text);
                            };

                            return element;
                        }
                    }
                }

                return null;
            }

            private class StatesIconManager
            {
                private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;

                StatesIconManager()
                {
                    {
                        var bitmap = new BitmapImage();

                        bitmap.BeginInit();
                        bitmap.StreamSource = new FileStream(Path.Combine(_serviceManager.Paths["Icons"], @"States\Red.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                        bitmap.EndInit();
                        if (bitmap.CanFreeze) bitmap.Freeze();

                        this.Red = bitmap;
                    }

                    {
                        var bitmap = new BitmapImage();

                        bitmap.BeginInit();
                        bitmap.StreamSource = new FileStream(Path.Combine(_serviceManager.Paths["Icons"], @"States\Yello.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                        bitmap.EndInit();
                        if (bitmap.CanFreeze) bitmap.Freeze();

                        this.Yello = bitmap;
                    }

                    {
                        var bitmap = new BitmapImage();

                        bitmap.BeginInit();
                        bitmap.StreamSource = new FileStream(Path.Combine(_serviceManager.Paths["Icons"], @"States\Green.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                        bitmap.EndInit();
                        if (bitmap.CanFreeze) bitmap.Freeze();

                        this.Green = bitmap;
                    }
                }

                private static StatesIconManager _instance = null;

                public static StatesIconManager Instance
                {
                    get
                    {
                        if (_instance == null) _instance = new StatesIconManager();
                        return _instance;
                    }
                }

                public ImageSource Red { get; private set; }
                public ImageSource Yello { get; private set; }
                public ImageSource Green { get; private set; }
            }
        }
    }
}
