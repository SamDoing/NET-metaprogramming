using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NetMetaprograming.GenericRepositoryBuilder.Utils
{
    public static class MethodInfoHelper
    {
        public static MethodInfo Of<TResult>(Expression<Func<TResult>> f) => ((MethodCallExpression)f.Body).Method;
        public static MethodInfo Of<T>(Expression<Action<T>> f) => ((MethodCallExpression)f.Body).Method;
        public static MethodInfo Of(Expression<Action> f) => ((MethodCallExpression)f.Body).Method;
    }
}
