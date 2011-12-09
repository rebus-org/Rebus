// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
using System;
using System.Linq.Expressions;

namespace Ponder
{
    public class Reflect
    {
        public static string Path<T>(Expression<Func<T, object>> expression)
        {
            return GetPropertyName(expression);
        }

        public static object Value(object obj, string path)
        {
            var dots = path.Split('.');

            foreach(var dot in dots)
            {
                var propertyInfo = obj.GetType().GetProperty(dot);
                if (propertyInfo == null) return null;
                obj = propertyInfo.GetValue(obj, new object[0]);
                if (obj == null) break;
            }

            return obj;
        }

        static string GetPropertyName(Expression expression)
        {
            if (expression == null) return "";

            if (expression is LambdaExpression)
            {
                expression = ((LambdaExpression) expression).Body;
            }

            if (expression is UnaryExpression)
            {
                expression = ((UnaryExpression)expression).Operand;
            }

            if (expression is MemberExpression)
            {
                dynamic memberExpression = expression;

                var lambdaExpression = (Expression)memberExpression.Expression;

                string prefix;
                if (lambdaExpression != null)
                {
                    prefix = GetPropertyName(lambdaExpression);
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        prefix += ".";
                    }
                }
                else
                {
                    prefix = "";
                }

                var propertyName = memberExpression.Member.Name;
                
                return prefix + propertyName;
            }

            return "";
        }
    }
}