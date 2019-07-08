using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;


namespace LGamekit.Excel2Json {

    public static class CSharpExporter {

        const string EnumSheetType = "enum";
        const string ClassSheetType = "class";

        const string TemplateFolder = "LGamekit/Excel2Json/Templates";
        const string EnumTemplateFile = "CSharpEnum.txt";
        const string ClassTemplateFile = "CSharpClass.txt";
        const string ClassDataTemplateFile = "CSharpClassData.txt";
        const string DataSerializerTemplateFile = "CSharpDataSerializer.txt";
        const string DataExporterTemplateFile = "CSharpDataExporter.txt";

        const string HeadersFile = "Headers.json";

        const string EnumTag = "{{.Enum}}";
        const string ClassTag = "{{.Class}}";
        const string FieldsTag = "{{.Fields}}";
        const string MethodsTag = "{{.Methods}}";
        const string ReadFieldsTag = "{{.ReadFields}}";
        const string MenuItemOrderTag = "{{.MenuItemOrder}}";
        const string WriteJsonsTag = "{{.WriteJsons}}";

        const int MenuItemOrder = 5100;

        [@MenuItem("Assets/Excel2Json/Export CSharp", false, 5002)]
        public static void Export() {

            var inputDir = EditorUtility.OpenFolderPanel("Export CSharp", "", "");
            if (string.IsNullOrEmpty(inputDir)) {
                return;
            }

            /// Load workbooks.

            var workbooks = WorkbookSerializer.LoadWorkbooks(inputDir);

            var enumSheets = new Dictionary<string, Sheet>();
            var classSheets = new Dictionary<string, Sheet>();
            var scources = new Dictionary<string, string>();

            foreach (var workbook in workbooks) {
                foreach (var sheet in workbook.Value.Sheets) {
                    var sheetType = sheet.GetValue(0, 0).Trim().ToLower();
                    switch (sheetType) {
                    case EnumSheetType:
                        enumSheets.Add(sheet.Name, sheet);
                        break;
                    case ClassSheetType:
                        classSheets.Add(sheet.Name, sheet);
                        break;
                    default:
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

            var templateDir = Path.Combine(Application.dataPath, TemplateFolder);
            var enumTemplate = File.ReadAllText(Path.Combine(templateDir, EnumTemplateFile), Encoding.UTF8);
            var classTemplate = File.ReadAllText(Path.Combine(templateDir, ClassTemplateFile), Encoding.UTF8);
            var classDataTemplate = File.ReadAllText(Path.Combine(templateDir, ClassDataTemplateFile), Encoding.UTF8);
            var dataSerializerTemplate = File.ReadAllText(Path.Combine(templateDir, DataSerializerTemplateFile), Encoding.UTF8);
            var dataExporterTemplate = File.ReadAllText(Path.Combine(templateDir, DataExporterTemplateFile), Encoding.UTF8);

            var outputDir = Path.Combine(inputDir, "Scripts");
            outputDir.CreateDirectory();

            foreach (var @enum in enums) {
                var enumStr = enumTemplate.Replace(EnumTag, @enum.Value.Name);
                var enumSb = new StringBuilder();

                foreach (var enumField in @enum.Value.Fields) {
                    enumSb.AppendFormat("\t{0}", enumField.Name);
                    if (enumField.CustomValue) {
                        enumSb.AppendFormat(" = {0}", enumField.Value);
                    }
                    enumSb.Append(",\n");
                }

                /// Write Enum.cs

                enumStr = enumStr.Replace(FieldsTag, enumSb.ToString());
                var enumPath = Path.Combine(outputDir, @enum.Value.Name + ".cs");
                File.WriteAllText(enumPath, enumStr, Encoding.UTF8);
            }

            foreach (var @class in classes) {
                var classStr = classTemplate.Replace(ClassTag, @class.Value.Name);
                var classSb = new StringBuilder();

                foreach (var classField in @class.Value.Fields) {
                    if (!string.IsNullOrEmpty(classField.Comment)) {
                        classSb.AppendTab();
                        classSb.AppendFormat("// {0}\n", classField.Comment);
                    }

                    classSb.AppendTab();
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
                    classSb.AppendLine(classField.Name + ";");
                }

                /// Write Class.cs

                classStr = classStr.Replace(FieldsTag, classSb.ToString());
                classStr = classStr.Replace(MethodsTag, string.Empty);
                var classPath = Path.Combine(outputDir, @class.Value.Name + ".cs");
                File.WriteAllText(classPath, classStr, Encoding.UTF8);

                /// Write ClassData.cs

                var classDataStr = classDataTemplate.Replace(ClassTag, @class.Value.Name);
                var classDataPath = Path.Combine(outputDir, @class.Value.Name + "Data.cs");
                File.WriteAllText(classDataPath, classDataStr, Encoding.UTF8);
            }

            /// Write Headers.json

            var headersJson = UnityEngine.JsonUtility.ToJson(headers, true);
            var headersPath = Path.Combine(outputDir, HeadersFile);
            File.WriteAllText(headersPath, headersJson, Encoding.UTF8);

            outputDir = Path.Combine(outputDir, "Editor");
            outputDir.CreateDirectory();

            int tab2 = 2;
            int tab3 = 3;
            int tab4 = 4;
            int tab5 = 5;

            var exporterSb = new StringBuilder();

            foreach (var @class in headers.Classes) {

                exporterSb.AppendTab(tab2);
                exporterSb.AppendFormat("path = Path.Combine(output, \"{0}Data.json\");\n", @class.Name);
                exporterSb.AppendTab(tab2);
                exporterSb.AppendFormat("json = JsonUtility.ToJson({0}DataSerializer.Parse(getSheet(\"{1}\")), true);\n",
                                        @class.Name, @class.Name);

                exporterSb.AppendTab(tab2);
                exporterSb.AppendLine("File.WriteAllText(path, json, Encoding.UTF8);");

                var sbSerializer = new StringBuilder();
                string serializerStr = dataSerializerTemplate.Replace(ClassTag, @class.Name);

                bool hasList = false;
                foreach (var field in @class.Fields) {
                    if (field.IsList) {
                        hasList = true;
                        break;
                    }
                }

                if (hasList) {
                    sbSerializer.AppendTab(tab3);
                    sbSerializer.AppendLine("string[] arr = null;");
                }

                for (int i = 0, len = @class.Fields.Count; i < len; i++) {
                    var field = @class.Fields[i];

                    if (field.IsList) {

                        sbSerializer.AppendTab(tab3);
                        sbSerializer.AppendFormat("arr = sheet.GetColRange(row, {0}, {1}, \"|\").Split('|');\n", field.Column, field.MaxSize);

                        Action<string> a2 = (string t) => {
                            sbSerializer.AppendTab(tab3);
                            sbSerializer.AppendFormat("_{0}.{1} = new List<{2}>();\n", @class.Name, field.Name, t);
                            sbSerializer.AppendTab(tab3);
                            sbSerializer.AppendLine("foreach(var str in arr) {");
                            sbSerializer.AppendTab(tab4);
                            sbSerializer.AppendLine("if (string.IsNullOrEmpty(str)) {");
                            sbSerializer.AppendTab(tab5);
                            sbSerializer.AppendLine("continue;");
                            sbSerializer.AppendTab(tab4);
                            sbSerializer.AppendLine("}");
                            sbSerializer.AppendTab(tab4);
                            sbSerializer.AppendFormat("{0} v{1};\n", t, i);
                            sbSerializer.AppendTab(tab4);
                            sbSerializer.AppendFormat("if ({0}.TryParse(str, out v{1})) ", t, i);
                            sbSerializer.Append("{\n");
                            sbSerializer.AppendTab(tab5);
                            sbSerializer.AppendFormat("_{0}.{1}.Add(v{2});\n", @class.Name, field.Name, i);
                            sbSerializer.AppendTab(tab4);
                            sbSerializer.AppendLine("}");
                            sbSerializer.AppendTab(tab3);
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
                                sbSerializer.AppendTab(tab3);
                                sbSerializer.AppendFormat("_{0}.{1} = new List<{2}>();\n", @class.Name, field.Name, "string");
                                sbSerializer.AppendTab(tab3);
                                sbSerializer.AppendLine("foreach(var str in arr) {");
                                sbSerializer.AppendTab(tab4);
                                sbSerializer.AppendLine("if (string.IsNullOrEmpty(str)) {");
                                sbSerializer.AppendTab(tab5);
                                sbSerializer.AppendLine("continue;");
                                sbSerializer.AppendTab(tab4);
                                sbSerializer.AppendLine("}");
                                sbSerializer.AppendTab(tab4);
                                sbSerializer.AppendFormat("_{0}.{1}.Add(str);\n", @class.Name, field.Name);
                                sbSerializer.AppendTab(tab3);
                                sbSerializer.AppendLine("}");
                            }
                            break;
                        case CSharp.ValueType.Bool:
                            a2(CSharpSerializer.BoolType);
                            break;
                        case CSharp.ValueType.Enum: {
                                sbSerializer.AppendTab(tab3);
                                sbSerializer.AppendFormat("_{0}.{1} = new List<{2}>();\n", @class.Name, field.Name, field.EnumType);
                                sbSerializer.AppendTab(tab3);
                                sbSerializer.AppendLine("foreach(var str in arr) {");
                                sbSerializer.AppendTab(tab4);
                                sbSerializer.AppendLine("if (string.IsNullOrEmpty(str)) {");
                                sbSerializer.AppendTab(tab5);
                                sbSerializer.AppendLine("continue;");
                                sbSerializer.AppendTab(tab4);
                                sbSerializer.AppendLine("}");
                                sbSerializer.AppendTab(tab4);
                                sbSerializer.AppendFormat("var obj = Enum.Parse(typeof({0}), str);\n", field.EnumType);
                                sbSerializer.AppendTab(tab4);
                                sbSerializer.AppendLine("if (obj != null) {");
                                sbSerializer.AppendTab(tab5);
                                sbSerializer.AppendFormat("_{0}.{1}.Add(({2})obj);\n", @class.Name, field.Name, field.EnumType);
                                sbSerializer.AppendTab(tab4);
                                sbSerializer.AppendLine("}");
                                sbSerializer.AppendTab(tab3);
                                sbSerializer.AppendLine("}");
                            }
                            break;
                        default:
                            continue;
                        }

                    } else {

                        Action<string> a1 = (string t) => {
                            sbSerializer.AppendTab(tab3);
                            sbSerializer.AppendFormat("string v{0};\n", i);
                            sbSerializer.AppendTab(tab3);
                            sbSerializer.AppendFormat("if (sheet.TryGetValue(row, {0}, out v{1})) ", field.Column, i);
                            sbSerializer.Append("{\n");
                            sbSerializer.AppendTab(tab4);
                            sbSerializer.AppendFormat("{0}.TryParse(v{1}, out _{2}.{3});\n", t, i, @class.Name, field.Name);
                            sbSerializer.AppendTab(tab3);
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
                                sbSerializer.AppendTab(tab3);
                                sbSerializer.AppendFormat("_{0}.{1} = sheet.GetValue(row, {2});\n", @class.Name, field.Name, field.Column);
                            }
                            break;
                        case CSharp.ValueType.Bool:
                            a1(CSharpSerializer.BoolType);
                            break;
                        case CSharp.ValueType.Enum: {
                                sbSerializer.AppendTab(tab3);
                                sbSerializer.AppendFormat("string v{0};\n", i);
                                sbSerializer.AppendTab(tab3);
                                sbSerializer.AppendFormat("if (sheet.TryGetValue(row, {0}, out v{1})) ", field.Column, i);
                                sbSerializer.Append("{\n");
                                sbSerializer.AppendTab(tab4);
                                sbSerializer.AppendFormat("var obj = Enum.Parse(typeof({0}), v{1});\n", field.EnumType, i);
                                sbSerializer.AppendTab(tab4);
                                sbSerializer.AppendLine("if (obj != null) {");
                                sbSerializer.AppendTab(tab5);
                                sbSerializer.AppendFormat("_{0}.{1} = ({2})obj;\n", @class.Name, field.Name, field.EnumType);
                                sbSerializer.AppendTab(tab4);
                                sbSerializer.AppendLine("}");
                                sbSerializer.AppendTab(tab3);
                                sbSerializer.AppendLine("}");
                            }
                            break;
                        default:
                            continue;
                        }

                    }

                }

                /// Write ClassDataSerializer.cs

                serializerStr = serializerStr.Replace(ReadFieldsTag, sbSerializer.ToString());
                var serializerPath = Path.Combine(outputDir, @class.Name + "DataSerializer.cs");
                File.WriteAllText(serializerPath, serializerStr, Encoding.UTF8);
            }

            /// Write CSharpDataExporter.cs

            var exporterStr = dataExporterTemplate.Replace(WriteJsonsTag, exporterSb.ToString())
                                                  .Replace(MenuItemOrderTag, MenuItemOrder.ToString());
            var exporterPath = Path.Combine(outputDir, "CSharpDataExporter.cs");
            File.WriteAllText(exporterPath, exporterStr, Encoding.UTF8);

            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(outputDir);
        }

    }


}