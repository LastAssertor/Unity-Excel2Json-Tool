using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;

namespace UnityExcel2JsonTool {

    public static class CSharpSerializer {

        public const int EnumFieldBeginRow = 1;
        public const int EnumFieldNameCol = 0;
        public const int EnumFieldValueCol = 1;

        public const string IntType = "int";
        public const string FloatType = "float";
        public const string StringType = "string";
        public const string BoolType = "bool";

        public const int ClassFieldBeginCol = 1;
        public const int ClassFieldCommentRow = 0;
        public const int ClassFieldNameRow = 1;
        public const int ClassFieldTypeRow = 2;
        public const int ClassFieldDefaultValueRow = 3;

        public static char[] ListTypeSeparator = new char[] { ':', '：' };
        public const char ListValueSeparator = '|';

        public static CSharp.Enum ParseEnum(Sheet sheet) {
            var @enum = new CSharp.Enum {
                Name = sheet.Name
            };
            var row = EnumFieldBeginRow;
            var rowLen = sheet.GetRowSize();
            var fields = new Dictionary<string, CSharp.EnumField>();

            for (; row < rowLen; row++) {
                var field = new CSharp.EnumField {
                    Name = sheet.GetValue(row, EnumFieldNameCol)
                };

                if (string.IsNullOrEmpty(field.Name)) {
                    continue;
                }

                var value = sheet.GetValue(row, EnumFieldValueCol);
                if (!string.IsNullOrEmpty(value)) {
                    field.CustomValue = true;
                    field.Value = int.Parse(value);
                }

                fields.Add(field.Name, field);
                @enum.Fields.Add(field);
            }

            return @enum;
        }

        public static CSharp.Class ParseClass(Sheet sheet, Dictionary<string, CSharp.Enum> enums) {
            var @class = new CSharp.Class {
                Name = sheet.Name
            };

            var col = ClassFieldBeginCol;
            var colLen = sheet.GetColSize();
            var fields = new Dictionary<string, CSharp.ClassField>();

            for (; col < colLen; col++) {
                var field = new CSharp.ClassField {
                    Comment = sheet.GetValue(ClassFieldCommentRow, col),
                    Name = sheet.GetValue(ClassFieldNameRow, col)
                };

                if (string.IsNullOrEmpty(field.Name)) {
                    continue;
                }

                var valueType = sheet.GetValue(ClassFieldTypeRow, col);
                var arr = valueType.Split(ListTypeSeparator);

                if (arr.Length > 1) {
                    int.TryParse(arr[1], out field.MaxSize);
                    valueType = arr[0];
                    field.IsList = true;
                }

                var lowerValueType = valueType.ToLower();

                switch (lowerValueType) {
                    case IntType:
                    field.ValueType = CSharp.ValueType.Int;
                    break;
                    case FloatType:
                    field.ValueType = CSharp.ValueType.Float;
                    break;
                    case StringType:
                    field.ValueType = CSharp.ValueType.String;
                    break;
                    case BoolType:
                    field.ValueType = CSharp.ValueType.Bool;
                    break;
                    default: {
                            if (enums.ContainsKey(valueType)) {
                                field.ValueType = CSharp.ValueType.Enum;
                                field.EnumType = valueType;
                            } else {
                                field.ValueType = CSharp.ValueType.Error;
                            }
                        }
                        break;
                }

                field.Column = col;
                field.DefaultValue = sheet.GetValue(ClassFieldDefaultValueRow, col);

                if (field.IsList && field.MaxSize > 1) {
                    if (!string.IsNullOrEmpty(field.DefaultValue)) {
                        for (int i = field.Column + 1; i < field.MaxSize; i++) {
                            var value = sheet.GetValue(ClassFieldDefaultValueRow, i);
                            if (string.IsNullOrEmpty(value)) {
                                break;
                            }
                            field.DefaultValue = field.DefaultValue + ListValueSeparator + value;
                        }
                    }
                    col += field.MaxSize - 1;
                }

                fields.Add(field.Name, field);
                @class.Fields.Add(field);
            }

            return @class;
        }

    }


}

