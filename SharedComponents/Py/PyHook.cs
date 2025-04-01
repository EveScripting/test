using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharedComponents.Py
{

    public class PyHookItem
    {
        private IntPtr _pyObjRef;
        private string _attributeName;
        private IntPtr _originalFunc;
        private PyDelegate _pyDelegate;
        private IntPtr _pyDelegateObj;
        private string _id;
        public PyHookItem(IntPtr pyObjRef, string attributeName)
        {
            _id = pyObjRef.ToString() + attributeName;
            _pyObjRef = pyObjRef;
            _attributeName = attributeName;
            _originalFunc = Py.PyObject_GetAttrString(pyObjRef, attributeName);
            _pyDelegate = new PyDelegate(Detour);
            _pyDelegateObj = CreateDelegatePyObj(_pyDelegate);
            Py.PyObject_SetAttrString(pyObjRef, attributeName, _pyDelegateObj);
        }

        public IntPtr Detour(IntPtr self, IntPtr args)
        {
            using (var py = new PySharp(false))
            {
                Console.WriteLine($"Called! [{_id}]");

                Console.WriteLine($"Calling orignal func. [{_id}]");
                var res = Py.PyEval_CallObjectWithKeywords(_originalFunc, args, PySharp.PyZero);
                var argPy = new PyObject(py, args, true); // new ref true means it wil be decrefed upon pysharp dispose
                var resPy = new PyObject(py, res, false); // new ref true means it wil be decrefed upon pysharp dispose
                Console.WriteLine(resPy.LogObject());
                return res;
            }
        }

        ~PyHookItem()
        {
            try
            {
                Console.WriteLine($"Restoring original func [{_id}]");
                // set back orig func
                Py.PyObject_SetAttrString(_pyObjRef, _attributeName, _originalFunc);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public IntPtr CreateDelegatePyObj(Delegate value)
        {
            Py.PyMethodDef md;
            md.ml_doc = (IntPtr)0;
            md.ml_name = Marshal.StringToHGlobalAnsi("");
            md.ml_meth = Marshal.GetFunctionPointerForDelegate(value);
            md.ml_flags = 1;
            var name = Py.PyString_FromString("");
            var mdPtr = Marshal.AllocHGlobal(Marshal.SizeOf(md));
            Marshal.StructureToPtr(md, mdPtr, false);
            var result = Py.PyCFunction_NewEx(mdPtr, (IntPtr)0, name);
            return result;
        }
    }

    public delegate IntPtr PyDelegate(IntPtr self, IntPtr args);

    public class PyHook
    {

        private Dictionary<string, PyHookItem> _hooks;

        public PyHook()
        {
            _hooks = new Dictionary<string, PyHookItem>();
        }
        public string AddGetHook(IntPtr pyObjRef, string attributeName)
        {
            var id = pyObjRef.ToString() + attributeName;
            if (!_hooks.ContainsKey(id))
            {
                Console.WriteLine($"Added hook with id [{id}]");
                _hooks.Add(id, new PyHookItem(pyObjRef, attributeName));
            }
            return id;
        }
    }
}
