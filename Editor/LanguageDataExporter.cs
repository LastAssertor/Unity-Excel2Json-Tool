using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;

namespace UnityExcel2JsonTool {

    public static class LanguageDataExporter {

        [@MenuItem("Assets/Unity-Excel2Json-Tool/3.多语言（导出多语言配置文件）", false, 5003)]
        public static void Export() {

            var guids = AssetDatabase.FindAssets("t:UnityExcel2JsonTool.Settings");
            if (guids.Length == 0) {
                Debug.LogWarning("Not found 'UnityExcel2JsonTool.Settings' asset.");
                return;
            }

            var settings = AssetDatabase.LoadAssetAtPath<Settings>(AssetDatabase.GUIDToAssetPath(guids[0]));

            if (string.IsNullOrEmpty(settings.exportWorkbookFolder)) {
                Debug.LogWarning("UnityExcel2JsonTool.Settings.exportWorkbookFolder can not be empty.");
                return;
            }

            if (string.IsNullOrEmpty(settings.exportLocalizationDataFolder)) {
                Debug.LogWarning("UnityExcel2JsonTool.Settings.exportLocalizationDataFolder can not be empty.");
                return;
            }

            var input = Path.Combine(settings.exportWorkbookFolder, settings.localizationWorkbookFileName);

            var workbookJson = File.ReadAllText(input + ".json");
            var workbook = UnityEngine.JsonUtility.FromJson<Workbook>(workbookJson);
            var languageDatas = new Dictionary<string, LanguageData>();

            foreach (var sheet in workbook.Sheets) {
                if (sheet.GetValue(0, 0).Trim().ToLower() != settings.localizationKeyTag) {
                    continue;
                }
                var dict = LanguageDataSerializer.Parse(sheet);
                foreach (var kvp in dict) {
                    languageDatas.Add(kvp.Key, kvp.Value);
                }
            }

            if (languageDatas.Count == 0) {
                return;
            }

            var outputDir = settings.exportLocalizationDataFolder;
            outputDir.CreateDirectory();

            foreach (var kvp in languageDatas) {
                var fileName = kvp.Key + ".json";
                var languagePath = Path.Combine(outputDir, fileName);
                var languageJson = UnityEngine.JsonUtility.ToJson(kvp.Value, true);
                File.WriteAllText(languagePath, languageJson);
            }

            AssetDatabase.Refresh();

            Debug.Log("Export localization configs successed!");

        }


    }
}

