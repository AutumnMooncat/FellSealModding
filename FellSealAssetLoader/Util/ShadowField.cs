using System;
using System.Globalization;
using System.Reflection;

namespace FellSealAssetLoader.Util
{
    public class ShadowField : FieldInfo
    {
        public override string Name { get; }
        public override Type DeclaringType { get; }
        public override Type ReflectedType { get; }
        public override Type FieldType { get; }
        public override FieldAttributes Attributes { get; }
        public override RuntimeFieldHandle FieldHandle { get; }
        public object Value { get; set; }

        public ShadowField(string name, FieldInfo prototype) : this(name, prototype.DeclaringType, prototype.ReflectedType, prototype.FieldType, prototype.Attributes)
        { }
        
        public ShadowField(string name, Type declaringType, Type reflectedType, Type fieldType, FieldAttributes attributes = FieldAttributes.Public | FieldAttributes.Static, RuntimeFieldHandle fieldHandle = default)
        {
            Name = name;
            DeclaringType = declaringType;
            ReflectedType = reflectedType;
            FieldType = fieldType;
            Attributes = attributes;
            FieldHandle = fieldHandle;
        }
        
        public override object[] GetCustomAttributes(bool inherit)
        {
            return Array.Empty<object>();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

        public override object GetValue(object obj)
        {
            return Value;
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            Value = value;
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return Array.Empty<object>();
        }
    }
}