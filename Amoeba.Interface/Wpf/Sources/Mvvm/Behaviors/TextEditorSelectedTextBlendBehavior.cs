using System.Windows;
using System.Windows.Interactivity;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;

namespace Amoeba.Interface
{
    public sealed class TextEditorSelectedTextBlendBehavior : Behavior<TextEditor>
    {
        public string SelectedText
        {
            get { return (string)GetValue(SelectedTextProperty); }
            set { SetValue(SelectedTextProperty, value); }
        }

        public static readonly DependencyProperty SelectedTextProperty =
            DependencyProperty.Register("SelectedText", typeof(string), typeof(TextEditorSelectedTextBlendBehavior), new PropertyMetadata(null));

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.TextArea.SelectionChanged += this.TextArea_SelectionChanged;
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.TextArea.SelectionChanged -= this.TextArea_SelectionChanged;
            base.OnDetaching();
        }

        private void TextArea_SelectionChanged(object sender, System.EventArgs e)
        {
            if (sender is TextArea textArea)
            {
                this.SelectedText = textArea.Selection.GetText();
            }
        }
    }
}
