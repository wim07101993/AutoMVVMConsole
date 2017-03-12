using AutoConsole.Attributes;
using GalaSoft.MvvmLight;
using System;

namespace TestConsoleApp
{
    internal class PersonVM : ViewModelBase
    {
        #region PROPERTIES

        [ShowInConsole]
        public string Name { get; set; }
        public double Weight { get; set; }
        [ShowInConsole]
        public double Length { get; set; }

        #endregion PROPERTIES



        #region METHODS

        public override string ToString() => Name;

        [ShowInConsole]
        public string Say(string whatToSay)
        {
            return whatToSay;
        }

        [ShowInConsole]
        public string SayTwoThings(string whatToSay, string whatToSayNext)
        {
            return $"{whatToSay} and {whatToSayNext}";
        }

        [ShowInConsole]
        public void Jump()
        {
            Console.WriteLine(this + " jumped");
        }

        #endregion METHODS
    }
}
