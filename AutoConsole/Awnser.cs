using System.Linq;
using System.Reflection;

namespace AutoConsole
{
    internal class Answer
    {
        #region CONSTRUCTOR

        public Answer()
        {
        }

        #endregion CONSTRUCTOR



        #region PROPERTIES

        public bool IsMethod => MemberInfo is MethodInfo;
        public bool IsProperty => MemberInfo is PropertyInfo;

        public MemberInfo MemberInfo { get; set; }
        public object Owner { get; set; }
        public Answer[] ParametersIfMethod { get; set; }

        #endregion PROPERTIES



        #region METHODS

        public object GetValue()
        {
            if (IsProperty)
                return ((PropertyInfo)MemberInfo).GetValue(Owner);
            if (IsMethod)
                return ((MethodInfo)MemberInfo).Invoke(
                    Owner,
                    ParametersIfMethod?.Select(answer => answer.GetValue()).ToArray());

            return null;
        }

        #endregion METHODS
    }
}
