using System;
using System.Collections.Generic;

namespace SchedulingUI
{
    ///<summary>
    ///A helper alias to make this file easier to modify
    ///</summary>
    using Color = ConsoleColor;

    /// <summary>
    /// All possible color categories for a given scheme.
    /// </summary>
    /// <remarks>
    /// While all colors can be used as a foreground or background, these colors
    /// should be used as noted (via the suffix).
    /// </remarks>
    public enum ColorCategory
    {
        BACKGROUND,
        FOREGROUND,
        ERROR_FG,
        WARNING_FG,
        HIGHLIGHT_BG,
        HIGHLIGHT_BG_2
    }

    /// <summary>
    /// An easy-to-use wrapper for multiple colors.
    /// </summary>
    public class ColorScheme
    {
        static ColorScheme()
		{
            // A white foreground with a black background (windows CMD)
			RegisterScheme (new ColorScheme () {
				Background = Color.Black,
				Foreground = Color.White,
				ErrorForeground = Color.Red,
				WarningForeground = Color.Yellow,
				HighlightBackground = Color.DarkCyan,
                Highlight2Background = Color.Cyan,
				Name = "WhiteOnBlack"
			});

            // A black foreground with a white background (xterm)
			RegisterScheme (new ColorScheme () {
				Background = Color.White,
				Foreground = Color.Black,
				ErrorForeground = Color.Red,
				WarningForeground = Color.Yellow,
				HighlightBackground = Color.DarkCyan,
                Highlight2Background = Color.Cyan,
                Name = "BlackOnWhite"
			});
            
			SetCurrent ("WhiteOnBlack");

		}
        
        /// <summary>
        /// The current background.
        /// </summary>
		public static Color CurrentBackground { get{ return Current.Background; } }

        /// <summary>
        /// The current foreground.
        /// </summary>
        public static Color CurrentForeground { get{ return Current.Foreground; } }

        /// <summary>
        /// The current error foreground.
        /// </summary>
        public static Color CurrentErrorForeground { get{ return Current.ErrorForeground; } }

        /// <summary>
        /// The current warning foreground.
        /// </summary>
        public static Color CurrentWarningForeground { get{ return Current.WarningForeground; } }

        /// <summary>
        /// The current highlight background.
        /// </summary>
        public static Color CurrentHighlightBackground { get{ return Current.HighlightBackground; } }

        /// <summary>
        /// The current highlight background #2.
        /// </summary>
        public static Color CurrentHighlight2Background { get { return Current.Highlight2Background; } }

        /// <summary>
        /// Gets the current ColorScheme
        /// </summary>
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
        
        private static void RegisterScheme(ColorScheme scheme)
        {
            schemes.Add(scheme);
        }
        
        /// <summary>
        /// Sets the current ColorScheme by name.
        /// </summary>
        /// <param name="name">The requested scheme's name</param>
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

        /// <summary>
        /// Gets the color for a specific color scheme.
        /// </summary>
        /// <param name="cat">The category</param>
        /// <returns></returns>
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
                    case ColorCategory.HIGHLIGHT_BG_2:
                        return Highlight2Background;
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
        public Color Highlight2Background { get; set; }
        
        /// <summary>
        /// This ColorScheme's name.
        /// </summary>
        public string Name { get; set; }
    }
}
