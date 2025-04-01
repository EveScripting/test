using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SharedComponents.Py
{
    public partial class PySharp : IDisposable
    {
        public static PyObject PyZero = new PyObject(null, IntPtr.Zero, false);
        public static PyObject PyNone = new PyObject(null, Py.PyNoneStruct, false);

        /// <summary>
        ///     Dummy code
        /// </summary>
        private PyObject _dummyCode;

        /// <summary>
        ///     Dummy frame
        /// </summary>
        private PyObject _frame;

        /// <summary>
        ///     Import cache
        /// </summary>
        private Dictionary<string, PyObject> _importCache;

        /// <summary>
        ///     Int cache
        /// </summary>
        private Dictionary<int, PyObject> _intCache;

        /// <summary>
        ///     Long cache
        /// </summary>
        private Dictionary<long, PyObject> _longCache;

        /// <summary>
        ///     Old frame
        /// </summary>
        private PyObject _oldFrame;

        /// <summary>
        ///     PyFalse cache
        /// </summary>
        private PyObject _pyFalse;

        /// <summary>
        ///     List of python objects, these will be released when disposing of PySharp
        /// </summary>
        private List<PyObject> _pyReferences;

        /// <summary>
        ///     PyTrue cache
        /// </summary>
        private PyObject _pyTrue;

        /// <summary>
        ///     String cache
        /// </summary>
        private Dictionary<string, PyObject> _stringCache;

        /// <summary>
        ///     Unicode cache
        /// </summary>
        private Dictionary<string, PyObject> _unicodeCache;

        /// <summary>
        ///     Create a new PySharp object
        /// </summary>
        public PySharp()
        {
            _dummyCode = PyZero;
            _frame = PyZero;

            _pyReferences = new List<PyObject>();
            _importCache = new Dictionary<string, PyObject>();
            _stringCache = new Dictionary<string, PyObject>();
            _unicodeCache = new Dictionary<string, PyObject>();
            _intCache = new Dictionary<int, PyObject>();
            _longCache = new Dictionary<long, PyObject>();
        }

        public PySharp(bool createFrame)
            : this()
        {
            if (!createFrame)
                return;

            // Create dummy code (needed for the new frame)
            _dummyCode = new PyObject(this, Py.PyCode_NewEmpty("", "", 1), true);
            // Create a new frame
            _frame = new PyObject(this,
                Py.PyFrame_New(Py.GetThreadState(), _dummyCode, Import("__main__").Attribute("__dict__"), Import("__main__").Attribute("__dict__")), true);
            // Exchange frames
            _oldFrame = new PyObject(this, Py.ExchangePyFrame(_frame), false);
        }

        // always call twice via try/finally
        public void SwapFrames()
        {
            if (_frame != PyZero)
            {
                _oldFrame = new PyObject(this, Py.ExchangePyFrame(_oldFrame), false);
            }
        }

        #region IDisposable Members

        /// <summary>
        ///     Dispose of all PyReferences
        /// </summary>
        public void Dispose()
        {
            // Clear any python errors we might have caused
            PyObject.HandlePythonError(this);

            // Release the frame created for PySharp
            if (_frame != PyZero)
            {
                // Return the old frame
                Py.ExchangePyFrame(_oldFrame);

                _frame = PyZero;
            }

            // Remove any of the references we caused
            foreach (var pyObject in _pyReferences)
                pyObject.Release();

            // Clear any python errors we might have caused
            PyObject.HandlePythonError(this);
        }

        #endregion

        /// <summary>
        ///     Import a PyModule
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public PyObject Import(string module)
        {
            PyObject result;
            if (!_importCache.TryGetValue(module, out result))
            {
                result = new PyObject(this, Py.PyImport_ImportModule(module), true);
                _importCache[module] = result;
            }
            return result;
        }

        private int Py_single_input = 256;
        private int Py_file_input = 257;
        private int Py_eval_input = 258;

        public void CompileStringAndExecuteString(string source, string filename)
        {
            IntPtr pycodeObj = Py.Py_CompileString(source, filename, Py_file_input);
            var codeObj = new PyObject(this, pycodeObj, true);
            if (codeObj.IsValid)
            {
                var main = Py.PyImport_AddModule("__main__");
                var mainObj = new PyObject(this, main, false);
                if (mainObj.IsValid)
                {
                    var globalDict = Py.PyModule_GetDict(main);
                    var globalDictObj = new PyObject(this, globalDict, false);
                    var localDict = Py.PyDict_New();
                    var localDictObj = new PyObject(this, localDict, true);
                    Py.PyEval_EvalCode(pycodeObj, globalDict, localDict);
                }
            }
        }

        /// <summary>
        /// Internal EVE time 'SimTime' -- Simulation time
        /// </summary>
        public long SimTime
        {
            get
            {
                if (!_simTime.HasValue)
                    _simTime = Import("blue").Attribute("os").Call("GetSimTime").ToLong();

                return _simTime.Value;
            }
        }

        private long? _simTime;


        public long WallclockTime
        {
            get
            {
                if (!_wallclockTime.HasValue)
                    _wallclockTime = Import("blue").Attribute("os").Call("GetWallclockTime").ToLong();

                return _wallclockTime.Value;
            }
        }

        private long? _wallclockTime;


        public long WallclockTimeNow
        {
            get
            {
                if (!_wallclockTimeNow.HasValue)
                    _wallclockTimeNow = Import("blue").Attribute("os").Call("GetWallclockTime").ToLong();

                return _wallclockTimeNow.Value;
            }
        }

        private long? _wallclockTimeNow;

        public DateTime SimTimeDateTime
        {
            get
            {
                return PyObject.PY_EPOCHE_DATE.AddTicks(SimTime);
            }
        }

        /// <summary>
        /// The time difference between Datetime.UtcNow and blue.os.GetWallclockTimeNow()
        /// </summary>
        public double TimeDiffNow
        {
            get
            {
                var now = DateTime.UtcNow;
                var simtime = PyObject.PY_EPOCHE_DATE.AddTicks(this.WallclockTimeNow);
                var diff = (now - simtime).TotalMilliseconds;
                return diff;
            }
        }

        /// <summary>
        ///     Get a PyObject from an object
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public PyObject From(object value)
        {
            if (value is bool)
                return From((bool)value);
            if (value is int)
                return From((int)value);
            if (value is long)
                return From((long)value);
            if (value is float)
                return From((float)value);
            if (value is double)
                return From((double)value);
            if (value is string)
                return From((string)value);
            if (value is byte[])
            {
                return From((byte[])value);
            }
            if (value is IEnumerable<PyObject>)
                return From((IEnumerable<PyObject>)value);
            if (value is IEnumerable<int>)
                return From((IEnumerable<int>)value);
            if (value is IEnumerable<long>)
                return From((IEnumerable<long>)value);
            if (value is IEnumerable<float>)
                return From((IEnumerable<float>)value);
            if (value is IEnumerable<double>)
                return From((IEnumerable<double>)value);
            if (value is IEnumerable<string>)
                return From((IEnumerable<string>)value);
            if (value is IEnumerable<object>)
                return From((IEnumerable<object>)value);
            if (value is Delegate)
                return From((Delegate)value);
            if (value is PyObject)
                return (PyObject)value;
            return null;
        }

        /// <summary>
        ///     Get a PyObject from an integer
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public PyObject From(bool value)
        {
            if (value && _pyTrue == null)
                _pyTrue = new PyObject(this, Py.PyBool_FromLong(1), true);

            if (!value && _pyFalse == null)
                _pyFalse = new PyObject(this, Py.PyBool_FromLong(0), true);

            return value ? _pyTrue : _pyFalse;
        }

        /// <summary>
        ///     Get a PyObject from an integer
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public PyObject From(int value)
        {
            PyObject result;
            if (!_intCache.TryGetValue(value, out result))
            {
                result = new PyObject(this, Py.PyLong_FromLong(value), true);
                _intCache[value] = result;
            }
            return result;
        }

        /// <summary>
        ///     Get a PyObject from a long
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public PyObject From(long value)
        {
            PyObject result;
            if (!_longCache.TryGetValue(value, out result))
            {
                result = new PyObject(this, Py.PyLong_FromLongLong(value), true);
                _longCache[value] = result;
            }
            return result;
        }

        public PyObject From(byte[] data)
        {
            PyObject result = null;
            var native = Marshal.AllocHGlobal(data.Length);
            try
            {
                Marshal.Copy(data, 0, native, data.Length);
                result = new PyObject(this, Py.PyString_FromStringAndSize(native, data.Length), true);
            }
            finally
            {
                Marshal.FreeHGlobal(native);
            }
            return result;
        }

        /// <summary>
        ///     Get a PyObject from a double
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public PyObject From(float value)
        {
            // Note: Caching double's has no use due to rounding errors
            return new PyObject(this, Py.PyFloat_FromDouble(value), true);
        }


        public PyObject CreateInstance(PyObject type)
        {
            if (type.GetPyType() != PyType.TypeType)
                return PyNone;

            return new PyObject(this, Py.PyObject_CallObject(type, IntPtr.Zero), true);
        }

        public PyObject CreateInstance(PyObject type, params object[] parms)
        {
            var pyParms = new List<PyObject>();
            foreach (var parm in parms)
            {
                var pyParm = this.From(parm);

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
                pyArgs = new PyObject(this, Py.Py_BuildValue(format), true);
            if (pyParms.Count == 1)
                pyArgs = new PyObject(this, Py.Py_BuildValue(format, pyParms[0]), true);
            if (pyParms.Count == 2)
                pyArgs = new PyObject(this, Py.Py_BuildValue(format, pyParms[0], pyParms[1]), true);
            if (pyParms.Count == 3)
                pyArgs = new PyObject(this, Py.Py_BuildValue(format, pyParms[0], pyParms[1], pyParms[2]), true);
            if (pyParms.Count == 4)
                pyArgs = new PyObject(this, Py.Py_BuildValue(format, pyParms[0], pyParms[1], pyParms[2], pyParms[3]), true);
            if (pyParms.Count == 5)
                pyArgs = new PyObject(this, Py.Py_BuildValue(format, pyParms[0], pyParms[1], pyParms[2], pyParms[3], pyParms[4]), true);
            if (pyParms.Count == 6)
                pyArgs = new PyObject(this, Py.Py_BuildValue(format, pyParms[0], pyParms[1], pyParms[2], pyParms[3], pyParms[4], pyParms[5]), true);
            if (pyParms.Count == 7)
                pyArgs = new PyObject(this, Py.Py_BuildValue(format, pyParms[0], pyParms[1], pyParms[2], pyParms[3], pyParms[4], pyParms[5], pyParms[6]),
                    true);
            if (pyParms.Count == 8)
                pyArgs = new PyObject(this,
                    Py.Py_BuildValue(format, pyParms[0], pyParms[1], pyParms[2], pyParms[3], pyParms[4], pyParms[5], pyParms[6], pyParms[7]), true);

            if (pyArgs == null)
                throw new NotImplementedException();

            if (type.GetPyType() != PyType.TypeType)
                return PyNone;

            if (pyParms.Count == 0)
                return new PyObject(this, Py.PyObject_CallObject(type, IntPtr.Zero), true);
            else
                return new PyObject(this, Py.PyObject_CallObject(type, pyArgs), true);
        }

        /// <summary>
        ///     Get a PyObject from a double
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public PyObject From(double value)
        {
            // Note: Caching double's has no use due to rounding errors
            return new PyObject(this, Py.PyFloat_FromDouble(value), true);
        }

        /// <summary>
        ///     Get a PyObject from a string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public PyObject From(string value)
        {
            PyObject result;
            if (!_stringCache.TryGetValue(value, out result))
            {
                result = new PyObject(this, Py.PyString_FromString(value), true);
                _stringCache[value] = result;
            }
            return result;
        }

        /// <summary>
        ///     Get a PyObject from a string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public PyObject UnicodeFrom(string value)
        {
            PyObject result;
            if (!_unicodeCache.TryGetValue(value, out result))
            {
                result = new PyObject(this, Py.PyUnicodeUCS2_FromUnicode(value, value.Length), true);
                _unicodeCache[value] = result;
            }
            return result;
        }

        /// <summary>
        ///     Get a PyObject from a list
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public PyObject From<TItem>(IEnumerable<TItem> value)
        {
            var result = new PyObject(this, Py.PyList_New(value.Count()), true);

            for (var i = 0; i < value.Count(); i++)
            {
                var pyItem = From(value.ElementAt(i));

                if (pyItem == null)
                    return PyZero;

                // PyList_SetItem steals a reference, this makes sure we dont free it later
                Py.Py_IncRef(pyItem);

                if (Py.PyList_SetItem(result, i, pyItem) == -1)
                    return PyZero;
            }

            return result;
        }

        /// <summary>
        ///     Get a PyObject from a delegate
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public PyObject From(Delegate value)
        {
            PyObject result, name;
            Py.PyMethodDef md;

            md.ml_doc = (IntPtr)0;
            md.ml_name = Marshal.StringToHGlobalAnsi("testmethod");
            md.ml_meth = Marshal.GetFunctionPointerForDelegate(value);
            md.ml_flags = 1; // METH_VARARGS
            name = From((string)"testmethod");

            var mdPtr = Marshal.AllocHGlobal(Marshal.SizeOf(md));
            Marshal.StructureToPtr(md, mdPtr, false);

            result = new PyObject(this, Py.PyCFunction_NewEx(mdPtr, (IntPtr)0, name), true);

            return result;
        }


        /// <summary>
        ///     Add a reference to the reference stack
        /// </summary>
        /// <param name="reference">The reference to add to the stack</param>
        /// <returns>The reference that was added to the reference stack</returns>
        internal PyObject AddReference(PyObject reference)
        {
            if (!reference.IsNull)
                _pyReferences.Add(reference);

            return reference;
        }

        /// <summary>
        ///     Remove a reference from the reference stack
        /// </summary>
        /// <param name="reference">The reference to remove from the stack</param>
        /// <returns>The reference that was removed from the reference stack</returns>
        internal PyObject RemoveReference(PyObject reference)
        {
            if (!reference.IsNull)
                _pyReferences.Remove(reference);

            return reference;
        }
    }
}