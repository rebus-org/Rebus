using System;
using System.Linq.Expressions;
using System.Reflection;
using GalaSoft.MvvmLight;

namespace Rebus.Snoop.ViewModel
{
    public abstract class ViewModel : ViewModelBase
    {
        protected void SetValue<T>(Expression<Func<T>> propertyExpression, object value, params string[] affectedProperties)
        {
            var propertyName = ExtractPropertyName(propertyExpression);

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

        protected static string ExtractPropertyName<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression == null)
                throw new ArgumentNullException("propertyExpression");
            var memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression == null)
                throw new ArgumentException("Expression was not a MemberExpression", "propertyExpression");
            var propertyInfo = memberExpression.Member as PropertyInfo;
            if (propertyInfo == null)
                throw new ArgumentException("Expression was not a PropertyInfo", "propertyExpression");
            if (propertyInfo.GetGetMethod(true).IsStatic)
                throw new ArgumentException("The property can not be static", "propertyExpression");
            
            
            return memberExpression.Member.Name;
        }
    }
}