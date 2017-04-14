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

namespace Amoeba.Interface
{
    delegate void AvalonEditClickEventHandler(string text);

    abstract class CustomAvalonEditHelperBase
    {
        protected delegate void SelectEventHandler(CustomElementRange range);

        protected static readonly Regex[] _uriRegexes;

        static CustomAvalonEditHelperBase()
        {
            var uriRegexes = new List<Regex>();
            uriRegexes.Add(new Regex(@"http(s)?://(\S)+", RegexOptions.Compiled));
            uriRegexes.Add(new Regex(@"Tag:[A-Za-z0-9\-_]+", RegexOptions.Compiled));
            uriRegexes.Add(new Regex(@"Seed:[A-Za-z0-9\-_]+", RegexOptions.Compiled));

            _uriRegexes = uriRegexes.ToArray();
        }

        public event AvalonEditClickEventHandler ClickEvent;

        protected virtual void OnClickEvent(string text)
        {
            this.ClickEvent?.Invoke(text);
        }

        protected abstract class AbstractCustomElementGenerator : VisualLineElementGenerator
        {
            private List<CustomElementSetting> _settings = new List<CustomElementSetting>();

            public AbstractCustomElementGenerator(IEnumerable<CustomElementSetting> settings)
            {
                _settings.AddRange(settings);
                _settings.Sort((x, y) => x.Offset.CompareTo(y.Offset));
            }

            public event AvalonEditClickEventHandler ClickEvent;

            protected virtual void OnClickEvent(string text)
            {
                this.ClickEvent?.Invoke(text);
            }

            public event SelectEventHandler SelectEvent;

            protected virtual void OnSelectEvent(CustomElementRange range)
            {
                this.SelectEvent?.Invoke(range);
            }

            public override int GetFirstInterestedOffset(int startOffset)
            {
                foreach (var target in _settings)
                {
                    if (startOffset <= target.Offset)
                    {
                        return target.Offset;
                    }
                }

                return -1;
            }

            protected Result FindMatch(int startOffset)
            {
                foreach (var target in _settings)
                {
                    if (target.Offset == startOffset)
                    {
                        TextDocument document = this.CurrentContext.Document;
                        string relevantText = document.GetText(target.Offset, target.Count);

                        return new Result(target.Type, relevantText);
                    }
                }

                return null;
            }

            protected class Result
            {
                public Result(string type, string value)
                {
                    this.Type = type;
                    this.Value = value;
                }

                public string Type { get; private set; }
                public string Value { get; private set; }
            }

            protected class CustomTextElement : FormattedTextElement
            {
                public CustomTextElement(string text)
                    : base(text, text.Length)
                {
                    this.Text = text;
                }

                public event AvalonEditClickEventHandler ClickEvent;

                protected virtual void OnClickEvent(string text)
                {
                    this.ClickEvent?.Invoke(text);
                }

                public string Text { get; set; }
                public Brush Foreground { get; set; }

                public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
                {
                    this.TextRunProperties.SetForegroundBrush(this.Foreground);

                    return base.CreateTextRun(startVisualColumn, context);
                }

                protected override void OnQueryCursor(QueryCursorEventArgs e)
                {
                    e.Handled = true;
                    e.Cursor = Cursors.Hand;
                }

                protected override void OnMouseDown(MouseButtonEventArgs e)
                {
                    if (e.LeftButton != MouseButtonState.Pressed) return;

                    e.Handled = true;
                    this.OnClickEvent(this.Text);
                }
            }

            protected class CustomObjectElement : InlineObjectElement
            {
                public CustomObjectElement(string text, UIElement element)
                    : base(text.Length, element)
                {
                    this.Text = text;
                }

                public event AvalonEditClickEventHandler ClickEvent;

                protected virtual void OnClickEvent(string text)
                {
                    this.ClickEvent?.Invoke(text);
                }

                public string Text { get; set; }

                public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
                {
                    return base.CreateTextRun(startVisualColumn, context);
                }

                protected override void OnQueryCursor(QueryCursorEventArgs e)
                {
                    e.Handled = true;
                    e.Cursor = Cursors.Hand;
                }

                protected override void OnMouseDown(MouseButtonEventArgs e)
                {
                    if (e.LeftButton != MouseButtonState.Pressed) return;

                    e.Handled = true;
                    this.OnClickEvent(this.Text);
                }
            }
        }

        protected struct CustomElementSetting
        {
            private string _type;
            private int _offset;
            private int _count;

            public CustomElementSetting(string type, int offset, int count)
            {
                _type = type;
                _offset = offset;
                _count = count;
            }

            public string Type { get { return _type; } }
            public int Offset { get { return _offset; } }
            public int Count { get { return _count; } }
        }

        protected struct CustomElementRange
        {
            private int _start;
            private int _end;

            public CustomElementRange(int start, int end)
            {
                _start = start;
                _end = (_start > end) ? _start : end;
            }

            public int Start { get { return _start; } }
            public int End { get { return _end; } }
        }
    }
}
