// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
// 
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Web.UI;
using OpenQA.Selenium.DevTools;

namespace SharedComponents.Py
{
    public partial class PyObject
    {
        /// <summary>
        ///     Attribute cache
        /// </summary>
        private Dictionary<string, PyObject> _attributeCache;

        /// <summary>
        ///     Attribute name
        /// </summary>
        private String _attributeName;

        /// <summary>
        ///     Dictionary cache (used by DictionaryItem)
        /// </summary>
        private Dictionary<PyObject, PyObject> _dictionaryCache;

        /// <summary>
        ///     Item cache (used by Item)
        /// </summary>
        private Dictionary<int, PyObject> _itemCache;

        /// <summary>
        ///     Store if its a new reference
        /// </summary>
        private bool _newReference;

        /// <summary>
        ///     Reference to the actual python object
        /// </summary>
        private IntPtr _pyReference;

        /// <summary>
        ///     Reference to the overall PySharp object
        /// </summary>
        private PySharp _pySharp;

        /// <summary>
        ///     PyType cache
        /// </summary>
        private PyType? _pyType;

        private PyObject _parent;

        /// <summary>
        ///     Create a PyObject
        /// </summary>
        /// <param name="pySharp">The main PySharp object</param>
        /// <param name="pyReference">The Python Reference</param>
        /// <param name="newReference">Is this a new reference? (e.g. did the reference counter get increased?)</param>
        /// ///
        /// <param name="attributeName">Attribute name of the new PyObject</param>
        public PyObject(PySharp pySharp, IntPtr pyReference, bool newReference, string attributeName = "", PyObject parent = null)
        {
            _pyReference = pyReference;
            _parent = parent;
            _newReference = newReference;
            _pySharp = pySharp;
            _attributeName = attributeName;

            if (pySharp != null && _newReference)
                pySharp.AddReference(this);

            HandlePythonError();

            if (!IsValid)
                return;

            // Only build up cache if it actually a valid object
            _attributeCache = new Dictionary<string, PyObject>();
            _dictionaryCache = new Dictionary<PyObject, PyObject>();
            _itemCache = new Dictionary<int, PyObject>();
        }

        /// <summary>
        ///     Is this PyObject valid?
        /// </summary>
        /// <remarks>
        ///     Both null and none values are considered invalid
        /// </remarks>
        public bool IsValid => !IsNull && !IsNone;

        /// <summary>
        ///     Is this PyObject Null?
        /// </summary>
        public bool IsNull => _pyReference == IntPtr.Zero;

        public string AttributeName => _attributeName;

        /// <summary>
        ///     Is this PyObject a PyNone?
        /// </summary>
        public bool IsNone => _pyReference == Py.PyNoneStruct;

        public IntPtr PyRefPtr => _pyReference;

        /// <summary>
        ///     Return the Python Reference Count
        /// </summary>
        public int ReferenceCount => Py.GetRefCnt(this);

        public string Repr => (string)new PyObject(_pySharp, Py.PyObject_Repr(this), true);

        /// <summary>
        ///     Does this python object has this attribute string?
        /// </summary>
        /// <remarks>
        ///     -----
        /// </remarks>
        public bool HasAttrString(string s)
        {
            return Py.PyObject_HasAttrString(this, s) == 1 ? true : false;
        }


        /// <summary>
        ///     Is this python object callable, eg. a function?
        /// </summary>
        /// <remarks>
        ///     -----
        /// </remarks>
        public bool IsCallable(IntPtr ptr)
        {
            return Py.PyCallable_Check(ptr) == 1 ? true : false;
        }

        public bool IsCallable()
        {
            return IsCallable(this._pyReference);
        }

        /// <summary>
        ///     Cast a PyObject to a IntPtr
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static implicit operator IntPtr(PyObject pyObject)
        {
            return pyObject._pyReference;
        }

        /// <summary>
        ///     Release the PyObject's internal reference
        /// </summary>
        public void Release()
        {
            if (!IsNull && _newReference)
                Py.Py_DecRef(_pyReference);

            _pyReference = IntPtr.Zero;
        }

        /// <summary>
        ///     Attach the PyObject to a new PySharp object
        /// </summary>
        /// <param name="pySharp">New PySharp object</param>
        /// <returns>A new copy of itself</returns>
        public PyObject Attach(PySharp pySharp)
        {
            if (_newReference)
                Py.Py_IncRef(_pyReference);

            return new PyObject(pySharp, _pyReference, _newReference);
        }

        public void IncRef()
        {
            Py.Py_IncRef(_pyReference);
        }

        public void DecRef()
        {
            Py.Py_DecRef(_pyReference);
        }

        public bool GetValue(out object obj, out Type t)
        {
            obj = null;
            t = null;
            switch (this.GetPyType())
            {
                case PyType.NoneType:
                    obj = "None";
                    t = t = typeof(string);
                    return true;

                case PyType.StringType:
                case PyType.UnicodeType:
                case PyType.DerivedStringType:
                    obj = (object)ToUnicodeString();
                    t = typeof(string);
                    return true;
                case PyType.BoolType:
                case PyType.DerivedBoolType:
                    obj = (object)ToBool();
                    t = typeof(bool);
                    return true;
                case PyType.IntType:
                case PyType.DerivedIntType:
                    obj = (object)ToInt();
                    t = typeof(int);
                    return true;
                case PyType.LongType:
                case PyType.DerivedLongType:
                    obj = (object)ToLong();
                    t = typeof(long);
                    return true;
                case PyType.FloatType:
                case PyType.DerivedFloatType:
                    obj = (object)ToFloat();
                    t = typeof(float);
                    return true;
                default:
                    break;
            }
            return false;
        }

        /// <summary>
        ///     Return python type
        /// </summary>
        /// <returns></returns>
        public PyType GetPyType()
        {
            if (IsNull)
                _pyType = PyType.Invalid;

            if (IsNone)
                _pyType = PyType.NoneType;

            if (!_pyType.HasValue)
            {
                _pyType = Py.GetPyType(this);
                if (_pyType == PyType.DerivedTypeType || _pyType == PyType.DerivedDerivedTypeType)
                {
                    if (HasAttrString("__bluetype__") && this["__bluetype__"].IsValid && this["__bluetype__"].ToUnicodeString().Equals("blue.List"))
                    {
                        _pyType = PyType.BlueList;
                    }
                }
            }

            return _pyType.Value;
        }

        public int GetCallableParamCount()
        {
            if (!this.IsCallable())
                return 0;

            var funcCode = this["func_code"];
            if (!funcCode.IsValid)
                return 0;

            var co_argcount = funcCode["co_argcount"];
            if (!co_argcount.IsValid)
                return 0;

            return co_argcount.ToInt();
        }

        public PyObject this[string key] => Attribute(key);

        /// <summary>
        ///     Returns an attribute from the current Python object
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public PyObject Attribute(string attribute, bool hasAttrCheck = true)
        {

            if (_attributeCache == null)
                return PySharp.PyZero;

            PyObject result;
            if (!_attributeCache.TryGetValue(attribute, out result))
            {

                if (!IsValid || string.IsNullOrEmpty(attribute) || attribute.ToLower().Equals("dead"))
                {
                    _attributeCache[attribute] = PySharp.PyZero;
                    return PySharp.PyZero;
                }

                if (hasAttrCheck && !HasAttrString(attribute))
                {
                    _attributeCache[attribute] = PySharp.PyZero;
                    return PySharp.PyZero;
                }

                result = new PyObject(_pySharp, Py.PyObject_GetAttrString(this, attribute), true, attribute, this);
                _attributeCache[attribute] = result;
            }
            return result;
        }

        public PyObject DeepCopy()
        {
            var copy = _pySharp.Import("copy");
            if (!copy.IsValid)
                return null;

            var deepCopy = copy.Attribute("deepcopy");
            if (!deepCopy.IsValid)
                return null;

            return deepCopy.CallThis(this);
        }

        /// <summary>
        ///     Returns a dictionary of all attributes within the current Python object
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, PyObject> Attributes()
        {
            if (!IsValid)
                return new Dictionary<string, PyObject>();

            PyObject AttrClosure(string s)
            {
                return Attribute(s);
            }

            return new PyObject(_pySharp, Py.PyObject_Dir(this), true).ToList<string>().ToDictionary(attr => attr, (Func<string, PyObject>)AttrClosure);
        }

        /// <summary>
        ///     Logs debug information about this PyObject
        /// </summary>
        public string LogObject()
        {

            if (!this.IsValid)
                return string.Empty;

            var s = string.Empty;
            s += $"Path [{GetPath()}]";
            s += string.Format("\nDumping attributes of {0}...\n", Repr);
            s += "\n";
            s += $"Type: {this.GetPyType().ToString()}";
            s += "\n";
            s += "Attributes:";
            s += "\n";
            foreach (var pair in Attributes())
            {
                var k = string.Format("  {0} : {1}\n", pair.Key, pair.Value.Repr);
                s += k;
            }
            s += "\n";
            return s;
        }

        /// <summary>
        ///     Returns a dictionary item from the current Python object
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public PyObject DictionaryItem(int key, bool forceAsDict = false)
        {
            if (_pySharp == null)
                return PySharp.PyZero;

            return DictionaryItem(_pySharp.From(key), forceAsDict);
        }

        /// <summary>
        ///     Returns a dictionary item from the current Python object
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public PyObject DictionaryItem(long key)
        {
            if (_pySharp == null)
                return PySharp.PyZero;

            return DictionaryItem(_pySharp.From(key));
        }

        /// <summary>
        ///     Returns a dictionary item from the current Python object
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public PyObject DictionaryItem(string key)
        {
            if (_pySharp == null)
                return PySharp.PyZero;

            return DictionaryItem(_pySharp.From(key));
        }

        private PyObject GetIterator()
        {
            try
            {
                if (_pySharp == null || !IsValid)
                    return PySharp.PyZero;

                if (GetPyType() != PyType.SetType) //TODO: add other types
                    return PySharp.PyZero;

                PyObject result;
                result = new PyObject(_pySharp, Py.PyObject_GetIter(this._pyReference), true);
                return result;
            }
            finally
            {
                HandlePythonError();
            }
        }

        private PyObject InteratorNext(PyObject py)
        {
            if (_pySharp == null || !IsValid || !py.IsValid)
                return PySharp.PyZero;
            try
            {
                PyObject result;
                result = new PyObject(_pySharp, Py.PyIter_Next(py._pyReference), true);
                return result;
            }

            finally
            {
                HandlePythonError();
            }
        }



        /// <summary>
        ///     Returns a dictionary item from the current Python object
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public PyObject DictionaryItem(PyObject key, bool forceAsDict = false)
        {
            if (!IsValid || key.IsNull)
                return PySharp.PyZero;

            try
            {
                PyObject result;
                if (!_dictionaryCache.TryGetValue(key, out result))
                {
                    var thisType = GetPyType();
                    if (forceAsDict || thisType == PyType.DerivedDictType || thisType == PyType.DictType)
                        result = new PyObject(_pySharp, Py.PyDict_GetItem(this, key), false);
                    else
                        result = new PyObject(_pySharp, Call("__getitem__", key), false);
                    _dictionaryCache[key] = result;
                }
                return result;
            }
            finally
            {
                HandlePythonError();
            }
        }

        /// <summary>
        ///     Returns a list item from the current Python object
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public PyObject GetItemAt(int index)
        {
            return GetItemAt(index, GetPyType());
        }



        /// <summary>
        ///     Returns a list item from the current Python object
        /// </summary>
        /// <param name="index"></param>
        /// <param name="type">Force the PyType to List or Tuple</param>
        /// <returns></returns>
        public PyObject GetItemAt(int index, PyType type)
        {
            if (!IsValid)
                return PySharp.PyZero;

            try
            {
                var decRef = false;
                var getItem = type == PyType.TupleType || type == PyType.DerivedTupleType ? (Func<IntPtr, int, IntPtr>)Py.PyTuple_GetItem : Py.PyList_GetItem;

                if (type == PyType.BlueList)
                {
                    decRef = true;
                    getItem = delegate (IntPtr ptr, int i)
                    {
                        var n = this._pySharp.From(i);
                        return Py.PyObject_GetItem(this, n);
                    };
                }

                if (!_itemCache.TryGetValue(index, out var result))
                {
                    result = new PyObject(_pySharp, getItem(this, index), decRef);
                    _itemCache[index] = result;
                }
                return result;
            }
            finally
            {
                HandlePythonError();
            }
        }

        /// <summary>
        ///     Returns the size of the list or tuple
        /// </summary>
        /// <returns></returns>
        public int Size()
        {
            return Size(GetPyType());
        }

        /// <summary>
        ///     Returns the size of the given type (tuple, otherwise list)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public int Size(PyType type)
        {
            if (!IsValid)
                return -1;
            try
            {
                if (type == PyType.BlueList)
                {
                    return Py.PyObject_Size(this);
                }

                if (type == PyType.SetType)
                {  //TODO: add other types
                    return (int)Py.PySet_Size(this._pyReference);
                }
                var getSize = type == PyType.TupleType || type == PyType.DerivedTupleType ? (Func<IntPtr, int>)Py.PyTuple_Size : Py.PyList_Size;
                return getSize(this);
            }
            finally
            {
                HandlePythonError();
            }
        }

        /// <summary>
        ///     Call a python function
        /// </summary>
        /// <param name="function"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        public PyObject Call(string function, params object[] parms)
        {
            var func = Attribute(function);

            if (!func.IsValid || !IsCallable(func._pyReference))
                return PySharp.PyZero;

            return func.CallThis(parms);
        }

        public PyObject CallDbg(string function, params object[] parms)
        {
            var func = Attribute(function);

            if (!func.IsValid || !IsCallable(func._pyReference))
                return PySharp.PyZero;

            Console.WriteLine($"Calling Function {function}");

            return func.CallThis(parms);
        }

        /// <summary>
        ///     Call a python function
        /// </summary>
        /// <param name="function"></param>
        /// <param name="keywords"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        public PyObject CallWithKeywords(string function, Dictionary<string, object> keywords, params object[] parms)
        {
            var func = Attribute(function);
            if (!func.IsValid || !IsCallable(func._pyReference))
                return PySharp.PyZero;
            return func.CallThisWithKeywords(keywords, parms);
        }

        /// <summary>
        ///     Call this PyObject as a python function
        /// </summary>
        /// <param name="parms"></param>
        /// <returns></returns>
        public PyObject CallThis(params object[] parms)
        {
            if (!this.IsCallable(_pyReference))
                return PySharp.PyZero;
            return CallThisWithKeywords(null, parms);
        }


        public SharpDX.Matrix ConvertToMaxtix()
        {
            SharpDX.Matrix m = new SharpDX.Matrix();
            int c = 0;
            foreach (var col in this.ToList())
            {
                int r = 0;
                foreach (var row in col.ToList())
                {
                    m[c, r] = row.ToFloat();
                    r++;
                }
                c++;
            }
            return m;
        }

        /// <summary>
        ///     Call this PyObject as a python function
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        private PyObject CallThisWithKeywords(Dictionary<string, object> keywords, params object[] parms)
        {
            if (!IsValid)
                return PySharp.PyZero;

            if (_pySharp == null)
                throw new NotImplementedException();

            var pyKeywords = PySharp.PyZero;
            if (keywords != null && keywords.Keys.Any())
            {
                pyKeywords = new PyObject(_pySharp, Py.PyDict_New(), true);
                foreach (var item in keywords)
                {
                    var pyValue = _pySharp.From(item.Value);

                    if (pyValue == null || pyValue.IsNull)
                        throw new NotImplementedException();

                    Py.PyDict_SetItem(pyKeywords, _pySharp.From(item.Key), pyValue);
                }
            }

            var pyParms = new List<PyObject>();
            foreach (var parm in parms)
            {
                var pyParm = _pySharp.From(parm);

                if (pyParm == null || pyParm.IsNull)
                    throw new NotImplementedException();

                // Fail if any parameter is invalid (PyNone is a valid parameter)
                if (pyParm.IsNull)
                    return PySharp.PyZero;

                pyParms.Add(pyParm);
            }

            var format = "(" + string.Join("", pyParms.Select(dummy => "O").ToArray()) + ")";
            PyObject pyArgs = null;
            if (pyParms.Count == 0)
                pyArgs = new PyObject(_pySharp, Py.Py_BuildValue(format), true);
            if (pyParms.Count == 1)
                pyArgs = new PyObject(_pySharp, Py.Py_BuildValue(format, pyParms[0]), true);
            if (pyParms.Count == 2)
                pyArgs = new PyObject(_pySharp, Py.Py_BuildValue(format, pyParms[0], pyParms[1]), true);
            if (pyParms.Count == 3)
                pyArgs = new PyObject(_pySharp, Py.Py_BuildValue(format, pyParms[0], pyParms[1], pyParms[2]), true);
            if (pyParms.Count == 4)
                pyArgs = new PyObject(_pySharp, Py.Py_BuildValue(format, pyParms[0], pyParms[1], pyParms[2], pyParms[3]), true);
            if (pyParms.Count == 5)
                pyArgs = new PyObject(_pySharp, Py.Py_BuildValue(format, pyParms[0], pyParms[1], pyParms[2], pyParms[3], pyParms[4]), true);
            if (pyParms.Count == 6)
                pyArgs = new PyObject(_pySharp, Py.Py_BuildValue(format, pyParms[0], pyParms[1], pyParms[2], pyParms[3], pyParms[4], pyParms[5]), true);
            if (pyParms.Count == 7)
                pyArgs = new PyObject(_pySharp, Py.Py_BuildValue(format, pyParms[0], pyParms[1], pyParms[2], pyParms[3], pyParms[4], pyParms[5], pyParms[6]),
                    true);
            if (pyParms.Count == 8)
                pyArgs = new PyObject(_pySharp,
                    Py.Py_BuildValue(format, pyParms[0], pyParms[1], pyParms[2], pyParms[3], pyParms[4], pyParms[5], pyParms[6], pyParms[7]), true);

            if (pyArgs == null)
                throw new NotImplementedException();

            return new PyObject(_pySharp, Py.PyEval_CallObjectWithKeywords(this, pyArgs, pyKeywords), true);
        }

        public int GetStringSize => GetPyType() == PyType.StringType ? Py.PyString_Size(this._pyReference) : 0;

        public IntPtr StringRepr => GetPyType() == PyType.StringType ? Py.PyString_AsString(this._pyReference) : IntPtr.Zero;

        public byte[] GetStringBytes()
        {
            var res = new byte[0];
            if (GetPyType() == PyType.StringType)
            {
                var size = GetStringSize;
                var ptr = StringRepr;
                res = new byte[size];
                Marshal.Copy(ptr, res, 0, size);
            }
            return res;
        }

        /// <summary>
        /// Return the PyObject as a string
        /// </summary>
        /// <param name="p">Internal use only to prevent endless loops due circlular tuples/lists</param>
        /// <returns></returns>
        public string ToUnicodeString(bool p = true)
        {
            try
            {
                if (!IsValid)
                    return null;
                var type = GetPyType();


                // Manually convert from buffers to string
                if (type == PyType.UnicodeType)
                {
                    var size = Py.PyUnicodeUCS2_GetSize(this._pyReference);
                    if (size <= 0)
                        return null;

                    var ptr = Py.PyUnicodeUCS2_AsUnicode(this._pyReference);
                    if (ptr == IntPtr.Zero)
                        return null;

                    return Marshal.PtrToStringUni(ptr, size);
                }
                else if (type == PyType.StringType)
                {
                    var size = Py.PyString_Size(this._pyReference);
                    if (size <= 0)
                        return null;

                    var ptr = Py.PyString_AsString(this._pyReference);
                    if (ptr == IntPtr.Zero)
                        return null;
                    return Marshal.PtrToStringAnsi(ptr, size);
                }
                else if (type == PyType.BoolType)
                {
                    return this.ToBool().ToString();
                }
                else if (type == PyType.IntType)
                {
                    return this.ToInt().ToString();
                }
                else if (type == PyType.FloatType)
                {
                    return this.ToFloat().ToString();
                }
                else if (type == PyType.LongType)
                {
                    return this.ToFloat().ToString();
                }
                else if (p && (type == PyType.TupleType || type == PyType.ListType))
                {
                    var list = this.ToList();
                    var ret = "(";
                    foreach (var obj in list)
                    {
                        ret += obj.ToUnicodeString(false) + ", ";
                    }
                    ret += ")";
                    return ret;
                }
                else
                {
                    var obj = Py.PyObject_Str(this);
                    if (obj == IntPtr.Zero)
                        return string.Empty;

                    var ptr = Py.PyString_AsString(obj);
                    if (ptr == IntPtr.Zero)
                        return null;
                    return Marshal.PtrToStringAnsi(ptr);
                }


            }
            finally
            {
                HandlePythonError();
            }
        }

        /// <summary>
        ///     Cast a PyObject to a string
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator string(PyObject pyObject)
        {
            return pyObject.ToUnicodeString();
        }

        /// <summary>
        ///     Returns the PyObject as a bool
        /// </summary>
        /// <returns></returns>
        public bool ToBool()
        {
            return ToInt() == 1;
        }

        /// <summary>
        ///     Cast a PyObject to an bool
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator bool(PyObject pyObject)
        {
            return pyObject.ToBool();
        }

        /// <summary>
        ///     Cast a PyObject to a nullable bool
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator bool?(PyObject pyObject)
        {
            return pyObject.IsValid ? pyObject.ToBool() : (bool?)null;
        }

        /// <summary>
        ///     Returns the PyObject as an integer
        /// </summary>
        /// <returns></returns>
        public int ToInt()
        {
            try
            {
                return IsValid ? Py.PyLong_AsLong(this) : 0;
            }
            finally
            {
                HandlePythonError();
            }
        }

        /// <summary>
        ///     Cast a PyObject to an integer
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator int(PyObject pyObject)
        {
            return pyObject.ToInt();
        }

        /// <summary>
        ///     Cast a PyObject to a nullable integer
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator int?(PyObject pyObject)
        {
            return pyObject.IsValid ? pyObject.ToInt() : (int?)null;
        }

        /// <summary>
        ///     Returns the PyObject as a long
        /// </summary>
        /// <returns></returns>
        public long ToLong()
        {
            try
            {
                return IsValid ? Py.PyLong_AsLongLong(this) : 0;
            }
            finally
            {
                HandlePythonError();
            }
        }

        /// <summary>
        ///     Cast a PyObject to a nullable long
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator long?(PyObject pyObject)
        {
            return pyObject.IsValid ? pyObject.ToLong() : (long?)null;
        }

        /// <summary>
        ///     Cast a PyObject to an integer
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator long(PyObject pyObject)
        {
            return pyObject.ToLong();
        }

        /// <summary>
        ///     Returns the PyObject as a double
        /// </summary>
        /// <returns></returns>
        public double ToDouble(double defaultValue = 0.0d)
        {
            try
            {
                return IsValid ? Py.PyFloat_AsDouble(this) : defaultValue;
            }
            finally
            {
                HandlePythonError();
            }
        }

        /// <summary>
        ///     Returns the PyObject as a float
        /// </summary>
        /// <returns></returns>
        public float ToFloat()
        {
            try
            {
                return IsValid ? (float)Py.PyFloat_AsDouble(this) : 0;
            }
            finally
            {
                HandlePythonError();
            }
        }

        /// <summary>
        ///     Cast a PyObject to an integer
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator double(PyObject pyObject)
        {
            return pyObject.ToDouble();
        }

        /// <summary>
        ///     Cast a PyObject to a nullable long
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator double?(PyObject pyObject)
        {
            return pyObject.IsValid ? pyObject.ToDouble() : (double?)null;
        }

        /// <summary>
        ///     Cast a PyObject to an integer
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator float(PyObject pyObject)
        {
            return pyObject.ToFloat();
        }

        /// <summary>
        ///     Cast a PyObject to a nullable long
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator float?(PyObject pyObject)
        {
            return pyObject.IsValid ? pyObject.ToFloat() : (float?)null;
        }

        /// <summary>
        ///     Returns the PyObject as a DateTime
        /// </summary>
        /// <returns></returns>
        public DateTime ToDateTime()
        {
            var value = PY_EPOCHE_DATE.AddTicks(ToLong());
            if (IsValid)
            {
                var diff = _pySharp.TimeDiffNow;
                value = value.AddMilliseconds(diff);
            }
            return value;
        }

        // this is the epoch based on pythons internal time.time
        public static readonly DateTime PY_EPOCH_TIME_TIME = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Based on seconds (normal internal python)
        /// </summary>
        /// <returns></returns>
        public DateTime ToDateTimeFromPythonTime()
        {
            var value = PY_EPOCH_TIME_TIME.AddSeconds(ToLong());
            if (IsValid)
            {
                var diff = _pySharp.TimeDiffNow;
                value = value.AddMilliseconds(diff);
            }
            return value;
        }

        public DateTime ToDateTimeFromPythonDateTime()
        {
            var year = this["year"];
            if (year.IsValid)
            {
                var month = this["month"].ToInt();
                var day = this["day"].ToInt();
                var hour = this["hour"].ToInt();
                var minute = this["minute"].ToInt();
                var second = this["second"].ToInt();
                var microsecond = this["microsecond"].ToInt();
                int ticks = microsecond * 10;
                DateTime dateTime = new DateTime(year.ToInt(), month, day, hour, minute, second, 0).AddTicks(ticks);
                return dateTime;
            }

            return default;
        }

        public DateTime ToDateTimeExact()
        {
            var value = PY_EPOCHE_DATE.AddTicks(ToLong());
            return value;
        }

        public static readonly DateTime PY_EPOCHE_DATE = new DateTime(1601, 1, 1);

        /// <summary>
        ///     Cast a PyObject to an integer
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator DateTime(PyObject pyObject)
        {
            return pyObject.ToDateTime();
        }

        /// <summary>
        ///     Cast a PyObject to a nullable long
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator DateTime?(PyObject pyObject)
        {
            return pyObject.IsValid ? pyObject.ToDateTime() : (DateTime?)null;
        }

        /// <summary>
        ///     Returns the PyObject as a list
        /// </summary>
        /// <returns></returns>
        public List<PyObject> ToList()
        {
            return ToList<PyObject>();
        }

        /// <summary>
        ///     Cast a PyObject to a List
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator List<PyObject>(PyObject pyObject)
        {
            return pyObject.ToList();
        }

        /// <summary>
        ///     Cast a PyObject to a List
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator List<int>(PyObject pyObject)
        {
            return pyObject.ToList<int>();
        }

        /// <summary>
        ///     Cast a PyObject to a List
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator List<long>(PyObject pyObject)
        {
            return pyObject.ToList<long>();
        }

        /// <summary>
        ///     Cast a PyObject to a List
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator List<string>(PyObject pyObject)
        {
            return pyObject.ToList<string>();
        }

        /// <summary>
        ///     Returns the PyObject as a list
        /// </summary>
        /// <returns></returns>
        public List<T> ToList<T>()
        {
            var result = new List<T>();
            if (!IsValid)
                return result;

            try
            {
                if (GetPyType() == PyType.SetType) //TODO: add other types
                    return SetToList<T>();

                var size = Size();
                for (var i = 0; i < size; i++)
                {
                    var item = GetItemAt(i);

                    object oItem = null;
                    if (typeof(T) == typeof(int))
                        oItem = item.ToInt();
                    if (typeof(T) == typeof(long))
                        oItem = item.ToLong();
                    if (typeof(T) == typeof(float))
                        oItem = item.ToFloat();
                    if (typeof(T) == typeof(double))
                        oItem = item.ToDouble();
                    if (typeof(T) == typeof(string))
                        oItem = item.ToUnicodeString();
                    if (typeof(T) == typeof(PyObject))
                        oItem = item;

                    if (oItem == null)
                        continue;

                    result.Add((T)oItem);
                }

                return result;
            }
            finally
            {
                HandlePythonError();
            }
        }

        private List<T> SetToList<T>()
        {
            var result = new List<T>();
            if (!IsValid || GetPyType() != PyType.SetType) //TODO: add other set types
                return result;
            try
            {
                var iter = this.GetIterator();
                for (PyObject obj; (obj = InteratorNext(iter)).IsValid && iter.IsValid;) // can't declare a variable within a while loop in c#
                {
                    var item = obj;
                    object oItem = null;
                    if (typeof(T) == typeof(int))
                        oItem = item.ToInt();
                    if (typeof(T) == typeof(long))
                        oItem = item.ToLong();
                    if (typeof(T) == typeof(float))
                        oItem = item.ToFloat();
                    if (typeof(T) == typeof(double))
                        oItem = item.ToDouble();
                    if (typeof(T) == typeof(string))
                        oItem = item.ToUnicodeString();
                    if (typeof(T) == typeof(PyObject))
                        oItem = item;

                    if (oItem == null)
                        continue;

                    result.Add((T)oItem);
                }
            }
            finally
            {
                HandlePythonError();
            }

            return result;
        }

        /// <summary>
        ///     Returns the PyObject as a dictionary
        /// </summary>
        /// <returns></returns>
        public Dictionary<PyObject, PyObject> ToDictionary()
        {
            return ToDictionary<PyObject>();
        }

        /// <summary>
        ///     Cast a PyObject to a dictionary
        /// </summary>
        /// <returns></returns>
        public Dictionary<TKey, PyObject> ToDictionary<TKey>()
        {
            var result = new Dictionary<TKey, PyObject>();
            if (!IsValid)
                return result;
            try
            {
                var keys = Call("keys").ToList();
                foreach (var key in keys)
                {
                    object oKey = null;

                    if (typeof(TKey) == typeof(int))
                        oKey = key.ToInt();
                    if (typeof(TKey) == typeof(long))
                        oKey = key.ToLong();
                    if (typeof(TKey) == typeof(float))
                        oKey = key.ToFloat();
                    if (typeof(TKey) == typeof(double))
                        oKey = key.ToDouble();
                    if (typeof(TKey) == typeof(string))
                        oKey = key.ToUnicodeString();
                    if (typeof(TKey) == typeof(PyObject))
                        oKey = key;

                    if (oKey == null)
                        continue;

                    result[(TKey)oKey] = DictionaryItem(key);
                }

                return result;
            }
            finally
            {
                HandlePythonError();
            }
        }

        /// <summary>
        ///     Cast a PyObject to a dictionary
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator Dictionary<PyObject, PyObject>(PyObject pyObject)
        {
            return pyObject.ToDictionary();
        }

        /// <summary>
        ///     Cast a PyObject to a dictionary
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator Dictionary<int, PyObject>(PyObject pyObject)
        {
            return pyObject.ToDictionary<int>();
        }

        /// <summary>
        ///     Cast a PyObject to a dictionary
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator Dictionary<long, PyObject>(PyObject pyObject)
        {
            return pyObject.ToDictionary<long>();
        }

        /// <summary>
        ///     Cast a PyObject to a dictionary
        /// </summary>
        /// <param name="pyObject"></param>
        /// <returns></returns>
        public static explicit operator Dictionary<string, PyObject>(PyObject pyObject)
        {
            return pyObject.ToDictionary<string>();
        }

        public List<string> GetPathRecursive(List<string> list = null)
        {
            if (list == null)
                list = new List<string>();

            if (_parent == null)
            {
                list.Reverse();
                return list;
            }

            list.Add(this.AttributeName);
            return GetPathRecursive(list);
        }

        public String GetPath()
        {
            var s = GetPathRecursive();
            var res = String.Join(".", s.ToArray());
            return res;
        }

        /// <summary>
        ///     Set an attribute value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetAttribute<T>(string attribute, T value)
        {
            var pyValue = PySharp.PyZero;

            var oValue = (object)value;
            if (value is PyObject)
                pyValue = (PyObject)oValue;

            // Only allow type conversions if we have a PySharp object reference
            if (_pySharp != null)
            {
                if (oValue is bool)
                    pyValue = _pySharp.From((bool)oValue);
                if (oValue is int)
                    pyValue = _pySharp.From((int)oValue);
                if (oValue is long l)
                    pyValue = _pySharp.From(l);
                if (oValue is float)
                    pyValue = _pySharp.From((float)oValue);
                if (oValue is double)
                    pyValue = _pySharp.From((double)oValue);
                if (oValue is string)
                    pyValue = _pySharp.From((string)oValue);
                if (oValue is IEnumerable<PyObject>)
                    pyValue = _pySharp.From((IEnumerable<PyObject>)oValue);
                if (oValue is IEnumerable<int>)
                    pyValue = _pySharp.From((IEnumerable<int>)oValue);
                if (oValue is IEnumerable<long>)
                    pyValue = _pySharp.From((IEnumerable<long>)oValue);
                if (oValue is IEnumerable<float>)
                    pyValue = _pySharp.From((IEnumerable<float>)oValue);
                if (oValue is IEnumerable<double>)
                    pyValue = _pySharp.From((IEnumerable<double>)oValue);
                if (oValue is IEnumerable<string>)
                    pyValue = _pySharp.From((IEnumerable<string>)oValue);
            }

            try
            {
                if (IsValid && !pyValue.IsNull)
                    return Py.PyObject_SetAttrString(this, attribute, pyValue) != -1;

                return false;
            }
            finally
            {
                HandlePythonError();
            }
        }

        public bool SetTriple(string attribute, object a, object b, object c)
        {

            if (!IsValid)
                return false;

            var pyValue = PySharp.PyZero;
            var list = new List<object>() { a, b, c };
            var returnList = new List<PyObject>();

            foreach (var value in list)
            {
                var oValue = (object)value;
                if (value is PyObject)
                    pyValue = (PyObject)oValue;

                // Only allow type conversions if we have a PySharp object reference
                if (_pySharp != null)
                {
                    if (oValue is bool)
                        pyValue = _pySharp.From((bool)oValue);
                    if (oValue is int)
                        pyValue = _pySharp.From((int)oValue);
                    if (oValue is long l)
                        pyValue = _pySharp.From(l);
                    if (oValue is float)
                        pyValue = _pySharp.From((float)oValue);
                    if (oValue is double)
                        pyValue = _pySharp.From((double)oValue);
                    if (oValue is string)
                        pyValue = _pySharp.From((string)oValue);
                    if (oValue is IEnumerable<PyObject>)
                        pyValue = _pySharp.From((IEnumerable<PyObject>)oValue);
                    if (oValue is IEnumerable<int>)
                        pyValue = _pySharp.From((IEnumerable<int>)oValue);
                    if (oValue is IEnumerable<long>)
                        pyValue = _pySharp.From((IEnumerable<long>)oValue);
                    if (oValue is IEnumerable<float>)
                        pyValue = _pySharp.From((IEnumerable<float>)oValue);
                    if (oValue is IEnumerable<double>)
                        pyValue = _pySharp.From((IEnumerable<double>)oValue);
                    if (oValue is IEnumerable<string>)
                        pyValue = _pySharp.From((IEnumerable<string>)oValue);
                }

                returnList.Add(pyValue);
            }

            if (returnList.Any(o => !o.IsValid))
                return false;

            try
            {
                var tuple = new PyObject(_pySharp, Py.PyTuple_Pack(3, returnList[0], returnList[1], returnList[2]), true);

                if (!tuple.IsValid)
                    return false;

                return Py.PyObject_SetAttrString(this, attribute, tuple) != -1;
            }
            finally
            {
                HandlePythonError();
            }
        }

        public static PyObject CreateTuple(PySharp pySharp, params object[] values)
        {
            var pyValue = PySharp.PyZero;
            var returnList = new List<PyObject>();

            foreach (var value in values)
            {
                var oValue = (object)value;
                if (value is PyObject)
                    pyValue = (PyObject)oValue;

                // Only allow type conversions if we have a PySharp object reference
                if (pySharp != null)
                {
                    if (oValue is bool)
                        pyValue = pySharp.From((bool)oValue);
                    if (oValue is int)
                        pyValue = pySharp.From((int)oValue);
                    if (oValue is long l)
                        pyValue = pySharp.From(l);
                    if (oValue is float)
                        pyValue = pySharp.From((float)oValue);
                    if (oValue is double)
                        pyValue = pySharp.From((double)oValue);
                    if (oValue is string)
                        pyValue = pySharp.From((string)oValue);
                    if (oValue is IEnumerable<PyObject>)
                        pyValue = pySharp.From((IEnumerable<PyObject>)oValue);
                    if (oValue is IEnumerable<int>)
                        pyValue = pySharp.From((IEnumerable<int>)oValue);
                    if (oValue is IEnumerable<long>)
                        pyValue = pySharp.From((IEnumerable<long>)oValue);
                    if (oValue is IEnumerable<float>)
                        pyValue = pySharp.From((IEnumerable<float>)oValue);
                    if (oValue is IEnumerable<double>)
                        pyValue = pySharp.From((IEnumerable<double>)oValue);
                    if (oValue is IEnumerable<string>)
                        pyValue = pySharp.From((IEnumerable<string>)oValue);
                }

                returnList.Add(pyValue);
            }

            if (returnList.Any(o => !o.IsValid))
                return PySharp.PyZero;

            try
            {

                var tuple = new PyObject(pySharp, Py.PyTuple_New(returnList.Count), true);

                if (!tuple.IsValid)
                    return PySharp.PyZero;

                var i = 0;
                foreach (var py in returnList)
                {
                    Py.Py_IncRef(py);
                    Py.PyTuple_SetItem(tuple, i, py);
                    i++;
                }

                return tuple;

            }
            finally
            {
                HandlePythonError(pySharp);
            }
        }



        /// <summary>
        ///     Clear this PyObject (the PyObject must be a List, Tuple or Dictionary)
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     For a list attribute, the list is cleared
        ///     For a Dictionary attribute, the dictionary is cleared
        /// </remarks>
        public bool Clear()
        {
            return Clear(GetPyType());
        }

        public bool PySet_Contains<T>(T value)
        {
            try
            {
                if (GetPyType() == PyType.SetType) //TODO: add other set types                
                {
                    var pyValue = PySharp.PyZero;

                    var oValue = (object)value;
                    if (value is PyObject)
                        pyValue = (PyObject)oValue;
                    // Only allow type conversions if we have a PySharp object reference
                    if (_pySharp != null)
                    {
                        if (oValue is bool)
                            pyValue = _pySharp.From((bool)oValue);
                        if (oValue is int)
                            pyValue = _pySharp.From((int)oValue);
                        if (oValue is long)
                            pyValue = _pySharp.From((long)oValue);
                        if (oValue is float)
                            pyValue = _pySharp.From((float)oValue);
                        if (oValue is double)
                            pyValue = _pySharp.From((double)oValue);
                        if (oValue is string)
                            pyValue = _pySharp.From((string)oValue);
                        if (oValue is IEnumerable<PyObject>)
                            pyValue = _pySharp.From((IEnumerable<PyObject>)oValue);
                        if (oValue is IEnumerable<int>)
                            pyValue = _pySharp.From((IEnumerable<int>)oValue);
                        if (oValue is IEnumerable<long>)
                            pyValue = _pySharp.From((IEnumerable<long>)oValue);
                        if (oValue is IEnumerable<float>)
                            pyValue = _pySharp.From((IEnumerable<float>)oValue);
                        if (oValue is IEnumerable<double>)
                            pyValue = _pySharp.From((IEnumerable<double>)oValue);
                        if (oValue is IEnumerable<string>)
                            pyValue = _pySharp.From((IEnumerable<string>)oValue);
                    }
                    else
                    {
                        return false;
                    }

                    if (Py.PySet_Contains(this, pyValue) == 1)
                    {
                        //Console.WriteLine("And this set contains the value.");
                        return true;
                    }
                    else
                    {
                    }

                    return false;
                }
            }

            finally
            {
                HandlePythonError();
            }

            return false;
        }

        /// <summary>
        ///     Clear this PyObject (the PyObject must be a List or Dictionary)
        /// </summary>
        /// <param name="pyType">Force this Python Type</param>
        /// <returns></returns>
        /// <remarks>
        ///     For a list attribute, the list is cleared
        ///     For a Dictionary attribute, the dictionary is cleared
        /// </remarks>
        public bool Clear(PyType pyType)
        {
            try
            {
                switch (pyType)
                {
                    case PyType.ListType:
                    case PyType.DerivedListType:
                        return Py.PyList_SetSlice(this, 0, Size() - 1, PySharp.PyZero) == 0;

                    case PyType.DictType:
                    case PyType.DictProxyType:
                    case PyType.DerivedDictType:
                    case PyType.DerivedDictProxyType:
                        return ToDictionary().All(item => Py.PyDict_DelItem(this, item.Key) == 0);
                }

                return false;
            }
            finally
            {
                HandlePythonError();
            }
        }

        /// <summary>
        ///     Handle a python error (e.g. clear error)
        /// </summary>
        /// <remarks>
        ///     This checks if an error actually occured and clears the error
        /// </remarks>
        public static void HandlePythonError(PySharp pySharp)
        {
            // TODO: Save the python error to a log file?
            if (Py.PyErr_Occurred() != IntPtr.Zero)
            {
                var stackTrace = new StackTrace(true);
                Console.WriteLine("C# Stacktrace:");
                Console.WriteLine(stackTrace);

                //Py.PyErr_PrintEx(0); // don't use this, eve is capturing std:error

                Py.PyErr_Fetch(out var a, out var b, out var c);
                var pyType = new PyObject(pySharp, a, true);
                var pyValue = new PyObject(pySharp, b, true);
                var pyTraceback = new PyObject(pySharp, c, true);

                if (pyType.IsValid)
                {
                    // Get the exceptions type of the exception being handled
                    Console.WriteLine($"TYPE -- {pyType.Repr}");
                }

                if (pyValue.IsValid)
                {
                    // The exception parameters
                    Console.WriteLine($"VALUE --  {pyValue.Repr}");
                }

                if (pyTraceback.IsValid)
                {
                    // The callstack
                    Console.WriteLine($"TRACEBACK -- {pyTraceback.Repr}");
                }

                var sys = pySharp.Import("sys");
                if (sys.IsValid)
                {
                    sys.SetAttribute("last_type", PySharp.PyNone);
                    sys.SetAttribute("last_value", PySharp.PyNone);
                    sys.SetAttribute("last_traceback", PySharp.PyNone);
                }

                Py.PyErr_Clear();
            }
        }

        private void HandlePythonError()
        {
            HandlePythonError(_pySharp);
        }
    }
}