using AutoConsole.Attributes;


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
        
    }
}