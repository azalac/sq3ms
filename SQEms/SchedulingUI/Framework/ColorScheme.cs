using System;
using System.Collections.Generic;

namespace SchedulingUI
{
    using Color = ConsoleColor;

    public enum ColorCategory
    {
        BACKGROUND,
        FOREGROUND,
        ERROR_FG,
        WARNING_FG,
        HIGHLIGHT_BG
    }

    public class ColorScheme
    {
        static ColorScheme()
        {
            RegisterScheme(new ColorScheme()
            {
                Background = Color.Black,
                Foreground = Color.White,
                ErrorForeground = Color.Red,
                WarningForeground = Color.Yellow,
                HighlightBackground = Color.Gray,
                Name = "WhiteOnBlack"
            });

            RegisterScheme(new ColorScheme()
            {
                Background = Color.White,
                Foreground = Color.Black,
                ErrorForeground = Color.Red,
                WarningForeground = Color.Yellow,
                HighlightBackground = Color.Gray,
                Name = "BlackOnWhite"
            });
            
			SetCurrent ("BlackOnWhite");

        }

		public static Color CurrentBackground { get{ return Current.Background; } }
		public static Color CurrentForeground { get{ return Current.Foreground; } }
		public static Color CurrentErrorForeground { get{ return Current.ErrorForeground; } }
		public static Color CurrentWarningForeground { get{ return Current.WarningForeground; } }
		public static Color CurrentHighlightBackground { get{ return Current.HighlightBackground; } }

        public static ColorScheme Current {
            get
            {
                return schemes[selectedScheme];
            }

            set
            {
                selectedScheme = schemes.IndexOf(value);
            }
        }

        private static int selectedScheme = 0;

        private static List<ColorScheme> schemes = new List<ColorScheme>();

        public static void RegisterScheme(ColorScheme scheme)
        {
            schemes.Add(scheme);
        }
        
        public static void SetCurrent(string name)
        {
            for(int i = 0; i < schemes.Count; i++)
            {
                if(schemes[i].Name == name)
                {
                    selectedScheme = i;
                    return;
                }
            }
            throw new ArgumentException("Could not find ColorScheme " + name);
        }

        public Color this[ColorCategory cat]
        {
            get
            {
                switch (cat)
                {
                    case ColorCategory.BACKGROUND:
                        return Background;
                    case ColorCategory.FOREGROUND:
                        return Foreground;
                    case ColorCategory.ERROR_FG:
                        return ErrorForeground;
                    case ColorCategory.WARNING_FG:
                        return WarningForeground;
                    case ColorCategory.HIGHLIGHT_BG:
                        return HighlightBackground;
                    default:
                        throw new ArgumentException("Bad color category " + cat);
                }
            }
        }

        public Color Background { get; set; }
        public Color Foreground { get; set; }
        public Color ErrorForeground { get; set; }
        public Color WarningForeground { get; set; }
        public Color HighlightBackground { get; set; }
        public string Name { get; set; }
    }
}
