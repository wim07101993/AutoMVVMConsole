using AutoConsole.Attributes;
using ClassLibrary.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using GalaSoft.MvvmLight;

namespace AutoConsole
{
    /// <summary>
    /// <para>Class to automaticly generate a console application with the methods and properties of a given data context.</para>
    /// <para>
    /// The methods and properties shown are those that have the <see cref="ShowInConsoleAttribute"/> attribute.
    /// If none of the members of the class have the attribute, all properties and methods are shown.
    /// </para>
    /// </summary>
    public class ConsoleBase : ObservableObject
    {
        #region FIELDS

        private ObservableCollection<object> _DataContextHierarchy;

        #endregion FIELDS


        #region  PROPERTIES

        /// <summary>
        /// <para>
        /// Gets the current data context (last of the <see cref="DataContextHierarchy"/> properties)
        /// or sets the <see cref="DataContextHierarchy"/> property to a new <see cref="List{T}"/> with the value in.
        /// </para>
        /// <para>The data context is the object from which this object gets its methods and properties.</para>
        /// <para>
        /// The methods and properties shown are those that have the <see cref="ShowInConsoleAttribute"/> attribute.
        /// If none of the members of the class have the attribute, all properties and methods are shown.
        /// </para>
        /// </summary>
        public object DataContext
        {
            get { return DataContextHierarchy.Last(); }
            set { DataContextHierarchy = new ObservableCollection<object> { value }; }
        }
        /// <summary>
        /// <para>
        /// Gets the base data context (first of the <see cref="DataContextHierarchy"/> properties)
        /// or sets the <see cref="DataContextHierarchy"/> property to a new <see cref="List{T}"/> with the value in.
        /// </para>
        /// <para>The data context is the object from which this object gets its methods and properties.</para>
        /// <para>
        /// The methods and properties shown are those that have the <see cref="ShowInConsoleAttribute"/> attribute.
        /// If none of the members of the class have the attribute, all properties and methods are shown.
        /// </para>
        /// </summary>
        public object BaseDataContext
        {
            get { return DataContextHierarchy.First(); }
            set { DataContextHierarchy = new ObservableCollection<object> { value }; }
        }

        /// <summary>
        /// <para>Gets or sets the data context hierarchy of the class.</para>
        /// <para>The data context is the object from which this object gets its methods and properties.</para>
        /// <para>
        /// The methods and properties shown are those that have the <see cref="ShowInConsoleAttribute"/> attribute.
        /// If none of the members of the class have the attribute, all properties and methods are shown.
        /// </para>
        /// </summary>
        public ObservableCollection<object> DataContextHierarchy
        {
            get { return _DataContextHierarchy; }
            protected set
            {
                if (Equals(_DataContextHierarchy, value))
                    return;

                if (_DataContextHierarchy != null)
                    _DataContextHierarchy.CollectionChanged -= _DataContextHierarchy_CollectionChanged;

                Set(ref _DataContextHierarchy, value);
                CreateQuestion();
                RaisePropertyChanged(() => BaseDataContext);
                RaisePropertyChanged(() => DataContext);

                _DataContextHierarchy.CollectionChanged += _DataContextHierarchy_CollectionChanged;
            }
        }

        /// <summary>
        /// <para>Gets or sets the question that is asked to the user of the application.</para>
        /// </summary>
        public string Question { get; protected set; }

        /// <summary>
        /// <para>Gets or sets the <see cref="bool"/> that stops the infinite loop of asking the <see cref="Question"/>.</para>
        /// <para>True: stop asking</para> 
        /// <para>False: if the method <see cref="AskQuestion"/> is called, the application continues asking the <see cref="Question"/></para>
        /// </summary>
        public bool Exit { protected get; set; } = false;

        #endregion PROPERTIES



        #region  METHODS

        private void _DataContextHierarchy_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CreateQuestion();
            RaisePropertyChanged(() => DataContext);
        }

        /// <summary>
        /// <para>Creates a question whith the given data context.</para>
        /// <para>
        /// The methods and properties shown are those that have the <see cref="ShowInConsoleAttribute"/> attribute.
        /// If none of the members of the class have the attribute, all properties and methods are shown.
        /// </para>
        /// </summary>
        private void CreateQuestion()
        {
            Question = DataContext.ToString();

            AddMethodsToQuestion();
            AddPropertiesToQuestion();
        }

        /// <summary>
        /// <para>Adds the methods that have the <see cref="ShowInConsoleAttribute"/> attribute to the property <see cref="Question"/>.</para>
        /// <para>If none of the methods of the class have the attribute, all and methods are shown.</para>
        /// </summary>
        private void AddMethodsToQuestion()
        {
            Question += "\r\nMETHODS:";
            var methods = DataContext
                .GetType()
                .GetMethods()
                .Where(x => Attribute.IsDefined(x, typeof(ShowInConsoleAttribute)))
                .ToList();

            if (EnumerableExtensions.IsNullOrEmpty(methods))
                methods = DataContext.GetType().GetMethods().ToList();

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
        /// <summary>
        /// <para>Adds the methods that have the <see cref="ShowInConsoleAttribute"/> attribute to the property <see cref="Question"/>.</para>
        /// <para>If none of the methods of the class have the attribute, all and methods are shown.</para>
        /// </summary>
        private void AddPropertiesToQuestion()
        {
            Question += "\r\nPROPERTIES:";
            var properties = DataContext
                .GetType()
                .GetProperties()
                .Where(x => Attribute.IsDefined(x, typeof(ShowInConsoleAttribute)))
                .ToList();

            if (EnumerableExtensions.IsNullOrEmpty(properties))
                properties = DataContext.GetType().GetProperties().ToList();

            if (EnumerableExtensions.IsNullOrEmpty(properties))
                Question += "\r\nNo visible properties";
            else
                foreach (var property in properties)
                    Question += $"\r\n\t- {property.GetDisplayName()} ({property.PropertyType.Name})";

        }

        /// <summary>
        /// <para>Starts an infinite loop in which the application asks the <see cref="Question"/>, waits for an answer and replies.</para>
        /// <para>The loop stops when the "exit" command is given, the property <see cref="Exit"/> is set to true or the application is shut down.</para>
        /// </summary>
        public void AskQuestion()
        {
            while (!Exit)
            {
                Console.WriteLine(Question);

                var stringAnswer = Console.ReadLine();

                if (!CheckIfStringIsSystemParameter(stringAnswer))
                    Console.WriteLine(ConvertStringToObject(stringAnswer, DataContext));
            }
        }

        /// <summary>
        /// <para>Checks if <see cref="str"/> is a system parameter:</para>
        /// <para>exit: shut the application down</para>
        /// <para>return: return to the base data context</para>
        /// <para>clear or cls: clears the screen</para>
        /// </summary>
        /// <param name="str">string input</param>
        private bool CheckIfStringIsSystemParameter(string str)
        {
            switch (str.ToLower())
            {
                case "exit":
                    Environment.Exit(Environment.ExitCode);
                    break;
                case "return":
                    RemoveLastAnswer();
                    return true;
                case "clear":
                case "cls":
                    Console.Clear();
                    return true;
            }
            return false;
        }

        /// <summary>
        /// <para>
        /// The method returns the value as if you wrote the code in the editor.
        /// </para>
        /// <para>
        /// Detects if the given string contains out of methods and/or parameters and/or just values ans splits it in those parts.
        /// </para>
        /// <para>
        /// Applies a <see cref="TryParse(string,out object,object)"/> on the parts.
        /// </para>
        /// </summary>
        /// <param name="str">string to convert</param>
        /// <param name="dataContext"><see cref="object"/> to find the properties and methods in</param>
        /// <returns>The result object of the statement</returns>
        private object ConvertStringToObject(string str, object dataContext)
        {
            if (string.IsNullOrWhiteSpace(str))
                return null;
            if (dataContext == null)
                throw new ArgumentNullException(nameof(dataContext));


            var goDeeperString = "->";
            var goDeeperInDataContext = false;
            if (str.Length > 2 && str.Substring(0, 2) == goDeeperString)
            {
                goDeeperInDataContext = true;
                goDeeperString = "";
                str = str.Substring(2);
            }
            object ret = null;


            char[] chars = { '.', '(' };
            var splitChar = str.SplitOnFirst(chars, out string[] split);

            if (splitChar == '0' && TryParse(str.Trim(' '), out object obj, dataContext))
                ret = obj;
            else if (splitChar == chars[0] && TryParse(split[0].Trim(' '), out obj, dataContext))
                ret = ConvertStringToObject(goDeeperString + split[1], obj);
            else if (splitChar == chars[1] && split[1].Contains(")"))
            {
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
                            ? ConvertStringToObject(goDeeperString + splitParametersAndRest[1], obj)
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
            }


            if (ret == null)
            {
                CommandNotKnown();
                return null;
            }

            if (goDeeperInDataContext)
                DataContextHierarchy.Add(ret);
            return ret;
        }

        /// <summary>
        /// <para>
        /// Tries to parse the <see cref="str"/> parameter to a base type.
        /// </para>
        /// <para>
        /// If it is unable to parse, it searches for a property with the same display name (assigned with the <see cref="DisplayNameAttribute"/> attribute).
        /// </para>
        /// <para>
        /// If there is no property with thath name, it tries to deserialize the string <see cref="str"/> with JSON.
        /// </para>
        /// <para>
        /// Else the object parameter <see cref="obj"/> is set to null and false is returned.
        /// </para>
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <param name="obj">Object in which the parsed value is stored</param>
        /// <param name="dataContext">Object that is used as data context</param>
        /// <returns>true: parse succeeded, false: parse failed</returns>
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
                var property = dataContext.GetType().GetProperties().Find(x => x.GetDisplayName() == str);
                if (property == null)
                    return false;
                obj = property.GetValue(dataContext);
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
        /// <summary>
        /// <para>
        /// Searches for a method ith the same display name (assigned with the <see cref="DisplayNameAttribute"/> attribute) and parametertypes as the
        /// given parameters in the <see cref="parameters"/> parameter.
        /// </para>
        /// <para>
        /// If there is such method, it is excecuted with the parameters.
        /// </para>
        /// <para>
        /// Else the object parameter <see cref="obj"/> is set to null and false is returned.
        /// </para>
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <param name="obj">Object in which the parsed value is stored</param>
        /// <param name="dataContext">Object that is used as data context</param>
        /// <param name="parameters">parameters for the method</param>
        /// <returns>true: parse succeeded, false: parse failed</returns>
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

        /// <summary>
        /// Prints an error message in the console.
        /// </summary>
        protected void CommandNotKnown()
        {
            Console.WriteLine("Command unknown");
        }

        protected void RemoveLastAnswer()
        {
            DataContextHierarchy.RemoveAt(DataContextHierarchy.Count - 1);
        }

        #endregion METHODS
    }
}
