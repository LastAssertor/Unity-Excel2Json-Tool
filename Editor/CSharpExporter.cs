using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;

namespace UnityExcel2JsonTool {

    public static class CSharpExporter {

        [@MenuItem("Assets/Unity-Excel2Json-Tool/2-1.生成脚本（表头信息、数据类、序列化类、反序列化类）", false, 5001)]
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

            if (string.IsNullOrEmpty(settings.exportScriptFolder)) {
                Debug.LogWarning("UnityExcel2JsonTool.Settings.exportScriptFolder can not be empty.");
                return;
            }

            /// Load workbooks.

            var workbooks = WorkbookSerializer.LoadWorkbooks(settings.exportWorkbookFolder);

            var enumSheets = new Dictionary<string, Sheet>();
            var classSheets = new Dictionary<string, Sheet>();
            var scources = new Dictionary<string, string>();

            foreach (var workbook in workbooks) {
                foreach (var sheet in workbook.Value.Sheets) {
                    var sheetType = sheet.GetValue(0, 0).Trim().ToLower();
                    if (sheetType == settings.enumSheetType) {
                        enumSheets.Add(sheet.Name, sheet);
                    } else if (sheetType == settings.classSheetType) {
                        classSheets.Add(sheet.Name, sheet);
                    } else {
                        continue;
                    }
                    scources.Add(sheet.Name, workbook.Value.Name);
                }
            }

            var headers = new CSharp.Headers();

            /// Parse enums.

            var enums = new Dictionary<string, CSharp.Enum>();

            foreach (var sheet in enumSheets) {
                var @enum = CSharpSerializer.ParseEnum(sheet.Value);
                @enum.Workbook = scources[@enum.Name];
                enums.Add(@enum.Name, @enum);
                headers.Enums.Add(@enum);
            }

            /// Parse classes.

            var classes = new Dictionary<string, CSharp.Class>();

            foreach (var sheet in classSheets) {
                var @class = CSharpSerializer.ParseClass(sheet.Value, enums);
                @class.Workbook = scources[@class.Name];
                classes.Add(@class.Name, @class);
                headers.Classes.Add(@class);
            }

            if (headers.Classes.Count == 0 && headers.Enums.Count == 0) {
                return;
            }

            var enumTemplate = settings.enumTemplate.text;
            var classTemplate = settings.classTemplate.text;
            var classDataTemplate = settings.classDataTemplate.text;
            var dataSerializerTemplate = settings.dataSerializerTemplate.text;
            var dataExporterTemplate = settings.dataExporterTemplate.text;

            var outputDir = settings.exportScriptFolder;
            outputDir.CreateDirectory();

            int tab2 = 2;

            foreach (var @enum in enums) {
                var enumStr = enumTemplate.Replace(settings.enumTag, @enum.Value.Name);
                var enumSb = new StringBuilder();

                for (int i = 0, len = @enum.Value.Fields.Count; i < len; i++) {
                    var enumField = @enum.Value.Fields[i];
                    enumSb.AppendTab(tab2).Append(enumField.Name);
                    if (enumField.CustomValue) {
                        enumSb.AppendFormat(" = {0}", enumField.Value);
                    }
                    if (i != len - 1) {
                        enumSb.Append(",\n");
                    }
                }

                /// Write Enum.cs

                enumStr = enumStr.Replace(settings.fieldsTag, enumSb.ToString());
                var enumPath = Path.Combine(outputDir, @enum.Value.Name + ".cs");
                File.WriteAllText(enumPath, enumStr);
            }

            foreach (var @class in classes) {
                var classStr = classTemplate.Replace(settings.classTag, @class.Value.Name);
                var classSb = new StringBuilder();

                foreach (var classField in @class.Value.Fields) {
                    if (!string.IsNullOrEmpty(classField.Comment)) {
                        classSb.AppendTab(tab2);
                        classSb.AppendFormat("// {0}\n", classField.Comment);
                    }

                    classSb.AppendTab(tab2);
                    classSb.Append("public ");

                    var fullType = string.Empty;
                    switch (classField.ValueType) {
                        case CSharp.ValueType.Int:
                        case CSharp.ValueType.Float:
                        case CSharp.ValueType.String:
                        case CSharp.ValueType.Bool:
                        fullType = classField.ValueType.ToString().ToLower();
                        break;
                        case CSharp.ValueType.Enum:
                        fullType = classField.EnumType;
                        break;
                        case CSharp.ValueType.Error:
                        break;
                    }

                    if (classField.IsList) {
                        fullType = "List<" + fullType + ">";
                    }

                    classSb.Append(fullType + " ");
                    classSb.Append(classField.Name);

                    if (!string.IsNullOrEmpty(classField.DefaultValue)) {

                        if (!classField.IsList) {

                            switch (classField.ValueType) {
                                case CSharp.ValueType.Int: {
                                        if (int.TryParse(classField.DefaultValue, out int v)) {
                                            classSb.Append($" = {v}");
                                        }
                                    }
                                    break;
                                case CSharp.ValueType.Float: {
                                        if (float.TryParse(classField.DefaultValue, out float v)) {
                                            classSb.Append($" = {v}f");
                                        }
                                    }
                                    break;
                                case CSharp.ValueType.Bool: {
                                        if (bool.TryParse(classField.DefaultValue.ToLower(), out bool v)) {
                                            classSb.Append($" = {v.ToString().ToLower()}");
                                        }
                                    }
                                    break;
                                case CSharp.ValueType.Enum: {
                                        classSb.Append($" = {classField.EnumType}.{classField.DefaultValue}");
                                    }
                                    break;
                                case CSharp.ValueType.String: {
                                        classSb.Append($" = \"{classField.DefaultValue}\"");
                                    }
                                    break;
                                case CSharp.ValueType.Error:
                                break;
                            }

                        } else {

                            switch (classField.ValueType) {
                                case CSharp.ValueType.Int: {
                                        classSb.Append(" = new " + fullType + " { ");
                                        var defaultValueArr = classField.DefaultValue.Split('|', '｜');
                                        for (int j = 0, defaultValueArrLen = defaultValueArr.Length; j < defaultValueArrLen; j++) {
                                            var tempStr = defaultValueArr[j];
                                            if (int.TryParse(tempStr, out int v)) {
                                                classSb.Append($"{v}");
                                                if (j != defaultValueArrLen - 1) {
                                                    classSb.Append(", ");
                                                }
                                            }
                                        }
                                        classSb.Append(" }");
                                    }
                                    break;
                                case CSharp.ValueType.Float: {
                                        classSb.Append(" = new " + fullType + " { ");
                                        var defaultValueArr = classField.DefaultValue.Split('|', '｜');
                                        for (int j = 0, defaultValueArrLen = defaultValueArr.Length; j < defaultValueArrLen; j++) {
                                            var tempStr = defaultValueArr[j];
                                            if (float.TryParse(tempStr, out float v)) {
                                                classSb.Append($"{v}f");
                                                if (j != defaultValueArrLen - 1) {
                                                    classSb.Append(", ");
                                                }
                                            }
                                        }
                                        classSb.Append(" }");
                                    }
                                    break;
                                case CSharp.ValueType.Bool: {
                                        classSb.Append(" = new " + fullType + " { ");
                                        var defaultValueArr = classField.DefaultValue.Split('|', '｜');
                                        for (int j = 0, defaultValueArrLen = defaultValueArr.Length; j < defaultValueArrLen; j++) {
                                            var tempStr = defaultValueArr[j].ToLower();
                                            if (bool.TryParse(tempStr, out bool v)) {
                                                classSb.Append($"{v}");
                                                if (j != defaultValueArrLen - 1) {
                                                    classSb.Append(", ");
                                                }
                                            }
                                        }
                                        classSb.Append(" }");
                                    }
                                    break;
                                case CSharp.ValueType.Enum: {
                                        classSb.Append(" = new " + fullType + " { ");
                                        var defaultValueArr = classField.DefaultValue.Split('|', '｜');
                                        for (int j = 0, defaultValueArrLen = defaultValueArr.Length; j < defaultValueArrLen; j++) {
                                            var tempStr = defaultValueArr[j];
                                            classSb.Append($"{classField.EnumType}.{tempStr}");
                                            if (j != defaultValueArrLen - 1) {
                                                classSb.Append(", ");
                                            }
                                        }
                                        classSb.Append(" }");
                                    }
                                    break;
                                case CSharp.ValueType.String: {
                                        classSb.Append(" = new " + fullType + " { ");
                                        var defaultValueArr = classField.DefaultValue.Split('|', '｜');
                                        for (int j = 0, defaultValueArrLen = defaultValueArr.Length; j < defaultValueArrLen; j++) {
                                            var tempStr = defaultValueArr[j];
                                            classSb.Append($"\"{tempStr}\"");
                                            if (j != defaultValueArrLen - 1) {
                                                classSb.Append(", ");
                                            }
                                        }
                                        classSb.Append(" }");
                                    }
                                    break;
                                case CSharp.ValueType.Error:
                                break;
                            }

                        }

                    }

                    classSb.AppendLine(";");

                }

                /// Write Class.cs

                classStr = classStr.Replace(settings.fieldsTag, classSb.ToString());
                classStr = classStr.Replace(settings.methodsTag, string.Empty);
                var classPath = Path.Combine(outputDir, @class.Value.Name + ".cs");
                File.WriteAllText(classPath, classStr);

                /// Write ClassData.cs

                var classDataStr = classDataTemplate.Replace(settings.classTag, @class.Value.Name);
                var classDataPath = Path.Combine(outputDir, @class.Value.Name + "Data.cs");
                File.WriteAllText(classDataPath, classDataStr);
            }

            /// Write Headers.json

            var headersJson = UnityEngine.JsonUtility.ToJson(headers, true);
            var headersPath = Path.Combine(outputDir, settings.headersFileName + ".json");
            File.WriteAllText(headersPath, headersJson);

            outputDir = Path.Combine(outputDir, "Editor");
            outputDir.CreateDirectory();

            int tab3 = 3;
            int tab4 = 4;
            int tab5 = 5;
            int tab6 = 6;

            var exporterSb = new StringBuilder();

            foreach (var @class in headers.Classes) {

                exporterSb.AppendTab(tab3);
                exporterSb.AppendFormat("path = Path.Combine(outputDir, \"{0}Data.json\");\n", @class.Name);
                exporterSb.AppendTab(tab3);
                exporterSb.AppendFormat("json = JsonUtility.ToJson({0}DataSerializer.Parse(getSheet(\"{1}\")), true);\n",
                                        @class.Name, @class.Name);

                exporterSb.AppendTab(tab3);
                exporterSb.AppendLine("File.WriteAllText(path, json);");

                var sbSerializer = new StringBuilder();
                string serializerStr = dataSerializerTemplate.Replace(settings.classTag, @class.Name);

                bool hasList = false;
                foreach (var field in @class.Fields) {
                    if (field.IsList) {
                        hasList = true;
                        break;
                    }
                }

                if (hasList) {
                    sbSerializer.AppendTab(tab4);
                    sbSerializer.AppendLine("string[] arr;");
                }

                for (int i = 0, len = @class.Fields.Count; i < len; i++) {
                    var field = @class.Fields[i];

                    if (field.IsList) {

                        sbSerializer.AppendTab(tab4);
                        sbSerializer.AppendFormat("arr = sheet.GetColRange(row, {0}, {1}, \"|\").Split('|', '｜');\n", field.Column, field.MaxSize);

                        Action<string> a2 = (string t) => {
                            sbSerializer.AppendTab(tab4);
                            sbSerializer.AppendFormat("_{0}.{1} = new List<{2}>();\n", @class.Name, field.Name, t);
                            sbSerializer.AppendTab(tab4);
                            sbSerializer.AppendLine("foreach(var str in arr) {");
                            sbSerializer.AppendTab(tab5);
                            sbSerializer.AppendLine("if (string.IsNullOrEmpty(str)) {");
                            sbSerializer.AppendTab(tab6);
                            sbSerializer.AppendLine("continue;");
                            sbSerializer.AppendTab(tab5);
                            sbSerializer.AppendLine("}");
                            sbSerializer.AppendTab(tab5);
                            sbSerializer.AppendFormat("if ({0}.TryParse(str, out {0} v{1})) ", t, i);
                            sbSerializer.AppendLine("{");
                            sbSerializer.AppendTab(tab6);
                            sbSerializer.AppendFormat("_{0}.{1}.Add(v{2});\n", @class.Name, field.Name, i);
                            sbSerializer.AppendTab(tab5);
                            sbSerializer.AppendLine("}");
                            sbSerializer.AppendTab(tab4);
                            sbSerializer.AppendLine("}");
                        };

                        switch (field.ValueType) {
                            case CSharp.ValueType.Int:
                            a2(CSharpSerializer.IntType);
                            break;
                            case CSharp.ValueType.Float:
                            a2(CSharpSerializer.FloatType);
                            break;
                            case CSharp.ValueType.String: {
                                    sbSerializer.AppendTab(tab4);
                                    sbSerializer.AppendFormat("_{0}.{1} = new List<{2}>();\n", @class.Name, field.Name, "string");
                                    sbSerializer.AppendTab(tab4);
                                    sbSerializer.AppendLine("foreach(var str in arr) {");
                                    sbSerializer.AppendTab(tab5);
                                    sbSerializer.AppendLine("if (string.IsNullOrEmpty(str)) {");
                                    sbSerializer.AppendTab(tab6);
                                    sbSerializer.AppendLine("continue;");
                                    sbSerializer.AppendTab(tab5);
                                    sbSerializer.AppendLine("}");
                                    sbSerializer.AppendTab(tab5);
                                    sbSerializer.AppendFormat("_{0}.{1}.Add(str);\n", @class.Name, field.Name);
                                    sbSerializer.AppendTab(tab4);
                                    sbSerializer.AppendLine("}");
                                }
                                break;
                            case CSharp.ValueType.Bool:
                            a2(CSharpSerializer.BoolType);
                            break;
                            case CSharp.ValueType.Enum: {
                                    sbSerializer.AppendTab(tab4);
                                    sbSerializer.AppendFormat("_{0}.{1} = new List<{2}>();\n", @class.Name, field.Name, field.EnumType);
                                    sbSerializer.AppendTab(tab4);
                                    sbSerializer.AppendLine("foreach(var str in arr) {");
                                    sbSerializer.AppendTab(tab5);
                                    sbSerializer.AppendLine("if (string.IsNullOrEmpty(str)) {");
                                    sbSerializer.AppendTab(tab6);
                                    sbSerializer.AppendLine("continue;");
                                    sbSerializer.AppendTab(tab5);
                                    sbSerializer.AppendLine("}");
                                    sbSerializer.AppendTab(tab5);
                                    sbSerializer.AppendFormat("var obj = Enum.Parse(typeof({0}), str);\n", field.EnumType);
                                    sbSerializer.AppendTab(tab5);
                                    sbSerializer.AppendLine("if (obj != null) {");
                                    sbSerializer.AppendTab(tab6);
                                    sbSerializer.AppendFormat("_{0}.{1}.Add(({2})obj);\n", @class.Name, field.Name, field.EnumType);
                                    sbSerializer.AppendTab(tab5);
                                    sbSerializer.AppendLine("}");
                                    sbSerializer.AppendTab(tab4);
                                    sbSerializer.AppendLine("}");
                                }
                                break;
                            default:
                            continue;
                        }

                    } else {

                        Action<string> a1 = (string t) => {
                            sbSerializer.AppendTab(tab4);
                            sbSerializer.AppendFormat("if (sheet.TryGetValue(row, {0}, out string v{1})) ", field.Column, i);
                            sbSerializer.Append("{\n");
                            sbSerializer.AppendTab(tab5);
                            sbSerializer.AppendFormat("{0}.TryParse(v{1}, out _{2}.{3});\n", t, i, @class.Name, field.Name);
                            sbSerializer.AppendTab(tab4);
                            sbSerializer.AppendLine("}");
                        };

                        switch (field.ValueType) {
                            case CSharp.ValueType.Int:
                            a1(CSharpSerializer.IntType);
                            break;
                            case CSharp.ValueType.Float:
                            a1(CSharpSerializer.FloatType);
                            break;
                            case CSharp.ValueType.String: {
                                    sbSerializer.AppendTab(tab4);
                                    sbSerializer.AppendFormat("_{0}.{1} = sheet.GetValue(row, {2});\n", @class.Name, field.Name, field.Column);
                                }
                                break;
                            case CSharp.ValueType.Bool:
                            a1(CSharpSerializer.BoolType);
                            break;
                            case CSharp.ValueType.Enum: {
                                    sbSerializer.AppendTab(tab4);
                                    sbSerializer.AppendFormat("if (sheet.TryGetValue(row, {0}, out string v{1})) ", field.Column, i);
                                    sbSerializer.Append("{\n");
                                    sbSerializer.AppendTab(tab5);
                                    sbSerializer.AppendFormat("var obj = Enum.Parse(typeof({0}), v{1});\n", field.EnumType, i);
                                    sbSerializer.AppendTab(tab5);
                                    sbSerializer.AppendLine("if (obj != null) {");
                                    sbSerializer.AppendTab(tab6);
                                    sbSerializer.AppendFormat("_{0}.{1} = ({2})obj;\n", @class.Name, field.Name, field.EnumType);
                                    sbSerializer.AppendTab(tab5);
                                    sbSerializer.AppendLine("}");
                                    sbSerializer.AppendTab(tab4);
                                    sbSerializer.AppendLine("}");
                                }
                                break;
                            default:
                            continue;
                        }

                    }

                }

                /// Write ClassDataSerializer.cs

                serializerStr = serializerStr.Replace(settings.readFieldsTag, sbSerializer.ToString());
                var serializerPath = Path.Combine(outputDir, @class.Name + "DataSerializer.cs");
                File.WriteAllText(serializerPath, serializerStr);
            }

            /// Write CSharpDataExporter.cs

            var exporterStr = dataExporterTemplate.Replace(settings.writeJsonsTag, exporterSb.ToString())
                                                  .Replace(settings.menuItemOrderTag, settings.menuItemOrder.ToString());
            var exporterPath = Path.Combine(outputDir, "CSharpDataExporter.cs");
            File.WriteAllText(exporterPath, exporterStr);

            AssetDatabase.Refresh();

            Debug.Log("Export scripts successed!");

        }

    }

}
