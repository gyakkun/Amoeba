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
using Amoeba.Service;
using Omnius.Security;

namespace Amoeba.Interface
{
    class AvalonEditChatMessagesHelper : CustomAvalonEditHelperBase
    {
        public static ICommand GetCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(CommandProperty);
        }

        public static void SetCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CommandProperty, value);
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(AvalonEditChatMessagesHelper), new PropertyMetadata(null));

        public static int GetMessages(DependencyObject obj)
        {
            return (int)obj.GetValue(MessagesProperty);
        }

        public static void SetMessages(DependencyObject obj, int value)
        {
            obj.SetValue(MessagesProperty, value);
        }

        public static readonly DependencyProperty MessagesProperty =
            DependencyProperty.RegisterAttached("Messages", typeof(ChatMessageInfo[]), typeof(AvalonEditChatMessagesHelper),
                new UIPropertyMetadata(
                    null,
                    (o, e) =>
                    {
                        var textEditor = o as TextEditor;
                        if (textEditor == null) return;

                        Clear(textEditor);
                        Setup(textEditor);

                        var messages = e.NewValue as ChatMessageInfo[];
                        if (messages == null) return;

                        Set(textEditor, messages);
                    }
                )
            );

        private static void Setup(TextEditor textEditor)
        {
            textEditor.TextArea.TextView.Options.EnableEmailHyperlinks = false;
            textEditor.TextArea.TextView.Options.EnableHyperlinks = false;

            textEditor.TextArea.TextView.LineTransformers.Clear();

            textEditor.TextArea.Caret.CaretBrush = Brushes.Transparent;
        }

        private static void Clear(TextEditor textEditor)
        {
            textEditor.Document.BeginUpdate();

            textEditor.Document.Text = "";
            textEditor.CaretOffset = 0;
            textEditor.SelectionLength = 0;
            textEditor.TextArea.TextView.ElementGenerators.Clear();
            textEditor.ScrollToHome();

            textEditor.Document.EndUpdate();
        }

        private static void Set(TextEditor textEditor, IEnumerable<ChatMessageInfo> collection)
        {
            var document = new StringBuilder();
            var settings = new List<CustomElementSetting>();

            foreach (var target in collection)
            {
                int startOffset = document.Length;

                {
                    string item1 = "@";

                    var item2 = target.Message.CreationTime.ToLocalTime().ToString(LanguagesManager.Instance.Global_DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                    var item3 = target.Message.AuthorSignature.ToString();

                    {
                        settings.Add(new CustomElementSetting("State", document.Length, item1.Length));
                        document.Append(item1);
                        document.Append(" ");

                        settings.Add(new CustomElementSetting("CreationTime", document.Length, item2.Length));
                        document.Append(item2);
                        document.Append(" - ");

                        settings.Add(new CustomElementSetting("Signature", document.Length, item3.Length));
                        document.Append(item3);

                        if (!Inspect.ContainTrustSignature(target.Message.AuthorSignature) && target.Message.Cost != null)
                        {
                            document.Append(" +");
                            document.Append(target.Message.Cost.Value);
                        }
                    }

                    document.AppendLine();
                }

                {
                    document.AppendLine();
                }

                {
                    foreach (var line in target.Message.Value.Comment
                        .Trim('\r', '\n')
                        .Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                        .Take(128))
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
            textEditor.ScrollToHome();

            textEditor.Document.Text = document.ToString();

            var elementGenerator = new CustomElementGenerator(settings);
            elementGenerator.SelectEvent += (CustomElementRange range) => textEditor.Select(range.Start, range.End - range.Start);
            elementGenerator.ClickEvent += (string text) =>
            {
                var command = GetCommand(textEditor);
                if (command != null)
                {
                    if (command.CanExecute(text))
                    {
                        command.Execute(text);
                    }
                }
            };
            textEditor.TextArea.TextView.ElementGenerators.Add(elementGenerator);

            textEditor.Document.EndUpdate();

            textEditor.CaretOffset = textEditor.Document.Text.Length;
            textEditor.TextArea.Caret.BringCaretToView();
            textEditor.ScrollToEnd();
        }

        class CustomElementGenerator : AbstractCustomElementGenerator
        {
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
                        var image = new Image();
                        if (result.Value == "#") image.Source = AmoebaEnvironment.Icons.GreenIcon;
                        else if (result.Value == "!") image.Source = AmoebaEnvironment.Icons.RedIcon;
                        else if (result.Value == "@") image.Source = AmoebaEnvironment.Icons.YelloIcon;

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

                        if (Inspect.ContainTrustSignature(Signature.Parse(result.Value)))
                        {
                            brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(AmoebaEnvironment.Config.Color.Message_Trust));
                        }
                        else
                        {
                            brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(AmoebaEnvironment.Config.Color.Message_Untrust));
                        }

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

                            element = new CustomObjectElement(uri, textBlock);
                        }
                        else if (uri.StartsWith("Tag:"))
                        {
                            var tag = AmoebaConverter.FromTagString(uri);

                            var textBlock = new TextBlock();
                            textBlock.Text = uri.Substring(0, Math.Min(64, uri.Length)) + ((uri.Length > 64) ? "..." : "");

                            element = new CustomObjectElement(uri, textBlock);
                        }
                        else if (uri.StartsWith("Seed:"))
                        {
                            var seed = AmoebaConverter.FromSeedString(uri);

                            var textBlock = new TextBlock();
                            textBlock.Text = uri.Substring(0, Math.Min(64, uri.Length)) + ((uri.Length > 64) ? "..." : "");

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
        }
    }
}
