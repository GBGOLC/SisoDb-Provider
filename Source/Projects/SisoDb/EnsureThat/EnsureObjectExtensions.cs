using System.Diagnostics;
using SisoDb.EnsureThat.Resources;

namespace SisoDb.EnsureThat
{
    public static class EnsureObjectExtensions
    {
        [DebuggerStepThrough]
        public static Param<T> IsNotNull<T>(this Param<T> param) where T : class
        {
            if (param.Value == null)
                throw ExceptionFactory.CreateForParamNullValidation(param.Name, ExceptionMessages.EnsureExtensions_IsNotNull);

            return param;
        }
    }
}