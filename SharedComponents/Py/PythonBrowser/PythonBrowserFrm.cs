using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using OpenQA.Selenium.DevTools.V116.Target;
using SharedComponents.Py.D3DDetour;

namespace SharedComponents.Py.PythonBrowser
{
    public partial class PythonBrowserFrm : Form
    {
        #region TypeMode enum

        public enum TypeMode
        {
            Auto,
            Value,
            Types,
            Class,
            List,
            Tuple,
            Dictionary
        }

        #endregion

        private bool _doEvaluate;
        private bool _done;
        private string _evaluate;
        private TypeMode _typeMode;

        private List<PyValue> _values;

        public PythonBrowserFrm()
        {
            try
            {
                InitializeComponent();

                Pulse.Initialize();
                D3DHook.OnFrame += OnFrame;

                _values = new List<PyValue>();
                AttributesList.ListViewItemSorter = new Sorter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public PyObject Evaluate(PySharp pySharp, PyObject pyObject)
        {
            var parts = _evaluate.Replace("][", "].[").Split('.');
            if (parts.Length == 0)
                return null;

            if (pyObject == null)
                pyObject = pySharp.Import(parts[0]);

            for (var i = 1; i < parts.Length; i++)
                if (parts[i].Contains("("))
                {
                    var attr = parts[i].Substring(0, parts[i].IndexOf('('));

                    if (!string.IsNullOrEmpty(attr)) {
                        pyObject = pyObject.Attribute(attr);
                        
                    }

                    Debug.WriteLine($"pyObject.GetCallableParamCount() [{pyObject.GetCallableParamCount()}]");

                    if (pyObject.IsCallable() && pyObject.GetCallableParamCount() == 0)
                    {
                        pyObject = pyObject.CallThis();
                    }

                }
                else if (parts[i].Contains("["))
                {
                    var attr = parts[i].Substring(0, parts[i].IndexOf('['));

                    var key = parts[i].Substring(parts[i].IndexOf('[') + 1, parts[i].IndexOf(']') - parts[i].IndexOf('[') - 1);
                    if (key.StartsWith("'") || key.StartsWith("\""))
                        key = key.Substring(1, key.Length - 2);

                    if (!string.IsNullOrEmpty(attr))
                        pyObject = pyObject.Attribute(attr);

                    if (pyObject.GetPyType() == PyType.DictType ||
                        pyObject.GetPyType() == PyType.DerivedDictType ||
                        pyObject.GetPyType() == PyType.DictProxyType ||
                        pyObject.GetPyType() == PyType.DerivedDictProxyType)
                    {
                        var dict = pyObject.ToDictionary();

                        pyObject = PySharp.PyZero;
                        foreach (var dictItem in dict)
                            if (GetPyValue(dictItem.Key) == key)
                                pyObject = dictItem.Value;
                    }
                    else if (pyObject.GetPyType() == PyType.SetType)
                    {
                        var list = pyObject.ToList();
                        pyObject = int.TryParse(key, out var index) ? list.ElementAtOrDefault(index) : PySharp.PyZero;
                    }
                    else
                    {
                        pyObject = int.TryParse(key, out var index) ? pyObject.GetItemAt(index) : PySharp.PyZero;
                    }
                }
                else
                {
                    pyObject = pyObject.Attribute(parts[i]);
                }

            return pyObject;
        }

        //void eval(const char* s)
        //{
        //    PyCodeObject* code = (PyCodeObject*)Py_CompileString(s, "test", Py_file_input);
        //        PyObject* main_module = PyImport_AddModule("__main__");
        //        PyObject* global_dict = PyModule_GetDict(main_module);
        //        PyObject* local_dict = PyDict_New();
        //        PyObject* obj = PyEval_EvalCode(code, global_dict, local_dict);

        //        PyObject* result = PyObject_Str(obj);
        //        PyObject_Print(result, stdout, 0);
        //    }

        private void OnFrame(object sender, EventArgs e)
        {
            try
            {
                using (var ps = new PySharp())
                {
                    // Get current target list
                    dynamic pySharp = ps;
                    dynamic pyObject = null;
                    //pyObject = pySharp.__builtin__.eve.session;

                    if (!String.IsNullOrEmpty(_execString))
                    {
                        ps.CompileStringAndExecuteString(_execString, "py");
                        _execString = null;
                    }

                    if (_doEvaluate)
                    {
                        _doEvaluate = false;
                        pyObject = Evaluate(pySharp, pyObject);
                    }

                    if (pyObject != null)
                        ListPyObject(pyObject);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                _done = true;
            }
        }

        private void ListPyObject(PyObject pyObject)
        {
            PyType type;
            switch (_typeMode)
            {
                case TypeMode.Value:
                    // Fill the value here
                    var value = new PyValue();
                    value.Attribute = "(this)";
                    value.Value = GetPyValue(pyObject);
                    value.Type = pyObject.GetPyType().ToString();
                    _values.Add(value);
                    return;

                case TypeMode.Types:
                    foreach (var attribute in pyObject.Attributes())
                    {
                        var item = new PyValue();
                        item.Attribute = attribute.Key;
                        item.Value = GetPyValue(attribute.Value);
                        var checktype = attribute.Value.GetPyType();
                        if (checktype == PyType.BoolType ||
                            checktype == PyType.IntType ||
                            checktype == PyType.LongType ||
                            checktype == PyType.FloatType ||
                            checktype == PyType.UnicodeType ||
                            checktype == PyType.StringType)
                        {
                            item.Type = attribute.Value.GetPyType().ToString();
                            item.Eval = _evaluate + "." + attribute.Key;
                            _values.Add(item);
                        }
                    }
                    return;

                case TypeMode.Class:
                    // Force it to unknown
                    type = PyType.Unknown;
                    break;

                case TypeMode.List:
                    // Force it to a list
                    type = PyType.ListType;
                    break;

                case TypeMode.Tuple:
                    // Force it to a tuple
                    type = PyType.TupleType;
                    break;

                case TypeMode.Dictionary:
                    // Force it to a dictionary
                    type = PyType.DictType;
                    break;

                default:
                    // Let the type decide
                    type = pyObject.GetPyType();
                    break;
            }

            switch (type)
            {
                case PyType.DictType:
                case PyType.DictProxyType:
                case PyType.DerivedDictType:
                case PyType.DerivedDictProxyType:
                    foreach (var attribute in pyObject.ToDictionary())
                    {
                        var item = new PyValue();
                        item.Attribute = GetPyValue(attribute.Key);
                        item.Value = GetPyValue(attribute.Value);
                        item.Type = attribute.Value.GetPyType().ToString();
                        item.Eval = _evaluate + "[" + item.Attribute + "]";
                        _values.Add(item);
                    }
                    break;

                case PyType.SetType:
                    int n = 0;
                    foreach (var attribute in pyObject.ToList())
                    {
                        var item = new PyValue();
                        item.Attribute = n.ToString();
                        item.Value = GetPyValue(attribute);
                        item.Type = attribute.GetPyType().ToString();
                        item.Eval = _evaluate + "[" + n + "]";
                        _values.Add(item);
                        n++;
                    }
                    break;
                case PyType.ListType:
                case PyType.TupleType:
                case PyType.DerivedListType:
                case PyType.DerivedTupleType:
                case PyType.BlueList:
                    var length = pyObject.Size(type);
                    for (var i = 0; i < length; i++)
                    {
                        var item = new PyValue();
                        item.Attribute = i.ToString();
                        item.Value = GetPyValue(pyObject.GetItemAt(i, type));
                        item.Type = pyObject.GetItemAt(i, type).GetPyType().ToString();
                        item.Eval = _evaluate + "[" + i + "]";
                        _values.Add(item);
                    }
                    break;

                default:
                    foreach (var attribute in pyObject.Attributes())
                    {
                        var item = new PyValue();
                        item.Attribute = attribute.Key;
                        item.Value = GetPyValue(attribute.Value);
                        item.Type = attribute.Value.GetPyType().ToString();
                        item.Eval = _evaluate + "." + attribute.Key;
                        _values.Add(item);
                    }
                    break;
            }
        }

        private string GetPyValue(PyObject attr)
        {
            switch (attr.GetPyType())
            {
                case PyType.FloatType:
                case PyType.DerivedFloatType:
                    return ((double)attr).ToString();

                case PyType.IntType:
                case PyType.LongType:
                case PyType.DerivedIntType:
                case PyType.DerivedLongType:
                    return ((long)attr).ToString();

                case PyType.BoolType:
                case PyType.DerivedBoolType:
                    return ((bool)attr).ToString();

                case PyType.StringType:
                case PyType.UnicodeType:
                case PyType.DerivedStringType:
                case PyType.DerivedUnicodeType:
                    return (string)attr;

                case PyType.MethodType:
                    var x = attr.Attribute("im_func").Attribute("func_code");
                    var name = (string)x.Attribute("co_name");
                    var argCount = (int)x.Attribute("co_argcount");
                    var args = string.Join(",", x.Attribute("co_varnames").ToList<string>().Take(argCount).ToArray());
                    return name + "(" + args + ")";

                default:
                    return attr.ToString();
            }
        }

        private void PreEvalutate()
        {
            _values.Clear();

            _typeMode = TypeMode.Auto;
            _typeMode = ValueButton.Checked ? TypeMode.Value : _typeMode;
            _typeMode = TypesButton.Checked ? TypeMode.Types : _typeMode;
            _typeMode = ClassButton.Checked ? TypeMode.Class : _typeMode;
            _typeMode = ListButton.Checked ? TypeMode.List : _typeMode;
            _typeMode = TupleButton.Checked ? TypeMode.Tuple : _typeMode;
            _typeMode = DictionaryButton.Checked ? TypeMode.Dictionary : _typeMode;

            _evaluate = EvaluateBox.Text;

            var found = false;
            foreach (var item in EvaluateBox.Items)
                if (item as string == _evaluate)
                    found = true;

            if (!found)
                EvaluateBox.Items.Insert(0, EvaluateBox.Text);

            _done = false;
        }

        private List<ListViewItem> _origItems = new List<ListViewItem>();
        private void PostEvaluate()
        {
            var timeout = 0;
            while (!_done)
            {
                Thread.Sleep(50);
                timeout++;
                if (timeout > 300) // 15 sec
                    break;
            }

            AttributesList.Items.Clear();
            _origItems.Clear();
            foreach (var value in _values)
            {
                var item = new ListViewItem(value.Attribute);
                item.SubItems.Add(value.Value);
                item.SubItems.Add(value.Type);
                item.Tag = value.Eval;
                AttributesList.Items.Add(item);
                _origItems.Add(item);

            }
        }

        private void EvaluateButton_Click(object sender, EventArgs e)
        {
            try
            {
                PreEvalutate();

                _doEvaluate = true;

                PostEvaluate();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

        }

        //private void StaticTestButton_Click(object sender, EventArgs e)
        //{
        //    PreEvalutate();

        //    _doEvaluate = true;

        //    PostEvaluate();
        //}

        private void EvaluateBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                EvaluateButton_Click(null, null);
                e.Handled = true;
            }
        }

        private void RadioButton_Click(object sender, EventArgs e)
        {
            EvaluateButton_Click(null, null);
        }

        private void AttributesList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (AttributesList.SelectedItems.Count != 1)
                    return;

                var tag = AttributesList.SelectedItems[0].Tag as string;
                if (string.IsNullOrEmpty(tag))
                    return;

                EvaluateBox.Text = tag;
                EvaluateButton_Click(null, null);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

        }

        private void AttributesList_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            try
            {
                var s = (Sorter)AttributesList.ListViewItemSorter;
                s.Column = e.Column;

                if (s.Order == SortOrder.Ascending)
                    s.Order = SortOrder.Descending;
                else
                    s.Order = SortOrder.Ascending;
                AttributesList.Sort();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

        }

        private void btn_windows_Click(object sender, EventArgs e)
        {
            EvaluateBox.Text = "carbonui.uicore.uicore.registry.windows";
            EvaluateButton_Click(null, null);
        }

        private void btn_builtin_Click(object sender, EventArgs e)
        {
            EvaluateBox.Text = builtin;
            EvaluateButton_Click(null, null);
        }

        private void btn_session_Click(object sender, EventArgs e)
        {
            EvaluateBox.Text = builtin + ".eve.session";
            EvaluateButton_Click(null, null);
        }

        private void btn_activeship_Click(object sender, EventArgs e)
        {
            EvaluateBox.Text = getService("clientDogmaIM") + ".dogmaLocation.GetShip()";
            EvaluateButton_Click(null, null);
        }

        private void btn_getservice_Click(object sender, EventArgs e)
        {
            EvaluateBox.Text = "__builtin__.sm.services";
            EvaluateButton_Click(null, null);
        }

        private void btn_const_Click(object sender, EventArgs e)
        {
            EvaluateBox.Text = builtin + ".const";
            EvaluateButton_Click(null, null);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);


        private void PythonBrowserFrm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Pulse.Shutdown();
        }

        #region Nested type: PyValue

        public class PyValue
        {
            public string Attribute { get; set; }
            public string Value { get; set; }
            public string Type { get; set; }
            public string Eval { get; set; }
        }

        #endregion

        #region const

        private const string builtin = "__builtin__";

        private string getService(string service)
        {
            return builtin + ".sm.GetService('" + service + "')";
        }

        #endregion
        private string _execString;

        private void button1_Click(object sender, EventArgs e)
        {
            _execString = richTextBox1.Text;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            EvaluateBox.Text = "carbonui.uicore.uicore.layer.shipui.slotsContainer.modulesByID";
            EvaluateButton_Click(null, null);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            EvaluateBox.Text = "__builtin__.sm.services[michelle]._Michelle__bp.balls";
            EvaluateButton_Click(null, null);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            AttributesList.Items.Clear();
            var items = _origItems.Where(k => k.Text.ToLower().Contains(textBox1.Text.ToLower())).ToList();
            foreach (var item in items)
            {
                AttributesList.Items.Add(item);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            EvaluateBox.Text = "__builtin__.sm.services[sceneManager].primaryJob.scene.objects";
            EvaluateButton_Click(null, null);
        }
    }

    // Implements the manual sorting of items by columns.
    internal class Sorter : IComparer
    {
        public int Column = 0;
        public SortOrder Order = SortOrder.Ascending;

        public int Compare(object x, object y) // IComparer Member
        {
            try
            {
                if (!(x is ListViewItem))
                    return 0;
                if (!(y is ListViewItem))
                    return 0;

                var l1 = (ListViewItem)x;
                var l2 = (ListViewItem)y;

                if (l1.ListView.Columns[Column].Tag == null)
                    l1.ListView.Columns[Column].Tag = "Text";

                if (l1.ListView.Columns[Column].Tag.ToString() == "Numeric")
                {
                    var fl1 = float.Parse(l1.SubItems[Column].Text.Replace("%", ""));
                    var fl2 = float.Parse(l2.SubItems[Column].Text.Replace("%", ""));

                    if (Order == SortOrder.Ascending)
                        return fl1.CompareTo(fl2);
                    else
                        return fl2.CompareTo(fl1);
                }
                else
                {
                    var str1 = l1.SubItems[Column].Text;
                    var str2 = l2.SubItems[Column].Text;

                    if (Order == SortOrder.Ascending)
                        return str1.CompareTo(str2);
                    else
                        return str2.CompareTo(str1);
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return 0;
        }
    }
}

//if (_doTest)
//{
//    _doTest = false;

//    pyObject = pySharp.__builtin__.eve.session;

//var file = pySharp.__builtin__.open("c:/blaat.txt", "wb");
//file.write("hello world");
//file.close();

//// Make eve reload the compiled code file (stupid DiscardCode function :)
//pySharp.Import("nasty").Attribute("nasty").Attribute("compiler").Call("Load", pySharp.Import("nasty").Attribute("nasty").Attribute("compiledCodeFile"));

//// Get a reference to all code files
//var dict = pySharp.Import("nasty").Attribute("nasty").Attribute("compiler").Attribute("code").ToDictionary();

//// Get the magic once
//var magic = pySharp.Import("imp").Call("get_magic");

//foreach (var item in dict)
//{
//    // Clean up the path
//    var path = (string)item.Key.Item(0);
//    if (path.IndexOf(":") >= 0)
//        path = path.Substring(path.IndexOf(":") + 1);
//    while (path.StartsWith("/.."))
//        path = path.Substring(3);
//    path = "c:/dump/code" + path + "c";

//    // Create the directory
//    Directory.CreateDirectory(Path.GetDirectoryName(path));

//    var file = pySharp.Import("__builtin__").Call("open", path, "wb");
//    var time = pySharp.Import("os").Attribute("path").Call("getmtime", path);

//    // Write the magic
//    file.Call("write", magic);
//    // Write the time
//    file.Call("write", pySharp.Import("struct").Call("pack", "<i", time));
//    // Write the code
//    pySharp.Import("marshal").Call("dump", item.Value.Item(0), file);
//    // Close the file
//    file.Call("close");
//}
//}