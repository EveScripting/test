//	--------------------------------------------------------------------
//		(c) 2012 by DotNetMastermind
//
//		filename			: ViewModelBase.cs
//		clr-namespace	: SoftArcs.WPFSmartLibrary.MVVMCore
//		class(es)		: ViewModelBase
//
//	--------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using SharedComponents.EVE;

namespace SharedComponents.Utility
{
    /// <summary>
    ///     This should be the base class for all view models.
    ///     Every class with bounded properties should derive from this class,
    ///     because here is the implementation of the "INotifyPropertyChanged"-Interface located.
    /// </summary>
    [Serializable]
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private ConcurrentDictionary<string, object> propertyValueStorage;
        #region Constructor

        public ViewModelBase()
        {
            propertyValueStorage = new ConcurrentDictionary<string, object>();
        }

        #endregion

        /// <summary>
        ///     Set the value of the property and raise the [PropertyChanged] event
        ///     (only if the saved value and the new value are not equal)
        /// </summary>
        /// <typeparam name="T">The property type</typeparam>
        /// <param name="property">The property as a lambda expression</param>
        /// <param name="value">The new value of the property</param>
        /// ///
        /// <param name="rateLimit">The rate limit of INotifyPropertyChanged in milliseconds.</param>
        protected void SetValue<T>(Expression<Func<T>> property, T value)
        {
            var lambdaExpression = property as LambdaExpression;

            if (lambdaExpression == null)
                throw new ArgumentException("Invalid lambda expression", "Lambda expression return value can't be null");

            var propertyName = getPropertyName(lambdaExpression); 

            var storedValue = getValue<T>(propertyName);

            if (!Equals(storedValue, value))
            {
                propertyValueStorage[propertyName] = value;
                OnPropertyChanged(propertyName);
            }
        }

        /// <summary>
        ///     Get the value of the property
        /// </summary>
        /// <typeparam name="T">The property type</typeparam>
        /// <param name="property">The property as a lambda expression</param>
        /// <returns>The value of the given property (or the default value)</returns>
        protected T GetValue<T>(Expression<Func<T>> property)
        {
            var lambdaExpression = property as LambdaExpression;

            if (lambdaExpression == null)
                throw new ArgumentException("Invalid lambda expression", "Lambda expression return value can't be null");

            var propertyName = getPropertyName(lambdaExpression);

            return getValue<T>(propertyName);
        }

        /// <summary>
        ///     Try to get the value from the internal dictionary of the given property name
        /// </summary>
        /// <typeparam name="T">The property type</typeparam>
        /// <param name="propertyName">The name of the property</param>
        /// <returns>Retrieve the value from the internal dictionary</returns>
        private T getValue<T>(string propertyName)
        {
            object value;

            if (propertyValueStorage.TryGetValue(propertyName, out value))
                return (T) value;
            else
                return default(T);
        }

        /// <summary>
        ///     Extract the property name from a lambda expression
        /// </summary>
        /// <param name="lambdaExpression">The lambda expression with the property</param>
        /// <returns>The extracted property name</returns>
        private string getPropertyName(LambdaExpression lambdaExpression)
        {
            MemberExpression memberExpression;

            if (lambdaExpression.Body is UnaryExpression)
            {
                var unaryExpression = lambdaExpression.Body as UnaryExpression;
                memberExpression = unaryExpression.Operand as MemberExpression;
            }
            else
            {
                memberExpression = lambdaExpression.Body as MemberExpression;
            }

            return memberExpression.Member.Name;
        }

        //+-------------------------------------------------------------------------------------
        //+ The methods below are for the standard approach :
        //+ =================================================
        //+ public string property;
        //+ public string Property
        //+ {
        //+   get { return property; }
        //+   set
        //+   {
        //+      if (property != value)
        //+      {
        //+         property = value;
        //+         RaisePropertyChangedEvent( () => Property );
        //+      }
        //+   }
        //+ }
        //+-------------------------------------------------------------------------------------

        /// <summary>
        ///     "Raise" the PropertyChanged-Event (parameterless => similar to the dotNet Framework 4.5 version)
        ///     C# 5.0 : private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        /// </summary>
        protected void RaisePropertyChangedEvent()
        {
            // Get the call stack
            var stackTrace = new StackTrace();

            // Get the calling method name
            var callingMethodName = stackTrace.GetFrame(1).GetMethod().Name;

            // Check if the callingMethodName contains an underscore like in "set_SomeProperty"
            if (callingMethodName.Contains("_"))
            {
                // Extract the property name
                var propertyName = callingMethodName.Split('_')[1];

                if (PropertyChanged != null && propertyName != String.Empty)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        ///     "Raise" the PropertyChanged-Event through a lambda expression
        /// </summary>
        /// <param name="lambdaExpression"></param>
        protected void RaisePropertyChangedEvent(Expression<Func<object>> lambdaExpression)
        {
            // The changed property is not identified through a string but rather through the property itself
            if (PropertyChanged != null)
            {
                // Extract the body of the lambda expression
                var lambdaBody = lambdaExpression.Body as MemberExpression;

                if (lambdaBody == null)
                {
                    // If the Property is a primitive data type (i.e. bool) the body of the lambda expression
                    // have to be converted to an "UnaryExpression" to get the desired "MemberExpression"
                    var unaryExpression = lambdaExpression.Body as UnaryExpression;
                    lambdaBody = unaryExpression.Operand as MemberExpression;
                }

                // "Raise" the PropertyChanged-Event with the "real name" of the Property
                PropertyChanged(this, new PropertyChangedEventArgs(lambdaBody.Member.Name));
            }
        }

        /// <summary>
        ///     "Raise" the PropertyChanged-Event (string based with parameter check)
        ///     => It is recommended to use the lambda version or the parameterless version
        /// </summary>
        /// <param name="propertyName"></param>
        protected void RaisePropertyChangedEvent(string propertyName)
        {
            // This is an improved "string based" version to "raise" the PropertyChanged-Event
            // Bevor raising the PropertyChanged Event, the property name is being evaluated !
            checkPropertyName(propertyName);

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Private Helpers

        [Conditional("DEBUG")]
        private void checkPropertyName(string propertyName)
        {
            var propertyDescriptor = TypeDescriptor.GetProperties(this)[propertyName];
            if (propertyDescriptor == null)
            {
                var message = string.Format(null, "The property with the propertyName '{0}' doesn't exist.", propertyName);
                Debug.Fail(message);

                //throw new InvalidOperationException( string.Format( null, "The property with the propertyName '{0}' doesn't exist.", propertyName ) );
            }
        }

        #endregion // Private Helpers

        /// <summary>
        ///     "Raise" the PropertyChanged-Event (string based)
        ///     => It is recommended to use the lambda version or the parameterless version
        /// </summary>
        /// <param name="propertyName"></param>
        protected void RaisePropertyChangedEvent_Deprecated(string propertyName)
        {
            // This is the old "string based" version to raise the PropertyChanged-Event
            // There is no evaluation whether the property name is valid or not !!!
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region < INotifyPropertyChanged > Members

        /// <summary>
        ///     Is connected to a method which handle changes to a property (located in the WPF Data Binding Engine)
        /// </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Raise the [PropertyChanged] event
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                try
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
                catch (Exception e)
                {
                    Cache.Instance.Log("Exception: " + e);
                }
        }

        #endregion
    }
}