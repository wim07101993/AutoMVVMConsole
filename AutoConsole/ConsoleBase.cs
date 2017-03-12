using AutoConsole.Attributes;
using ClassLibrary.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace AutoConsole
{
    public class ConsoleBase
    {

        #region FIELDS

        private object _dataContext;

        private bool _exit;
        private bool _return;

        #endregion FIELDS



        #region  PROPERTIES

        public object DataContext
        {
            get { return _dataContext; }
            set
            {
                if (Equals(_dataContext, value))
                    return;

                _dataContext = value;

                CreateQuestion();
            }
        }

        public string Question { get; set; }
        internal List<Answer> AnswerList { get; set; }

        #endregion PROPERTIES



        #region  METHODS

        private void CreateQuestion()
        {
            Question = _dataContext.ToString();

            AddMethodsToQuestion();
            AddPropertiesToQuestion();
        }
        private void AddMethodsToQuestion()
        {
            Question += "\r\nMETHODS:";
            var methods = _dataContext
                .GetType()
                .GetMethods()
                .Where(x => Attribute.IsDefined(x, typeof(ShowInConsoleAttribute)))
                .ToList();

            if (EnumerableExtensions.IsNullOrEmpty(methods))
                Question += "\r\nNo visible methods";
            else
                foreach (var method in methods)
                {
                    Question += $"\r\n\t- {method.GetDisplayName()}(";

                    var parameters = method.GetParameters();
                    if (!EnumerableExtensions.IsNullOrEmpty(parameters))
                    {
                        foreach (var parameter in parameters)
                            Question += $"{parameter.ParameterType.Name} {parameter.GetName()}, ";

                        Question = Question.Substring(0, Question.Length - 2);
                    }

                    Question += ")";
                }
        }
        private void AddPropertiesToQuestion()
        {
            Question += "\r\nPROPERTIES:";
            var properties = _dataContext
                .GetType()
                .GetProperties()
                .Where(x => Attribute.IsDefined(x, typeof(ShowInConsoleAttribute)))
                .ToList();

            if (EnumerableExtensions.IsNullOrEmpty(properties))
                Question += "\r\nNo visible properties";
            else
                foreach (var property in properties)
                    Question += $"\r\n\t- {property.GetDisplayName()}";

        }

        public void AskQuestion()
        {
            while (!_exit)
            {
                Console.WriteLine(Question);

                var stringAnswer = Console.ReadLine();

                CheckIfStringIsSystemParameter(stringAnswer);

                Console.WriteLine(ConvertStringToObject(stringAnswer, DataContext));

                AnswerQuestion();
            }
        }
        private void AnswerQuestion()
        {
            if (EnumerableExtensions.IsNullOrEmpty(AnswerList))
                return;

            var temp = new object();

            foreach (var answer in AnswerList)
                temp = answer.GetValue();

            if (temp == null)
                Console.WriteLine("null");
            else
                Console.WriteLine(temp);
        }

        private void CheckIfStringIsSystemParameter(string str)
        {
            switch (str.ToLower())
            {
                case "exit":
                    Environment.Exit(Environment.ExitCode);
                    break;
                case "return":
                    AnswerList.Clear();
                    CreateQuestion();
                    _exit = true;
                    break;
            }
        }
        private object ConvertStringToObject(string str, object dataContext)
        {
            if (string.IsNullOrWhiteSpace(str))
                return null;
            if (dataContext == null)
                throw new ArgumentNullException(nameof(dataContext));

            char[] chars = { '.', '(' };
            var splitChar = str.SplitOnFirst(chars, out string[] split);

            if (splitChar == '0' && TryParse(str.Trim(' '), out object obj, dataContext))
                return obj;

            if (splitChar == chars[0] && TryParse(split[0].Trim(' '), out obj, dataContext))
                return ConvertStringToObject(split[1], obj);

            if (splitChar != chars[1] || !split[1].Contains(")"))
            {
                CommandNotKnown();
                return null;
            }

            var splitParametersAndRest = split[1].SplitOnFirst(')');

            var stringParameters = splitParametersAndRest[0].Split(',');

            if (stringParameters.Length > 1)
            {
                var parameters =
                    stringParameters.Select(stringParameter => ConvertStringToObject(stringParameter, dataContext))
                        .ToArray();

                if (TryParse(split[0].Trim(' '), out obj, dataContext, parameters))
                {
                    return splitParametersAndRest.Length > 1
                        ? ConvertStringToObject(splitParametersAndRest[1], obj)
                        : obj;
                }

            }
            else
            {
                object[] parameters = null;
                if (!string.IsNullOrWhiteSpace(splitParametersAndRest[0])
                    && TryParse(splitParametersAndRest[0].Trim(' '), out object parameter, dataContext))
                    parameters = new[] { parameter };

                if (TryParse(split[0].Trim(' '), out obj, dataContext, parameters))
                    return obj;
            }

            CommandNotKnown();
            return null;
        }

        private static bool TryParse(string str, out object obj, object dataContext = null)
        {
            obj = null;

            if (bool.TryParse(str, out bool boolean))
                obj = boolean;
            else if (byte.TryParse(str, out byte b))
                obj = b;
            else if (short.TryParse(str, out short s))
                obj = s;
            else if (int.TryParse(str, out int integer))
                obj = integer;
            else if (long.TryParse(str, out long l))
                obj = l;
            else if (double.TryParse(str, out double d))
                obj = d;

            else if (str.Length >= 2 && str.Last() == '"' && str.First() == '"')
            {
                var ret = str.Substring(1, str.Length - 2);

                if (!ret.Contains("\""))
                    obj = ret;
            }

            else if (dataContext != null)
            {
                var member = dataContext.GetType().GetMembers().Find(x => x.GetDisplayName() == str);
                if (member == null)
                    return false;

                var method = member as PropertyInfo;
                obj = method?.GetValue(dataContext);
            }
            else
                try
                {
                    obj = str.JsonDeserialize();
                }
                catch
                {
                    // ignored
                }

            return obj != null;
        }
        private static bool TryParse(string str, out object obj, object dataContext, object[] parameters)
        {
            if (dataContext != null)
            {
                MethodInfo method;
                if (EnumerableExtensions.IsNullOrEmpty(parameters))
                    method =
                        dataContext.GetType()
                            .GetMethods()
                            .Find(x => x.GetDisplayName() == str &&
                                EnumerableExtensions.IsNullOrEmpty(x.GetParameters()));
                else
                    method = (from m in dataContext.GetType().GetMethods()
                              let mParameters = m.GetParameters()
                              where m.GetDisplayName() == str
                              && mParameters.Length == parameters.Length
                              && !parameters.Where(
                                  (t, i) => t.GetType() != mParameters[i].ParameterType)
                                  .Any()
                              select m)
                              .FirstOrDefault();


                if (method != null)
                {
                    obj = method.Invoke(dataContext, parameters);
                    return true;
                }
            }

            obj = str;
            return false;
        }


        protected void CommandNotKnown()
        {
            Console.WriteLine("Command unknown");
        }

        protected void RemoveLastAnswer()
        {
            if (EnumerableExtensions.IsNullOrEmpty(AnswerList))
                AnswerList.RemoveAt(AnswerList.Count - 1);
        }

        #endregion METHODS
    }
}
