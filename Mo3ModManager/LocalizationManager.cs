using System;
using System.Globalization;

namespace Mo3ModManager
{
    /// <summary>
    /// Manages the application's UI language: applying a language to the
    /// strongly-typed Resources class, and persisting the user's manual
    /// choice (if any) across restarts via the Settings.settings file.
    /// </summary>
    static class LocalizationManager
    {
        /// <summary>
        /// The list of languages the application ships translations for.
        /// The two-letter ISO code is used both for CultureInfo lookups and
        /// for persistence in user settings.
        /// </summary>
        public static readonly string[] SupportedLanguages = { "en", "zh" };

        /// <summary>
        /// Gets the two-letter ISO code of the language currently applied
        /// (i.e. what Mo3ModManager.Properties.Resources.Culture reflects),
        /// or null if no explicit language has been applied (auto-detect
        /// from system UI culture is in effect).
        /// </summary>
        public static string CurrentLanguage {
            get {
                var culture = Properties.Resources.Culture;
                return culture == null ? null : culture.TwoLetterISOLanguageName;
            }
        }

        /// <summary>
        /// Applies the given language code ("en", "zh", or null/empty for
        /// "follow system language") to the Resources class. Does not persist
        /// the choice; call SaveLanguagePreference separately if desired.
        /// </summary>
        public static void ApplyLanguage(string languageCode)
        {
            Properties.Resources.Culture = String.IsNullOrEmpty(languageCode)
                ? null
                : new CultureInfo(languageCode);
        }

        /// <summary>
        /// Loads the persisted language preference (if any) from user
        /// settings and applies it. Should be called once at startup, before
        /// any window is constructed.
        /// </summary>
        public static void LoadAndApplySavedLanguage()
        {
            string saved;
            try
            {
                saved = Properties.Settings.Default.Language;
            }
            catch (Exception)
            {
                // Corrupted or inaccessible user.config: fall back to auto-detect
                // rather than preventing the application from starting.
                saved = String.Empty;
            }
            ApplyLanguage(saved);
        }

        /// <summary>
        /// Persists the given language code ("en", "zh", or null/empty for
        /// "follow system language") so it is remembered across restarts.
        /// </summary>
        public static void SaveLanguagePreference(string languageCode)
        {
            try
            {
                Properties.Settings.Default.Language = languageCode ?? String.Empty;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                // Saving the preference is a nice-to-have; failing to persist
                // it should not crash the application or block the language
                // switch from taking effect for the current session.
                System.Diagnostics.Trace.WriteLine("[Warn] Failed to save language preference: " + ex.Message);
            }
        }

        /// <summary>
        /// Returns the display name (in its own language) for a supported
        /// language code, used to populate the language switch menu.
        /// </summary>
        public static string GetDisplayName(string languageCode)
        {
            switch (languageCode)
            {
                case "en":
                    return "English";
                case "zh":
                    return "简体中文";
                default:
                    return languageCode;
            }
        }
    }
}
