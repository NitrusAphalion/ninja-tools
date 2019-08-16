using Stylet;

namespace NinjaTools
{
    public class Globals : PropertyChangedBase
    {
        public static   Globals Static { get; } = new Globals();
        public          double  EditorFontSize
        {
            get => editorFontSize;
            set
            {
                if (editorFontSize == value)
                    return;
                editorFontSize = value;
                NotifyOfPropertyChange(nameof(EditorFontSize));
            }
        }

        public const double EditorFontMaxSize = 60d;
        public const double EditorFontMinSize = 5d;

        private double editorFontSize = 13.333;

        // Use singleton pattern since we can't make it static if we want to implement INotifyPropertyChanged for two way binding
        private Globals() { }
    }
}