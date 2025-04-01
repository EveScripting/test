using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharedComponents.EveMarshal
{

    public class PyUnknown : PyRep
    {

        public PyUnknown()
            : base(PyObjectType.List)
        {
        }

        public override void Decode(Unmarshal context, MarshalOpcode op)
        {

        }

        protected override void EncodeInternal(BinaryWriter output)
        {

        }

        public override string ToString()
        {
            return "<PyUnknown>";
        }

        public override void dump(PrettyPrinter printer)
        {
            printer.addLine("[PyUnknown] " + PrettyPrinter.PrintRawData(this));
        }

    }

}