using System.IO;

namespace SharedComponents.EveMarshal
{

    public class PySubStruct : PyRep
    {
        public PyRep Definition { get; set; }

        public PySubStruct()
            : base(PyObjectType.SubStruct)
        {
            
        }

        public override void Decode(Unmarshal context, MarshalOpcode op)
        {
            Definition = context.ReadObject(this);
        }

        protected override void EncodeInternal(BinaryWriter output)
        {
            output.WriteOpcode(MarshalOpcode.SubStruct);
            Definition.Encode(output);
        }

        public override void dump(PrettyPrinter printer)
        {
            printer.addLine("[PySubStruct]");
            printer.addItem(Definition);
        }

    }

}