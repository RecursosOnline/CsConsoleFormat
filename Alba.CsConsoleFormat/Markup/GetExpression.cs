﻿using System;
using System.ComponentModel;
using Alba.CsConsoleFormat.Framework.Text;

// TODO Support more complex getter expressions
// TODO Support converters properly (see MS.Internal.Data.DefaultValueConverter)
namespace Alba.CsConsoleFormat.Markup
{
    public class GetExpression : GetExpressionBase
    {
        public string Format { get; set; }
        public ConverterDelegate Converter { get; set; }
        public object Parameter { get; set; }

        protected override object GetValueFromSource (object source)
        {
            if (source == null)
                return null;
            if (Path.IsNullOrEmpty())
                return ConvertValue(source);
            return TraversePathToProperty(source);
        }

        protected override object ConvertValue (object value)
        {
            if (Converter != null)
                value = Converter(value, Parameter, EffectiveCulture);
            if (Format != null)
                value = string.Format(EffectiveCulture, Format, value);

            if (value == null) // TODO ???
                return null;
            // Check whether value can be assigned to the property
            Type valueType = value.GetType();
            if (TargetType.IsAssignableFrom(valueType))
                return value;
            // Try converting using Convert class
            if (typeof(IConvertible).IsAssignableFrom(TargetType) && typeof(IConvertible).IsAssignableFrom(valueType))
                return Convert.ChangeType(value, TargetType, EffectiveCulture);
            // Try converting with value's TypeConverter
            TypeConverter valueConverter = TypeDescriptor.GetConverter(valueType);
            if (valueConverter.CanConvertTo(TargetType))
                return valueConverter.ConvertTo(ValueConverterContext.Context, EffectiveCulture, value, TargetType);
            // Try converting with target's TypeConverter
            TypeConverter targetConverter = TypeDescriptor.GetConverter(TargetType);
            if (targetConverter.CanConvertFrom(valueType))
                return targetConverter.ConvertFrom(ValueConverterContext.Context, EffectiveCulture, value);

            throw new InvalidOperationException($"Cannot convert from '{valueType}' to '{TargetType}'.");
        }

        private class ValueConverterContext : ITypeDescriptorContext
        {
            public static readonly ValueConverterContext Context = new ValueConverterContext();

            public IContainer Container => null;
            public object Instance => null;
            public PropertyDescriptor PropertyDescriptor => null;
            public object GetService (Type serviceType) => null;
            public bool OnComponentChanging () => false;

            public void OnComponentChanged ()
            {}
        }
    }
}