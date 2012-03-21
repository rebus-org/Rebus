using System;
using System.Reflection;
using GalaSoft.MvvmLight;

namespace Rebus.Snoop.ViewModel
{
    public abstract class ViewModel : ViewModelBase
    {
        protected void SetValue(string propertyName, object value, params string[] affectedProperties)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("You need to specify the name of a property.", "propertyName");
            }

            if (char.IsLower(propertyName[0]))
            {
                throw new ArgumentException(string.Format("{0} is not a valid property name - property names must start with an upper case letter!", propertyName), "propertyName");
            }

            var fieldName = char.ToLower(propertyName[0]) + propertyName.Substring(1);

            var fieldInfo = GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (fieldInfo == null)
            {
                throw new InvalidOperationException(
                    string.Format("Attempted to set value of {0} to {1}, but the matching field named {2} could not be found!",
                        propertyName, value, fieldName));
            }

            var oldValue = fieldInfo.GetValue(this);

            if (ReferenceEquals(null, oldValue) && ReferenceEquals(null, value)) return;
            if (!ReferenceEquals(null, oldValue) && !ReferenceEquals(null, value) && oldValue.Equals(value)) return;

            fieldInfo.SetValue(this, value);

            RaisePropertyChanged(propertyName, oldValue, value, true);

            foreach(var affectedPropertyName in affectedProperties)
            {
                RaisePropertyChanged(affectedPropertyName);
            }
        }    
    }
}