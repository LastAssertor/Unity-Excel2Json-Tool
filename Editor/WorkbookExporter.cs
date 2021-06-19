using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;


namespace UnityExcel2JsonTool {

    public static class WorkbookExporter {

        [@MenuItem("Assets/Unity-Excel2Json-Tool/1.预处理（导出表单文件）", false, 5000)]
        public static void Export() {

            var guids = AssetDatabase.FindAssets("t:UnityExcel2JsonTool.Settings");
            if (guids.Length == 0) {
                Debug.LogWarning("Not found 'UnityExcel2JsonTool.Settings' asset.");
                return;
            }

            var settings = AssetDatabase.LoadAssetAtPath<Settings>(AssetDatabase.GUIDToAssetPath(guids[0]));

            if (string.IsNullOrEmpty(settings.excelFolder)) {
                Debug.LogWarning("UnityExcel2JsonTool.Settings.excelFolder can not be empty.");
                return;
            }

            if (string.IsNullOrEmpty(settings.exportWorkbookFolder)) {
                Debug.LogWarning("UnityExcel2JsonTool.Settings.exportWorkbookFolder can not be empty.");
                return;
            }

            string[] files;

            try {
                files = Directory.GetFiles(settings.excelFolder, "*.xlsx", SearchOption.AllDirectories);
            } catch (Exception ex) {
                Debug.LogWarning(ex);
                return;
            }

            if (files.Length == 0) {
                return;
            }

            StringBuilder sb = new StringBuilder($"Export workbook successed! files = {files.Length}\n");

            try {

                if (!Directory.Exists(settings.exportWorkbookFolder)) {
                    Directory.CreateDirectory(settings.exportWorkbookFolder);
                }

                foreach (var path in files) {
                    var fileName = Path.GetFileNameWithoutExtension(path) + ".json";
                    var workbookPath = Path.Combine(settings.exportWorkbookFolder, fileName);
                    var workbook = WorkbookSerializer.LoadWorkbook(path);
                    var workbookJson = UnityEngine.JsonUtility.ToJson(workbook, true);
                    File.WriteAllText(workbookPath, workbookJson);
                    sb.AppendLine(workbookPath);
                }

            } catch (Exception ex) {
                Debug.LogWarning(ex);
                return;
            }

            AssetDatabase.Refresh();
            Debug.Log(sb);
        }

    }

}

