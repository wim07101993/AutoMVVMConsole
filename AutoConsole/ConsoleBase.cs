using AutoConsole.Attributes;
using ClassLibrary.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
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

        private ObservableCollection<object> _dataContextHierarchy;
        private bool _didNotKnowCommand;
        private bool _showAvailableMembersOnce;

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
            get => DataContextHierarchy.Last();
            set => DataContextHierarchy = new ObservableCollection<object> {value};
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
            get => DataContextHierarchy.First();
            set => DataContextHierarchy = new ObservableCollection<object> {value};
        }

        /// <summary>
        /// <para>Gets or sets the data context hierarchy of the class.</para>
        /// <para>The data context is the object from which this object gets its methods and properties.</para>
        /// <para>
        /// The methods and properties shown are those that have the <see cref="ShowInConsoleAttribute"/> attribute.
        /// If none of the members of the class have the attribute, all properties and methods are shown.
        /// </para>
        /// </summary>
        protected ObservableCollection<object> DataContextHierarchy
        {
            get => _dataContextHierarchy;
            set
            {
                if (Equals(_dataContextHierarchy, value))
                    return;

                if (_dataContextHierarchy != null)
                    _dataContextHierarchy.CollectionChanged -= _DataContextHierarchy_CollectionChanged;

                Set(ref _dataContextHierarchy, value);
                CreateQuestion();
                RaisePropertyChanged(() => BaseDataContext);
                RaisePropertyChanged(() => DataContext);

                _dataContextHierarchy.CollectionChanged += _DataContextHierarchy_CollectionChanged;
            }
        }

        /// <summary>
        /// <para>Gets the data context hierarchy of the class.</para>
        /// <para>The data context is the object from which this object gets its methods and properties.</para>
        /// <para>
        /// The methods and properties shown are those that have the <see cref="ShowInConsoleAttribute"/> attribute.
        /// If none of the members of the class have the attribute, all properties and methods are shown.
        /// </para>
        /// </summary>
        public IReadOnlyCollection<object> ReadOnlyDataContextHierarchy => DataContextHierarchy;

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

        /// <summary>
        /// Gets or sets the <see cref="bool"/> that indicates if all members should be shown on en console or just those with the <see cref="ShowInConsoleAttribute"/>
        /// </summary>
        public bool ShowAllMembers { private get; set; }

        /// <summary>
        /// Gets or sets the <see cref="bool"/> that indicates if the members should be shown. This happenes only once, after that they are shown, the value automatically resets
        /// </summary>
        public bool ShowAvailableMembersOnce
        {
            private get
            {
                var temp = _showAvailableMembersOnce;
                _showAvailableMembersOnce = false;
                return temp;
            }
            set { Set(ref _showAvailableMembersOnce, value); }
        }

        #endregion PROPERTIES


        #region  METHODS

        /// <summary>
        /// This method is called when the <see cref="DataContextHierarchy"/> property is changed. It regenerates the <see cref="Question"/> and raises a property changed on the <see cref="DataContext"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _DataContextHierarchy_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CreateQuestion();
            RaisePropertyChanged(() => DataContext);
        }

        /// <summary>
        /// <para>Starts an infinite loop in which the application asks the <see cref="Question"/>, waits for an answer and replies.</para>
        /// <para>The loop stops when the "exit" command is given, the property <see cref="Exit"/> is set to true or the application is shut down.</para>
        /// </summary>
        public void AskQuestion()
        {
            do
            {
                _didNotKnowCommand = false;

                if (ShowAvailableMembersOnce)
                {
                    Console.WriteLine(Question);
                    ShowAvailableMembersOnce = false;
                }

                var stringAnswer = Console.ReadLine();

                if (!CheckIfStringIsSystemParameter(stringAnswer))
                    Console.WriteLine(ConvertStringToObject(stringAnswer, DataContext));

                Console.WriteLine("\r");
            } while (!Exit);
        }

        /// <summary>
        /// Prints an error message in the console.
        /// </summary>
        protected void CommandNotKnown()
        {
            if (_didNotKnowCommand)
                return;

            Console.WriteLine("Command unknown");
            _didNotKnowCommand = true;
        }


        #region create question

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

            ShowAvailableMembersOnce = true;
            RaisePropertyChanged(() => Question);
        }

        /// <summary>
        /// <para>Adds the methods that have the <see cref="ShowInConsoleAttribute"/> attribute to the property <see cref="Question"/>.</para>
        /// <para>If none of the methods of the class have the attribute, all and methods are shown.</para>
        /// </summary>
        private void AddMethodsToQuestion()
        {
            Question += "\r\nMETHODS:";
            IEnumerable<MethodInfo> methods;
            if (ShowAllMembers)
                methods = DataContext
                    .GetType()
                    .GetMethods()
                    .ToList();
            else
                methods = DataContext
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

            IEnumerable<PropertyInfo> properties;
            if (ShowAllMembers)
                properties = DataContext
                    .GetType()
                    .GetProperties()
                    .ToList();
            else
                properties = DataContext
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

        #endregion create question


        #region answer parsing

        /// <summary>
        /// <para>Checks if <see cref="str"/> is a system parameter:</para>
        /// <para>exit: shut the application down</para>
        /// <para>return: return to the base data context</para>
        /// <para>clear or cls: clears the screen</para>
        /// <para>...</para>
        /// </summary>
        /// <param name="str">string input</param>
        private bool CheckIfStringIsSystemParameter(string str)
        {
            switch (str.ToLower())
            {
                case "\\clear":
                case "\\cls":
                    Console.Clear();
                    return true;
                case "\\exit":
                    Environment.Exit(Environment.ExitCode);
                    return true;
                case "\\h":
                case "\\help":
                    const string help = "CASE INSENSITIVE\r\n" +
                                        "- \\cls or \\clear:\t\tClears the screen\r\n" +
                                        "- \\exit:\t\t\tShut the application down\r\n" +
                                        "- \\h or \\help:\t\t\tShow all commands\r\n" +
                                        "- \\m or \\members:\t\tShow the members of the data context\r\n" +
                                        "- \\r or \\return:\t\tReturn to the base data context\r\n" +
                                        "CASE SENSITIVE\r\n" +
                                        "- return:\t\t\tReturn to the base data context\r\n" +
                                        "- \\A or \\AllMembers:\t\tShow all the members of the data context\r\n" +
                                        "- \\a or \\LimitedMembers:\tSow only the members with the \"ShowInConsoleAttribute\"";

                    Console.WriteLine(help);
                    ShowAvailableMembersOnce = true;
                    return true;
                case "\\m":
                case "\\members":
                    CreateQuestion();
                    return true;
                case "\\return":
                case "return":
                    if (DataContextHierarchy.Count > 1)
                        DataContextHierarchy.RemoveLast();
                    else
                        Console.WriteLine("Allready on at level");
                    return true;
            }

            switch (str)
            {
                case "\\A":
                case "\\AllMembers":
                    ShowAllMembers = true;
                    Console.WriteLine("All members will be shown");
                    CreateQuestion();
                    return true;
                case "\\a":
                case "\\LimitedMembers":
                    ShowAllMembers = false;
                    Console.WriteLine("Only members with the \"ShowInConsoleAttribute\" will be shown");
                    CreateQuestion();
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
            {
                CommandNotKnown();
                return null;
            }

            str = str.Trim();
            if (str[0] == '.')
                str = str.Substring(1).Trim();

            var commandNotKnown = true;
            var newHierarchyLevel = false;
            if (str.Length > 2 && str.Substring(0, 2) == "->")
            {
                newHierarchyLevel = true;
                str = str.Substring(2);
            }

            char[] chars = {'.', '(', '[', '='};
            var splitChar = str.FindFirst(chars, out int indexOfSplitChar);

            string rest = null;
            if (splitChar == '.' && TryParse(dataContext, str.Substring(0, indexOfSplitChar), out object ret))
            {
                rest = str.Substring(indexOfSplitChar + 1);
                commandNotKnown = false;
            }
            else if (splitChar == '(' && ConvertMethodStringToReturnValue(dataContext, str, out ret, out rest))
                commandNotKnown = false;
            else if (splitChar == '[' && ConvertArraySelectorStringToReturnValue(dataContext, str, out ret, out rest))
                commandNotKnown = false;
            else if (splitChar == '=' && ConvertAssignmentStringToReturnValue(dataContext, str, out ret))
                commandNotKnown = false;
            else if (TryParse(dataContext, str, out ret))
                commandNotKnown = false;


            if (!string.IsNullOrWhiteSpace(rest))
                ret = ConvertStringToObject(rest, ret);


            if (commandNotKnown)
                CommandNotKnown();
            else if (newHierarchyLevel)
            {
                DataContextHierarchy.Add(ret);
                ShowAvailableMembersOnce = true;
            }

            return ret;
        }

        /// <summary>
        /// Parses a string into a method and excecutes it.
        /// </summary>
        /// <param name="dataContext">Data context to find method in</param>
        /// <param name="str">Input string to parse and convert</param>
        /// <param name="returnValue">Return value of the method</param>
        /// <param name="leftOverString">Last part of the string that is not used (after closing bracket)</param>
        /// <returns>True: successfull, false: failed</returns>
        private bool ConvertMethodStringToReturnValue(
            object dataContext, string str,
            out object returnValue, out string leftOverString)
        {
            if (string.IsNullOrEmpty(str) || dataContext == null)
            {
                returnValue = null;
                leftOverString = str;
                return false;
            }

            var split = str.SplitOnFirst('(');
            var methodName = split[0];
            var rest = $"({split[1]}";

            if (FindOpeningAndClosingBracket(
                rest,
                BracketType.Round,
                out int openingBracketIndex,
                out int closingBracketIndex))
            {
                leftOverString = rest.Substring(closingBracketIndex + 1);

                var parameterStrings =
                    ConvertStringToParameterStringArray(
                        rest.Substring(openingBracketIndex + 1,
                            closingBracketIndex - (openingBracketIndex + 1)));

                var parameters = parameterStrings?
                    .Select(parameterString => ConvertStringToObject(parameterString, dataContext))
                    .ToArray();

                if (TryParse(dataContext, methodName, parameters, out returnValue))
                    return true;
            }

            returnValue = null;
            leftOverString = str;
            return false;
        }

        /// <summary>
        /// Parses an indexing for an array or list and gives the value of that index back.
        /// </summary>
        /// <param name="dataContext">Data context to find index-value in</param>
        /// <param name="str">Input string to parse and convert</param>
        /// <param name="returnValue">Value of index</param>
        /// <param name="leftOverString">Last part of the string that is not used (after closing bracket)</param>
        /// <returns>True: successfull, false: failed</returns>
        private bool ConvertArraySelectorStringToReturnValue(
            object dataContext, string str,
            out object returnValue, out string leftOverString)
        {
            if (string.IsNullOrEmpty(str) || dataContext == null)
            {
                returnValue = null;
                leftOverString = str;
                return false;
            }

            var split = str.SplitOnFirst('[');
            var propertyName = split[0];
            var rest = $"[{split[1]}";

            if (FindOpeningAndClosingBracket(
                rest,
                BracketType.Square,
                out int openingBracketIndex,
                out int closingBracketIndex))
            {
                leftOverString = rest.Substring(closingBracketIndex + 1);

                var stringIndex =
                    ConvertStringToObject(rest.Substring(openingBracketIndex + 1, closingBracketIndex - 1), dataContext)
                        ?
                        .ToString();

                if (int.TryParse(stringIndex, out int index))
                {
                    if (string.IsNullOrWhiteSpace(propertyName) && dataContext is IEnumerable)
                    {
                        returnValue = ((IEnumerable) dataContext).Cast<object>().ElementAt(index);
                        return true;
                    }

                    if (TryParse(dataContext, propertyName, out object ret) && ret is IEnumerable)
                    {
                        returnValue = ((IEnumerable) ret).Cast<object>().ElementAt(index);
                        return true;
                    }
                }
            }

            returnValue = null;
            leftOverString = str;
            return false;
        }

        private bool ConvertAssignmentStringToReturnValue(object dataContext, string str,
            out object returnValue)
        {
            if (string.IsNullOrEmpty(str) || dataContext == null)
            {
                returnValue = null;
                return false;
            }

            str = str.Trim();
            var split = str.SplitOnFirst('=');

            if (split.Length == 1)
            {
                returnValue = null;
                return false;
            }

            var property = dataContext.GetType().GetProperty(split[0].Trim());

            if (property != null)
            {
                var value = ConvertStringToObject(split[1].Trim(), dataContext);
                property.SetValue(dataContext, value);
                returnValue = value;
                return true;
            }

            returnValue = null;
            return false;
        }

        private enum BracketType
        {
            Square,
            Curly,
            Round
        }

        /// <summary>
        /// <para>Searches for the opening and closing bracket of a certain type.</para>
        /// <para>Sqare = [], Curly = {}, Round = ()</para>
        /// </summary>
        /// <param name="str">Input string</param>
        /// <param name="bracketType">Type of bracket to search for</param>
        /// <param name="openingBracketIndex">Index of the opening bracket</param>
        /// <param name="closingBracketIndex">Index of the closing bracket</param>
        /// <returns>True: did find brackets, false: did not find brackets</returns>
        private static bool FindOpeningAndClosingBracket(
            string str, BracketType bracketType,
            out int openingBracketIndex, out int closingBracketIndex)
        {
            List<char> openingBrackets = null;

            openingBracketIndex = -1;
            closingBracketIndex = -1;

            var openingBracket = '(';
            var closingBracket = ')';

            switch (bracketType)
            {
                case BracketType.Curly:
                    openingBracket = '{';
                    closingBracket = '}';
                    break;
                case BracketType.Square:
                    openingBracket = '[';
                    closingBracket = ']';
                    break;
            }

            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] == openingBracket)
                {
                    if (EnumerableExtensions.IsNullOrEmpty(openingBrackets))
                    {
                        openingBrackets = new List<char> {str[i]};
                        openingBracketIndex = i;
                    }
                    else
                        openingBrackets.Add(str[i]);

                    continue;
                }

                if (str[i] == closingBracket)
                {
                    if (EnumerableExtensions.IsNullOrEmpty(openingBrackets))
                        return false;

                    openingBrackets.RemoveLast();

                    if (openingBrackets.Count == 0)
                    {
                        closingBracketIndex = i;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Converts a string that represents the parameters of a method into an array of strings, the parameters
        /// </summary>
        /// <param name="str">Input parameters</param>
        /// <returns>Array of paramters</returns>
        private static string[] ConvertStringToParameterStringArray(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            var parameterSeparatorIndexes = new List<int>();
            var parameters = new List<string>();
            var brackets = new List<char>();

            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] == '(')
                {
                    brackets.Add(str[i]);
                    continue;
                }

                if (str[i] == ')')
                {
                    if (EnumerableExtensions.IsNullOrEmpty(brackets))
                        return null;

                    brackets.RemoveLast();
                    continue;
                }

                if (str[i] == ',' && EnumerableExtensions.IsNullOrEmpty(brackets))
                    parameterSeparatorIndexes.Add(i);
            }


            if (parameterSeparatorIndexes.Count == 0)
                parameters.Add(str);
            else
            {
                for (var i = 0; i < parameterSeparatorIndexes.Count; i++)
                {
                    if (i == 0)
                    {
                        parameters.Add(str.Substring(0, parameterSeparatorIndexes[i]));
                        continue;
                    }

                    parameters.Add(str.Substring(parameterSeparatorIndexes[i - 1], parameterSeparatorIndexes[i]));
                }

                parameters.Add(str.Substring(parameterSeparatorIndexes.Last() + 1));
            }

            return parameters.ToArray();
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
        private static bool TryParse(object dataContext, string str, out object obj)
        {
            obj = null;
            if (str.Equals("null"))
                return true;

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

            else if (str.Length >= 2 && str.Last() == '\'' && str.First() == '\'')
            {
                var ret = str.Substring(1, str.Length - 2);

                if (!ret.Contains("'"))
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
        /// <param name="dataContext">Object that is used as data context</param>
        /// <param name="str">String to parse</param>
        /// <param name="parameters">parameters for the method</param>
        /// <param name="obj">Object in which the parsed value is stored</param>
        /// <returns>true: parse succeeded, false: parse failed</returns>
        private static bool TryParse(object dataContext, string str, object[] parameters, out object obj)
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
                {
                    method = null;
                    foreach (var m in dataContext.GetType().GetMethods())
                    {
                        var mParameters = m.GetParameters();

                        if (m.GetDisplayName() != str || mParameters.Length != parameters.Length)
                            continue;

                        for (var i = 0; i < parameters.Length; i++)
                        {
                            if (parameters[i] == null)
                            {
                                method = mParameters[i].ParameterType.IsNullable()
                                    ? m
                                    : null;

                                break;
                            }

                            if (mParameters[i].ParameterType == parameters[i].GetType())
                            {
                                method = m;
                                break;
                            }

                            try
                            {
                                parameters[i] = Convert.ChangeType(parameters[i], mParameters[i].ParameterType);
                                method = m;
                                break;
                            }
                            catch (InvalidCastException)
                            {
                            }
                            catch (FormatException)
                            {
                            }

                            method = null;
                        }

                        if (method != null)
                            break;
                    }
                }


                if (method != null)
                {
                    obj = method.Invoke(dataContext, parameters);
                    return true;
                }
            }

            obj = str;
            return false;
        }

        #endregion answer parsing

        #endregion METHODS
    }
}