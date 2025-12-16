using ConsoleGUI.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumbee.Console
{
    public abstract class Prompt : Control, IInputListener
    {
        public abstract void OnInput(InputEvent inputEvent);
    }
}
