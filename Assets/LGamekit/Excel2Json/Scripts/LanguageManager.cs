using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;

namespace LGamekit.Excel2Json {


    public static class LanguageManager {

        const string LanguageKey = "LanguageManager";
        static SystemLanguage _language;
        static Dictionary<string, string> dictionary;
        static bool initialized;

        public static System.Action onChangeLanguage;
        public static SystemLanguage defaultLanguage = SystemLanguage.ChineseSimplified;
        public static readonly List<SystemLanguage> languages = new List<SystemLanguage>() {
        SystemLanguage.ChineseSimplified,
        SystemLanguage.English
    };

        public static SystemLanguage language {
            get {
                if (!initialized) {
                    var selected = PlayerPrefs.GetInt(LanguageKey);
                    if (selected >= 0 && selected < languages.Count) {
                        _language = languages[selected];
                    } else {
                        _language = defaultLanguage;
                    }
                    dictionary = Load(_language);
                    initialized = true;
                    OnChangeLanguage();
                }
                return _language;
            }
            set {
                var selected = languages.IndexOf(value);
                if (!initialized) {
                    if (selected >= 0) {
                        PlayerPrefs.SetInt(LanguageKey, selected);
                        PlayerPrefs.Save();
                        _language = value;
                    } else {
                        _language = defaultLanguage;
                    }
                    dictionary = Load(_language);
                    initialized = true;
                    OnChangeLanguage();
                } else {
                    if (selected >= 0) {
                        if (_language != value) {
                            dictionary = Load(value);
                            PlayerPrefs.SetInt(LanguageKey, selected);
                            PlayerPrefs.Save();
                            _language = value;
                            OnChangeLanguage();
                        }
                    }
                }
            }
        }

        static void OnChangeLanguage() {
            if (onChangeLanguage != null)
                onChangeLanguage.Invoke();
        }

        public static bool Has(string key) {
            return dictionary.ContainsKey(key);
        }

        public static string Get(string key) {
            return Has(key) ? dictionary[key] : string.Empty;
        }

        public static string Format(string key, params object[] parameters) {
            return string.Format(Get(key), parameters);
        }

        public static Dictionary<string, string> Load(SystemLanguage language) {
            var path = "Languages/" + language.ToString();
            var json = Resources.Load<TextAsset>(path).text;
            var list = UnityEngine.JsonUtility.FromJson<LanguageData>(json).Data;
            var dict = new Dictionary<string, string>();
            for (int i = 0, len = list.Count / 2; i < len; i++) {
                dict.Add(list[i], list[len + i]);
            }
            return dict;
        }

#if UNITY_EDITOR

        public static Dictionary<string, LanguageData> LoadAll() {
            var dict = new Dictionary<string, LanguageData>();
            var folder = Path.Combine(Application.dataPath, "Languages");
            var paths = Directory.GetFiles(folder, "*.json");
            foreach (var path in paths) {
                var json = File.ReadAllText(path, Encoding.UTF8);
                var data = UnityEngine.JsonUtility.FromJson<LanguageData>(json);
                dict.Add(data.Language, data);
            }
            return dict;
        }

#endif

    }



}

