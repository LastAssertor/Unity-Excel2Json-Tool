using System.Collections;
using System.Collections.Generic;
using System;

namespace LGamekit.Excel2Json
{


    public class CSharp
    {

        public enum ValueType
        {
            Error,
            Int,
            Float,
            String,
            Bool,
            Enum
        }

        [Serializable]
        public class EnumField
        {
            public string Name;
            public bool CustomValue;
            public int Value;
        }

        [Serializable]
        public class Enum
        {
            public string Workbook;
            public string Name;
            public List<EnumField> Fields = new List<EnumField>();

            public EnumField GetField(string fieldName)
            {
                foreach (var field in Fields)
                {
                    if (field.Name == fieldName)
                    {
                        return field;
                    }
                }
                return null;
            }

        }

        [Serializable]
        public class ClassField
        {
            public string Name;
            public string Comment;
            public string DefaultValue;
            public ValueType ValueType;
            public string EnumType;
            public bool IsList;
            public int Column;
            public int MaxSize;

        }

        [Serializable]
        public class Class
        {
            public string Workbook;
            public string Name;
            public List<ClassField> Fields = new List<ClassField>();

            public ClassField GetField(string fieldName)
            {
                foreach (var field in Fields)
                {
                    if (field.Name == fieldName)
                    {
                        return field;
                    }
                }
                return null;
            }

        }

        [Serializable]
        public class Headers
        {
            public List<Enum> Enums = new List<Enum>();
            public List<Class> Classes = new List<Class>();

            public Enum GetEnum(string enumName)
            {
                foreach (var @enum in Enums)
                {
                    if (@enum.Name == enumName)
                    {
                        return @enum;
                    }
                }
                return null;
            }

            public Class GetClass(string className)
            {
                foreach (var @class in Classes)
                {
                    if (@class.Name == className)
                    {
                        return @class;
                    }
                }
                return null;
            }

        }

    }


}

