using System;
using System.Collections.Generic;
using System.Globalization;

namespace EnhancedCommands.System.Windows.Input.DialogCommands
{
    public class OkCancelTextProvider
    {
        private static readonly object Sync = new object();

        private static string cachedKey;
        private static (string ok, string cancel) cachedTexts;

        public static (string ok, string cancel) Get(CultureInfo culture)
        {
            // 캐시 키: CultureInfo.Name (예: "ko-KR", "zh-Hant-TW")
            var key = (culture != null && !string.IsNullOrEmpty(culture.Name)) ? culture.Name : "en";

            // fast path
            if(string.Equals(key, cachedKey, StringComparison.OrdinalIgnoreCase))
                return cachedTexts;

            lock(Sync)
            {
                if(string.Equals(key, cachedKey, StringComparison.OrdinalIgnoreCase))
                    return cachedTexts;

                (string ok, string cancel) v;

                // 1) full culture name exact match
                if(Table.TryGetValue(key, out v))
                {
                    cachedTexts = v;
                    cachedKey = key;
                    return v;
                }

                // 2) progressively strip subtags: zh-Hant-TW -> zh-Hant -> zh
                var probe = key;
                while(true)
                {
                    var dash = probe.LastIndexOf('-');
                    if(dash <= 0) break;

                    probe = probe.Substring(0, dash);
                    if(Table.TryGetValue(probe, out v))
                    {
                        cachedTexts = v;
                        cachedKey = key; // 원래 key로 캐시
                        return v;
                    }
                }

                // 3) TwoLetter fallback (ko-KR -> ko), for cases where Name is empty/odd
                var lang = (culture != null && !string.IsNullOrEmpty(culture.TwoLetterISOLanguageName))
                    ? culture.TwoLetterISOLanguageName: "en";

                if(Table.TryGetValue(lang, out v))
                {
                    cachedTexts = v;
                    cachedKey = key;
                    return v;
                }

                // 4) default
                v = Table["en"];
                cachedTexts = v;
                cachedKey = key;
                return v;
            }
        }


        private static readonly Dictionary<string, (string ok, string cancel)> Table =
            new Dictionary<string, (string ok, string cancel)>(StringComparer.OrdinalIgnoreCase)
            {
                // ---------------------------------------------------------------------
                // Default / Latin baseline
                // ---------------------------------------------------------------------
                ["en"] = ("OK", "Cancel"),

                // ---------------------------------------------------------------------
                // CJK
                // ---------------------------------------------------------------------
                ["ko"] = ("확인", "취소"),
                ["ja"] = ("OK", "キャンセル"),

                // Chinese: base + script + region safety
                // Base zh is treated as Simplified by default.
                ["zh"] = ("确定", "取消"),
                ["zh-hans"] = ("确定", "取消"),
                ["zh-hant"] = ("確定", "取消"),

                // Region → Traditional (critical safety)
                ["zh-tw"] = ("確定", "取消"),
                ["zh-hk"] = ("確定", "取消"),
                ["zh-mo"] = ("確定", "取消"),

                // Region → Simplified (optional but explicit)
                ["zh-cn"] = ("确定", "取消"),
                ["zh-sg"] = ("确定", "取消"),
                // (optional) Malaysia often uses Simplified in practice; leave as Simplified.
                ["zh-my"] = ("确定", "取消"),

                // ---------------------------------------------------------------------
                // Romance
                // ---------------------------------------------------------------------
                ["fr"] = ("OK", "Annuler"),

                // Spanish: Accept/Cancel is common UI wording.
                ["es"] = ("Aceptar", "Cancelar"),

                // Portuguese: region split (safety)
                ["pt"] = ("OK", "Cancelar"),
                ["pt-br"] = ("OK", "Cancelar"),
                ["pt-pt"] = ("OK", "Cancelar"),

                ["it"] = ("OK", "Annulla"),
                ["ro"] = ("OK", "Anulează"),

                // ---------------------------------------------------------------------
                // Germanic / Nordic
                // ---------------------------------------------------------------------
                ["de"] = ("OK", "Abbrechen"),
                ["nl"] = ("OK", "Annuleren"),

                // Swedish/Norwegian are commonly "Avbryt"
                ["sv"] = ("OK", "Avbryt"),
                ["no"] = ("OK", "Avbryt"),
                ["nb"] = ("OK", "Avbryt"),
                ["nn"] = ("OK", "Avbryt"),

                // Danish: "Annuller" (not "Avbryt")
                ["da"] = ("OK", "Annuller"),

                ["fi"] = ("OK", "Peruuta"),
                ["is"] = ("OK", "Hætta við"),

                // ---------------------------------------------------------------------
                // Slavic / Cyrillic
                // ---------------------------------------------------------------------
                ["ru"] = ("OK", "Отмена"),
                ["uk"] = ("OK", "Скасувати"),
                ["pl"] = ("OK", "Anuluj"),
                ["cs"] = ("OK", "Zrušit"),
                ["sk"] = ("OK", "Zrušiť"),
                ["bg"] = ("OK", "Отказ"),

                // Serbian: script split (critical safety)
                ["sr"] = ("OK", "Откажи"),          // treat plain sr as Cyrillic on Windows contexts
                ["sr-cyrl"] = ("OK", "Откажи"),
                ["sr-latn"] = ("OK", "Otkaži"),

                // Croatian / Slovenian (Latin)
                ["hr"] = ("OK", "Odustani"),
                ["sl"] = ("OK", "Prekliči"),

                // ---------------------------------------------------------------------
                // Greek
                // ---------------------------------------------------------------------
                ["el"] = ("OK", "Ακύρωση"),

                // ---------------------------------------------------------------------
                // RTL (Arabic/Hebrew/Persian/Urdu)
                // ---------------------------------------------------------------------
                ["ar"] = ("موافق", "إلغاء"),
                ["he"] = ("אישור", "ביטול"),
                ["fa"] = ("تأیید", "لغو"),
                ["ur"] = ("ٹھیک ہے", "منسوخ"),

                // ---------------------------------------------------------------------
                // Indic (major scripts)
                // ---------------------------------------------------------------------
                ["hi"] = ("ठीक", "रद्द करें"),
                ["bn"] = ("ঠিক আছে", "বাতিল"),
                ["ta"] = ("சரி", "ரத்து"),
                ["te"] = ("సరే", "రద్దు"),
                ["kn"] = ("ಸರಿ", "ರದ್ದು"),
                ["ml"] = ("ശരി", "റദ്ദാക്കുക"),
                ["mr"] = ("ठीक", "रद्द"),
                ["gu"] = ("બરાબર", "રદ"),
                ["pa"] = ("ਠੀਕ ਹੈ", "ਰੱਦ ਕਰੋ"),
                ["si"] = ("හරි", "අවලංගු"),

                // ---------------------------------------------------------------------
                // Southeast Asia
                // ---------------------------------------------------------------------
                ["th"] = ("ตกลง", "ยกเลิก"),
                ["vi"] = ("OK", "Hủy"),
                ["id"] = ("OK", "Batal"),
                ["ms"] = ("OK", "Batal"),
                ["tl"] = ("OK", "Kanselah"),

                // ---------------------------------------------------------------------
                // Africa
                // ---------------------------------------------------------------------
                ["sw"] = ("Sawa", "Ghairi"),
                ["am"] = ("እሺ", "ሰርዝ"),
            };

    }
}
