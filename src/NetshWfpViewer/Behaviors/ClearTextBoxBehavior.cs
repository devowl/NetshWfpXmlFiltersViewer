using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace NetshWfpViewer.Behaviors
{
    /// <summary>
    /// Behavior for a Button: clears the referenced TextBox on click.
    /// </summary>
    public class ClearTextBoxBehavior : Behavior<Button>
    {
        public static readonly DependencyProperty TextBoxProperty =
            DependencyProperty.Register(
                nameof(TextBox),
                typeof(TextBox),
                typeof(ClearTextBoxBehavior));

        public TextBox TextBox
        {
            get => (TextBox)GetValue(TextBoxProperty);
            set => SetValue(TextBoxProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Click += OnClick;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Click -= OnClick;
            base.OnDetaching();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            TextBox?.Clear();
        }
    }
}
