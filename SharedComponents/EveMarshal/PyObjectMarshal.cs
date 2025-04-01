using System.Data;
using System.IO;

namespace SharedComponents.EveMarshal
{
    
    public class PyObjectMarshal : PyRep
    {
        public PyObjectMarshal()
            : base(PyObjectType.ObjectData)
        {
            
        }

        public PyObjectMarshal(string objectName, PyRep arguments)
            : base(PyObjectType.ObjectData)
        {
            Name = objectName;
            Arguments = arguments;
        }

        public string Name { get; set; }
        public PyRep Arguments { get; set; }

        public override void Decode(Unmarshal context, MarshalOpcode op)
        {
            var nameObject = context.ReadObject(this);
            if (nameObject.Type != PyObjectType.String)
                throw new DataException("Expected PyString");
            Name = (nameObject as PyString).Value;

            Arguments = context.ReadObject(this);
        }

        protected override void EncodeInternal(BinaryWriter output)
        {
            output.WriteOpcode(MarshalOpcode.Object);
            new PyString(Name).Encode(output);
            Arguments.Encode(output);
        }

        public override void dump(PrettyPrinter printer)
        {
            printer.addLine("[PyObject Name: " + Name + "]" + PrettyPrinter.PrintRawData(this));
            printer.addItem(Arguments);
        }

    }

}