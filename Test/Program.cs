using System;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Game;

namespace IngameScript {
    partial class Program: MyGridProgram {
        StringBuilder _text = new StringBuilder();
        IMyProjector _proj;

        public Program() {
            /*ShipCore.Create(
                GridTerminalSystem,
                Me,
                Runtime,
                new SensorBlockDevice()
            );*/
            _proj = GridTerminalSystem.GetBlockWithName("ITEMIZED") as IMyProjector;

            var blocks = _proj.RemainingBlocksPerType;
            char[] delimiters = new char[] { ',' };
            char[] remove = new char[] { '[', ']' };
            foreach (var item in blocks) {
            	string[] blockInfo = item.ToString().Trim(remove).Split(delimiters, StringSplitOptions.None);
                var name = blockInfo[0].Split(new char[] { '/' }, StringSplitOptions.None);
                var count = blockInfo[1];

                _text.Append(name[1]);
                _text.Append(',');
                _text.Append(count);
                _text.Append('\n');

            }


            Me.CustomData = _text.ToString();
        }

        public void Save() {
            Storage = _text.ToString(); 
        }

        public void Main(string argument, UpdateType updateSource) {
            /*var a = new XmlSerializer(typeof(int));
            ShipCore.I.RunMain();*/
        }
    }
}
